using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Forecasting;
using ATMCashForecasting.Application.Forecasting.Models;
using ATMCashForecasting.Domain.Entities;
using ATMCashForecasting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ATMCashForecasting.Application.Services;

/// <summary>
/// Module 4 orchestration: pulls per-ATM history, runs it through IForecastingEngine, archives the
/// previous current results into ForecastHistory, persists the new current ForecastResult rows, and
/// back-fills actuals/accuracy on prior forecasts whose target date has now passed.
/// </summary>
public class ForecastService : IForecastService
{
    private const int HistoryWindowDays = 120;

    private readonly IUnitOfWork _uow;
    private readonly IForecastingEngine _engine;
    private readonly IDateTimeProvider _clock;
    private readonly IAlertService _alertService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ForecastService> _logger;

    public ForecastService(
        IUnitOfWork uow,
        IForecastingEngine engine,
        IDateTimeProvider clock,
        IAlertService alertService,
        IAuditService auditService,
        ILogger<ForecastService> logger)
    {
        _uow = uow;
        _engine = engine;
        _clock = clock;
        _alertService = alertService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ForecastRunSummaryDto> RunForecastAsync(RunForecastRequest request, CancellationToken ct = default)
    {
        var started = _clock.UtcNow;
        var forecastRunId = Guid.NewGuid();
        var asOf = _clock.Today;

        var atmIds = request.AtmIds is { Count: > 0 }
            ? request.AtmIds
            : await _uow.Atms.Query()
                .Where(a => !a.IsDeleted && a.Status == AtmOperationalStatus.Active)
                .Select(a => a.Id)
                .ToListAsync(ct);

        var processed = 0;
        var failed = 0;
        var cutoff = asOf.AddDays(-HistoryWindowDays);

        foreach (var atmId in atmIds)
        {
            try
            {
                var history = await _uow.Transactions.Query()
                    .Where(t => t.AtmId == atmId && t.TransactionDate >= cutoff && t.TransactionDate <= asOf)
                    .OrderBy(t => t.TransactionDate)
                    .Select(t => new HistoricalPoint(t.TransactionDate, t.WithdrawalAmount))
                    .ToListAsync(ct);

                if (history.Count < 3)
                {
                    failed++;
                    await _alertService.RaiseAsync(atmId, AlertType.ForecastFailure, AlertSeverity.Warning,
                        "Insufficient history for forecast",
                        $"ATM {atmId} has only {history.Count} day(s) of transaction history; at least 3 are required to forecast.", ct);
                    continue;
                }

                var output = _engine.Forecast(new ForecastRequest(atmId, history, request.Method, request.Horizon, asOf));
                if (output.Points.Count == 0)
                {
                    failed++;
                    continue;
                }

                await ArchiveExistingResultsAsync(atmId, ct);

                foreach (var point in output.Points)
                {
                    await _uow.ForecastResults.AddAsync(new ForecastResult
                    {
                        AtmId = atmId,
                        ForecastRunId = forecastRunId,
                        RunAtUtc = started,
                        Method = request.Method,
                        Horizon = request.Horizon,
                        TargetDate = point.TargetDate,
                        ForecastedWithdrawalAmount = point.ForecastedAmount,
                        ConfidenceScore = point.ConfidenceScore,
                        LowerBound = point.LowerBound,
                        UpperBound = point.UpperBound
                    }, ct);
                }

                if (!output.IsReliable)
                {
                    await _alertService.RaiseAsync(atmId, AlertType.LowConfidenceForecast, AlertSeverity.Warning,
                        "Low-confidence forecast generated",
                        $"Forecast for ATM {atmId} using {request.Method} has average confidence below 50%.", ct);
                }

                processed++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Forecast run {RunId} failed for ATM {AtmId}", forecastRunId, atmId);
            }
        }

        await _uow.SaveChangesAsync(ct);
        await BackfillActualsAsync(ct);

        await _auditService.LogAsync(AuditAction.ForecastRun, nameof(ForecastResult), forecastRunId.ToString(),
            null, $"Method={request.Method}, Horizon={request.Horizon}, Processed={processed}, Failed={failed}", "system-scheduler", ct);

        return new ForecastRunSummaryDto(forecastRunId, started, processed, failed, _clock.UtcNow - started);
    }

    private async Task ArchiveExistingResultsAsync(int atmId, CancellationToken ct)
    {
        var existing = await _uow.ForecastResults.Query().Where(f => f.AtmId == atmId).ToListAsync(ct);
        if (existing.Count == 0) return;

        var history = existing.Select(f => new ForecastHistory
        {
            AtmId = f.AtmId,
            ForecastRunId = f.ForecastRunId,
            RunAtUtc = f.RunAtUtc,
            Method = f.Method,
            Horizon = f.Horizon,
            TargetDate = f.TargetDate,
            ForecastedWithdrawalAmount = f.ForecastedWithdrawalAmount,
            ConfidenceScore = f.ConfidenceScore,
            ActualWithdrawalAmount = f.ActualWithdrawalAmount,
            AbsolutePercentageError = f.AbsolutePercentageError
        });

        await _uow.ForecastHistories.AddRangeAsync(history, ct);
        foreach (var item in existing)
        {
            _uow.ForecastResults.Remove(item);
        }
    }

    /// <summary>Fills in actuals + MAPE for archived forecasts whose target date has now occurred.</summary>
    private async Task BackfillActualsAsync(CancellationToken ct)
    {
        var today = _clock.Today;
        var pending = await _uow.ForecastHistories.Query()
            .Where(f => f.ActualWithdrawalAmount == null && f.TargetDate < today)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        var atmDatePairs = pending.Select(p => (p.AtmId, p.TargetDate)).Distinct().ToList();
        var atmIds = atmDatePairs.Select(p => p.AtmId).Distinct().ToList();

        var actuals = await _uow.Transactions.Query()
            .Where(t => atmIds.Contains(t.AtmId))
            .ToDictionaryAsync(t => (t.AtmId, t.TransactionDate), t => t.WithdrawalAmount, ct);

        foreach (var forecast in pending)
        {
            if (!actuals.TryGetValue((forecast.AtmId, forecast.TargetDate), out var actual)) continue;

            forecast.ActualWithdrawalAmount = actual;
            forecast.AbsolutePercentageError = actual == 0
                ? (forecast.ForecastedWithdrawalAmount == 0 ? 0 : 1)
                : Math.Abs((forecast.ForecastedWithdrawalAmount - actual) / actual);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ForecastResultDto>> GetLatestForecastAsync(int atmId, CancellationToken ct = default)
    {
        return await _uow.ForecastResults.Query()
            .Include(f => f.Atm)
            .Where(f => f.AtmId == atmId)
            .OrderBy(f => f.TargetDate)
            .Select(f => new ForecastResultDto(
                f.AtmId, f.Atm.AtmCode, f.Method, f.Horizon, f.TargetDate,
                f.ForecastedWithdrawalAmount, f.ConfidenceScore, f.LowerBound ?? 0, f.UpperBound ?? 0))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ForecastAccuracyPointDto>> GetAccuracyHistoryAsync(int atmId, int days, CancellationToken ct = default)
    {
        var since = _clock.Today.AddDays(-days);
        return await _uow.ForecastHistories.Query()
            .Where(f => f.AtmId == atmId && f.TargetDate >= since)
            .OrderBy(f => f.TargetDate)
            .Select(f => new ForecastAccuracyPointDto(f.TargetDate, f.ForecastedWithdrawalAmount, f.ActualWithdrawalAmount, f.AbsolutePercentageError))
            .ToListAsync(ct);
    }

    public async Task<decimal> GetOverallForecastAccuracyAsync(CancellationToken ct = default)
    {
        var errors = await _uow.ForecastHistories.Query()
            .Where(f => f.AbsolutePercentageError != null)
            .Select(f => f.AbsolutePercentageError!.Value)
            .ToListAsync(ct);

        if (errors.Count == 0) return 0;

        var mape = errors.Average();
        var accuracy = Math.Max(0, 1 - mape) * 100;
        return Math.Round(accuracy, 2);
    }
}
