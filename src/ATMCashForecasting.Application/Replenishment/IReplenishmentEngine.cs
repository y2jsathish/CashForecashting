using ATMCashForecasting.Application.Forecasting.Models;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Replenishment;

public record ReplenishmentInput(
    int AtmId,
    decimal CurrentCash,
    decimal AtmCapacity,
    decimal SafetyBuffer,
    IReadOnlyList<ForecastPoint> ForecastPoints,
    decimal AverageDailyWithdrawal);

public record ReplenishmentOutput(
    int AtmId,
    decimal ForecastDemand,
    decimal RecommendedLoadAmount,
    ReplenishmentPriority Priority,
    RiskLevel RiskLevel,
    DateOnly ProjectedDepletionDate);

public interface IReplenishmentEngine
{
    ReplenishmentOutput Recommend(ReplenishmentInput input, DateOnly asOfDate);
}
