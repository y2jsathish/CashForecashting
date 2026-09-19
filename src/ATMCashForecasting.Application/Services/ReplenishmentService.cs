using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.Common.Models;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Forecasting.Models;
using ATMCashForecasting.Application.Replenishment;
using ATMCashForecasting.Domain.Entities;
using ATMCashForecasting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATMCashForecasting.Application.Services;

/// <summary>
/// Module 5 orchestration: for every ATM with a live forecast, pulls the latest known cash
/// balance, delegates the load-required / priority / depletion-date computation to
/// IReplenishmentEngine, persists the recommendation, and raises risk alerts (Module 6/8).
/// </summary>
public class ReplenishmentService : IReplenishmentService
{
    private readonly IUnitOfWork _uow;
    private readonly IReplenishmentEngine _engine;
    private readonly IDateTimeProvider _clock;
    private readonly IAlertService _alertService;

    public ReplenishmentService(IUnitOfWork uow, IReplenishmentEngine engine, IDateTimeProvider clock, IAlertService alertService)
    {
        _uow = uow;
        _engine = engine;
        _clock = clock;
        _alertService = alertService;
    }

    public async Task<int> GenerateRecommendationsAsync(CancellationToken ct = default)
    {
        var asOf = _clock.Today;
        var generated = 0;

        var atmIds = await _uow.ForecastResults.Query().Select(f => f.AtmId).Distinct().ToListAsync(ct);

        foreach (var atmId in atmIds)
        {
            var atm = await _uow.Atms.GetByIdAsync(atmId, ct);
            if (atm is null || atm.IsDeleted || atm.Status != AtmOperationalStatus.Active) continue;

            var forecastRows = await _uow.ForecastResults.Query()
                .Where(f => f.AtmId == atmId)
                .OrderBy(f => f.TargetDate)
                .ToListAsync(ct);

            if (forecastRows.Count == 0) continue;

            var latestTransaction = await _uow.Transactions.Query()
                .Where(t => t.AtmId == atmId)
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefaultAsync(ct);

            if (latestTransaction is null) continue;

            var recentWithdrawals = await _uow.Transactions.Query()
                .Where(t => t.AtmId == atmId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(30)
                .Select(t => t.WithdrawalAmount)
                .ToListAsync(ct);

            var forecastPoints = forecastRows
                .Select(f => new ForecastPoint(f.TargetDate, f.ForecastedWithdrawalAmount, f.ConfidenceScore, f.LowerBound ?? 0, f.UpperBound ?? 0))
                .ToList();

            var input = new ReplenishmentInput(
                atmId,
                latestTransaction.RemainingCash,
                atm.Capacity,
                atm.SafetyBufferAmount,
                forecastPoints,
                recentWithdrawals.Count > 0 ? recentWithdrawals.Average() : 0);

            var output = _engine.Recommend(input, asOf);

            var recommendation = new ReplenishmentRecommendation
            {
                AtmId = atmId,
                ForecastResultId = forecastRows[0].Id,
                CurrentCash = latestTransaction.RemainingCash,
                ForecastDemand = output.ForecastDemand,
                SafetyBuffer = atm.SafetyBufferAmount,
                AtmCapacity = atm.Capacity,
                RecommendedLoadAmount = output.RecommendedLoadAmount,
                Priority = output.Priority,
                ProjectedDepletionDate = output.ProjectedDepletionDate,
                RiskLevel = output.RiskLevel
            };

            await _uow.Recommendations.AddAsync(recommendation, ct);
            generated++;

            if (output.RiskLevel is RiskLevel.Critical or RiskLevel.High)
            {
                var severity = output.RiskLevel == RiskLevel.Critical ? AlertSeverity.Critical : AlertSeverity.High;
                await _alertService.RaiseAsync(atmId, AlertType.CashDepletion, severity,
                    $"{output.RiskLevel} cash-out risk: {atm.AtmCode}",
                    $"ATM {atm.AtmCode} is projected to deplete by {output.ProjectedDepletionDate:yyyy-MM-dd}. Recommended load: {output.RecommendedLoadAmount:C}.",
                    ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return generated;
    }

    public async Task<IReadOnlyList<ReplenishmentRecommendationDto>> GetActiveRecommendationsAsync(CancellationToken ct = default)
    {
        return await _uow.Recommendations.Query()
            .Include(r => r.Atm)
            .Where(r => !r.IsFulfilled)
            .OrderByDescending(r => r.RiskLevel)
            .ThenBy(r => r.ProjectedDepletionDate)
            .Select(r => new ReplenishmentRecommendationDto(
                r.Id, r.AtmId, r.Atm.AtmCode, r.Atm.AtmName,
                r.CurrentCash, r.ForecastDemand, r.RecommendedLoadAmount,
                r.Priority, r.RiskLevel, r.ProjectedDepletionDate, r.IsFulfilled))
            .ToListAsync(ct);
    }

    public async Task<Result<int>> RecordCashLoadAsync(RecordCashLoadRequest request, string? userId, CancellationToken ct = default)
    {
        var atm = await _uow.Atms.GetByIdAsync(request.AtmId, ct);
        if (atm is null || atm.IsDeleted)
        {
            return Result<int>.Failure("ATM not found.");
        }

        var latestTransaction = await _uow.Transactions.Query()
            .Where(t => t.AtmId == request.AtmId)
            .OrderByDescending(t => t.TransactionDate)
            .FirstOrDefaultAsync(ct);

        var cashBefore = latestTransaction?.RemainingCash ?? 0;

        ReplenishmentRecommendation? recommendation = null;
        if (request.RecommendationId.HasValue)
        {
            recommendation = await _uow.Recommendations.GetByIdAsync(request.RecommendationId.Value, ct);
        }

        var cashLoad = new AtmCashLoad
        {
            AtmId = request.AtmId,
            LoadDateUtc = DateTime.UtcNow,
            AmountLoaded = request.AmountLoaded,
            CashBeforeLoad = cashBefore,
            CashAfterLoad = Math.Min(atm.Capacity, cashBefore + request.AmountLoaded),
            Priority = recommendation?.Priority ?? ReplenishmentPriority.Routine,
            RecommendationId = recommendation?.Id,
            LoadedByUserId = userId,
            IsPlanned = request.IsPlanned,
            Notes = request.Notes,
            CreatedBy = userId
        };

        await _uow.CashLoads.AddAsync(cashLoad, ct);
        await _uow.SaveChangesAsync(ct);

        if (recommendation is not null)
        {
            recommendation.IsFulfilled = true;
            recommendation.FulfilledByCashLoadId = cashLoad.Id;
            _uow.Recommendations.Update(recommendation);
            await _uow.SaveChangesAsync(ct);
        }

        return Result<int>.Success(cashLoad.Id);
    }
}
