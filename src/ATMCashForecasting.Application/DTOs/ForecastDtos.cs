using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.DTOs;

public record RunForecastRequest(IReadOnlyList<int>? AtmIds, ForecastMethod Method, ForecastHorizon Horizon);

public record ForecastResultDto(
    int AtmId,
    string AtmCode,
    ForecastMethod Method,
    ForecastHorizon Horizon,
    DateOnly TargetDate,
    decimal ForecastedWithdrawalAmount,
    decimal ConfidenceScore,
    decimal LowerBound,
    decimal UpperBound);

public record ForecastRunSummaryDto(
    Guid ForecastRunId,
    DateTime RunAtUtc,
    int AtmsProcessed,
    int AtmsFailed,
    TimeSpan Duration);

public record ForecastAccuracyPointDto(DateOnly TargetDate, decimal ForecastedAmount, decimal? ActualAmount, decimal? AbsolutePercentageError);
