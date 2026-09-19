using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATMCashForecasting.Application.Services;

/// <summary>Module 7: aggregates cross-cutting KPIs and chart feeds for the executive/role dashboards.</summary>
public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IForecastService _forecastService;

    public DashboardService(IUnitOfWork uow, IDateTimeProvider clock, IForecastService forecastService)
    {
        _uow = uow;
        _clock = clock;
        _forecastService = forecastService;
    }

    public async Task<DashboardKpiDto> GetKpisAsync(CancellationToken ct = default)
    {
        var totalAtms = await _uow.Atms.Query().CountAsync(a => !a.IsDeleted, ct);
        var activeAtms = await _uow.Atms.Query().CountAsync(a => !a.IsDeleted && a.Status == AtmOperationalStatus.Active, ct);

        var cashOutRisk = await _uow.Recommendations.Query()
            .CountAsync(r => !r.IsFulfilled && (r.RiskLevel == RiskLevel.Critical || r.RiskLevel == RiskLevel.High), ct);

        var replenishmentDue = await _uow.Recommendations.Query().CountAsync(r => !r.IsFulfilled, ct);

        var accuracy = await _forecastService.GetOverallForecastAccuracyAsync(ct);

        var cutoff = _clock.Today.AddDays(-1);
        var totalCash = await _uow.Transactions.Query()
            .Where(t => t.TransactionDate >= cutoff)
            .GroupBy(t => t.AtmId)
            .Select(g => g.OrderByDescending(t => t.TransactionDate).First().RemainingCash)
            .SumAsync(ct);

        return new DashboardKpiDto(totalAtms, activeAtms, cashOutRisk, replenishmentDue, accuracy, totalCash);
    }

    public async Task<IReadOnlyList<RegionalCashSummaryDto>> GetRegionalSummaryAsync(CancellationToken ct = default)
    {
        var cutoff = _clock.Today.AddDays(-1);

        var latestCashByAtm = await _uow.Transactions.Query()
            .Where(t => t.TransactionDate >= cutoff)
            .GroupBy(t => t.AtmId)
            .Select(g => new { AtmId = g.Key, Cash = g.OrderByDescending(t => t.TransactionDate).First().RemainingCash })
            .ToListAsync(ct);

        var cashByAtmId = latestCashByAtm.ToDictionary(x => x.AtmId, x => x.Cash);

        var atRiskAtmIds = (await _uow.Recommendations.Query()
                .Where(r => !r.IsFulfilled && (r.RiskLevel == RiskLevel.Critical || r.RiskLevel == RiskLevel.High))
                .Select(r => r.AtmId)
                .ToListAsync(ct))
            .ToHashSet();

        var atms = await _uow.Atms.Query()
            .Include(a => a.Region)
            .Where(a => !a.IsDeleted)
            .Select(a => new { a.Id, a.RegionId, RegionName = a.Region.RegionName })
            .ToListAsync(ct);

        return atms
            .GroupBy(a => a.RegionName)
            .Select(g => new RegionalCashSummaryDto(
                g.Key,
                g.Sum(a => cashByAtmId.TryGetValue(a.Id, out var c) ? c : 0),
                g.Count(),
                g.Count(a => atRiskAtmIds.Contains(a.Id))))
            .OrderByDescending(r => r.TotalCash)
            .ToList();
    }

    public async Task<IReadOnlyList<RiskDistributionDto>> GetRiskDistributionAsync(CancellationToken ct = default)
    {
        var distribution = await _uow.Recommendations.Query()
            .Where(r => !r.IsFulfilled)
            .GroupBy(r => r.RiskLevel)
            .Select(g => new RiskDistributionDto(g.Key, g.Count()))
            .ToListAsync(ct);

        foreach (var level in Enum.GetValues<RiskLevel>())
        {
            if (!distribution.Any(d => d.RiskLevel == level))
            {
                distribution.Add(new RiskDistributionDto(level, 0));
            }
        }

        return distribution.OrderBy(d => d.RiskLevel).ToList();
    }

    public async Task<IReadOnlyList<ForecastVsActualPointDto>> GetForecastVsActualAsync(int atmId, int days, CancellationToken ct = default)
    {
        var since = _clock.Today.AddDays(-days);

        var history = await _uow.ForecastHistories.Query()
            .Where(f => f.AtmId == atmId && f.TargetDate >= since)
            .Select(f => new ForecastVsActualPointDto(f.TargetDate, f.ForecastedWithdrawalAmount, f.ActualWithdrawalAmount))
            .ToListAsync(ct);

        var current = await _uow.ForecastResults.Query()
            .Where(f => f.AtmId == atmId)
            .Select(f => new ForecastVsActualPointDto(f.TargetDate, f.ForecastedWithdrawalAmount, f.ActualWithdrawalAmount))
            .ToListAsync(ct);

        return history.Concat(current).OrderBy(p => p.Date).ToList();
    }
}
