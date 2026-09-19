using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.Services;
using ATMCashForecasting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ATMCashForecasting.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ATMCashForecasting.Infrastructure.BackgroundJobs;

/// <summary>
/// Module 8 automation: scans for ATMs that have gone silent (no transaction feed in 24h) and raises
/// AtmOffline alerts, then dispatches any Pending NotificationLog rows through the registered senders.
/// Scheduled every 15 minutes via Program.cs.
/// </summary>
public class AlertScanJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAlertService _alertService;
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly ILogger<AlertScanJob> _logger;

    public AlertScanJob(ApplicationDbContext dbContext, IAlertService alertService, IEnumerable<INotificationSender> senders, ILogger<AlertScanJob> logger)
    {
        _dbContext = dbContext;
        _alertService = alertService;
        _senders = senders;
        _logger = logger;
    }

    public async Task ScanForOfflineAtmsAsync()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var silentAtms = await _dbContext.Atms
            .Where(a => a.Status == Domain.Enums.AtmOperationalStatus.Active)
            .Where(a => !_dbContext.Transactions.Any(t => t.AtmId == a.Id && t.CreatedAtUtc >= cutoff))
            .ToListAsync();

        foreach (var atm in silentAtms)
        {
            await _alertService.RaiseAsync(atm.Id, AlertType.AtmOffline, AlertSeverity.High,
                $"No data feed from {atm.AtmCode}",
                $"ATM {atm.AtmCode} has not reported a transaction in over 24 hours. Verify switch connectivity.");
        }

        _logger.LogInformation("Alert scan found {Count} silent ATMs", silentAtms.Count);
    }

    public async Task DispatchPendingNotificationsAsync()
    {
        var pending = await _dbContext.NotificationLogs
            .Include(n => n.Alert)
            .Where(n => n.Status == NotificationStatus.Pending || (n.Status == NotificationStatus.Failed && n.RetryCount < 3))
            .Take(200)
            .ToListAsync();

        foreach (var log in pending)
        {
            var sender = _senders.FirstOrDefault(s => s.Channel == log.Channel);
            if (sender is null)
            {
                continue;
            }

            var sent = await sender.SendAsync(log.Recipient, log.Alert.Title, log.Alert.Message);
            if (sent)
            {
                log.Status = NotificationStatus.Sent;
                log.SentAtUtc = DateTime.UtcNow;
            }
            else
            {
                log.Status = NotificationStatus.Failed;
                log.RetryCount++;
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Dispatched {Count} pending notifications", pending.Count);
    }
}
