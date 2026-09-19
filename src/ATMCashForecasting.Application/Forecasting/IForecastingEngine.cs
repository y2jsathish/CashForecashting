using ATMCashForecasting.Application.Forecasting.Models;

namespace ATMCashForecasting.Application.Forecasting;

/// <summary>
/// Phase 1 statistical forecasting engine. Phase 2 (ML.NET / Prophet / XGBoost / LSTM) plugs in via
/// the same contract — see IMLForecastProvider and docs/ROADMAP.md.
/// </summary>
public interface IForecastingEngine
{
    ForecastOutput Forecast(ForecastRequest request);
}
