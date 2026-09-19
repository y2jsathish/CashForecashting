using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.DTOs;

public record ReplenishmentRecommendationDto(
    int Id,
    int AtmId,
    string AtmCode,
    string AtmName,
    decimal CurrentCash,
    decimal ForecastDemand,
    decimal RecommendedLoadAmount,
    ReplenishmentPriority Priority,
    RiskLevel RiskLevel,
    DateOnly ProjectedDepletionDate,
    bool IsFulfilled);

public record RecordCashLoadRequest(int AtmId, decimal AmountLoaded, int? RecommendationId, bool IsPlanned, string? Notes);
