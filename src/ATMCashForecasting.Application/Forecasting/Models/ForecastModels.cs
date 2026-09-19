using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Forecasting.Models;

/// <summary>One point of a daily historical time series fed into the engine.</summary>
public record HistoricalPoint(DateOnly Date, decimal WithdrawalAmount);

public record ForecastRequest(
    int AtmId,
    IReadOnlyList<HistoricalPoint> History,
    ForecastMethod Method,
    ForecastHorizon Horizon,
    DateOnly AsOfDate);

public record ForecastPoint(DateOnly TargetDate, decimal ForecastedAmount, decimal ConfidenceScore, decimal LowerBound, decimal UpperBound);

public record ForecastOutput(int AtmId, ForecastMethod Method, ForecastHorizon Horizon, IReadOnlyList<ForecastPoint> Points)
{
    public bool IsReliable => Points.Count > 0 && Points.Average(p => p.ConfidenceScore) >= 0.5m;
}
