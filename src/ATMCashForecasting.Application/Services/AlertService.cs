using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Domain.Entities;
using ATMCashForecasting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATMCashForecasting.Application.Services;

/// <summary>
/// Raises and tracks alerts (Module 8). Dispatch to Email/SMS/Teams is handled by the
/// Infrastructure-layer background job, which reads NotificationLog rows queued here.
/// </summary>
public class AlertService : IAlertService
{
    private readonly IUnitOfWork _uow;

    public AlertService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<AlertDto>> GetOpenAlertsAsync(CancellationToken ct = default)
    {
        return await _uow.Alerts.Query()
            .Include(a => a.Atm)
            .Where(a => a.Status == AlertStatus.Open || a.Status == AlertStatus.Acknowledged)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.TriggeredAtUtc)
            .Select(a => new AlertDto(a.Id, a.AtmId, a.Atm != null ? a.Atm.AtmCode : null, a.AlertType, a.Severity, a.Status, a.Title, a.Message, a.TriggeredAtUtc))
            .ToListAsync(ct);
    }

    public async Task RaiseAsync(int? atmId, AlertType type, AlertSeverity severity, string title, string message, CancellationToken ct = default)
    {
        // De-duplicate: don't raise the same open alert type for the same ATM twice in a row.
        var alreadyOpen = await _uow.Alerts.Query().AnyAsync(a =>
            a.AtmId == atmId && a.AlertType == type && a.Status == AlertStatus.Open, ct);

        if (alreadyOpen) return;

        var alert = new Alert
        {
            AtmId = atmId,
            AlertType = type,
            Severity = severity,
            Title = title,
            Message = message
        };

        await _uow.Alerts.AddAsync(alert, ct);
        await _uow.SaveChangesAsync(ct);

        foreach (var channel in new[] { NotificationChannel.Email, NotificationChannel.Sms, NotificationChannel.Teams })
        {
            // Only Email + Teams are queued by default; SMS is reserved for Critical severity to control cost.
            if (channel == NotificationChannel.Sms && severity != AlertSeverity.Critical) continue;

            await _uow.NotificationLogs.AddAsync(new NotificationLog
            {
                AlertId = alert.Id,
                Channel = channel,
                Recipient = "role-based-distribution-list",
                Status = NotificationStatus.Pending
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task AcknowledgeAsync(int alertId, string? userId, CancellationToken ct = default)
    {
        var alert = await _uow.Alerts.GetByIdAsync(alertId, ct);
        if (alert is null) return;

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAtUtc = DateTime.UtcNow;
        alert.AcknowledgedByUserId = userId;
        _uow.Alerts.Update(alert);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task ResolveAsync(int alertId, CancellationToken ct = default)
    {
        var alert = await _uow.Alerts.GetByIdAsync(alertId, ct);
        if (alert is null) return;

        alert.Status = AlertStatus.Resolved;
        alert.ResolvedAtUtc = DateTime.UtcNow;
        _uow.Alerts.Update(alert);
        await _uow.SaveChangesAsync(ct);
    }
}
