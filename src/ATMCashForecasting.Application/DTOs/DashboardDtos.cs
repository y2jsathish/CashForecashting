using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.DTOs;

public record DashboardKpiDto(
    int TotalAtms,
    int ActiveAtms,
    int CashOutRiskAtms,
    int ReplenishmentDueAtms,
    decimal ForecastAccuracyPercent,
    decimal TotalCashPosition);

public record RegionalCashSummaryDto(string RegionName, decimal TotalCash, int AtmCount, int AtRiskCount);

public record ForecastVsActualPointDto(DateOnly Date, decimal Forecast, decimal? Actual);

public record RiskDistributionDto(RiskLevel RiskLevel, int Count);
