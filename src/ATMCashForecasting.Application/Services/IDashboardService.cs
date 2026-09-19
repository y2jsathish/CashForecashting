using ATMCashForecasting.Application.DTOs;

namespace ATMCashForecasting.Application.Services;

public interface IDashboardService
{
    Task<DashboardKpiDto> GetKpisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RegionalCashSummaryDto>> GetRegionalSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RiskDistributionDto>> GetRiskDistributionAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ForecastVsActualPointDto>> GetForecastVsActualAsync(int atmId, int days, CancellationToken ct = default);
}
