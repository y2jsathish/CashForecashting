using ATMCashForecasting.Application.DTOs;

namespace ATMCashForecasting.Application.Services;

public interface IForecastService
{
    Task<ForecastRunSummaryDto> RunForecastAsync(RunForecastRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ForecastResultDto>> GetLatestForecastAsync(int atmId, CancellationToken ct = default);
    Task<IReadOnlyList<ForecastAccuracyPointDto>> GetAccuracyHistoryAsync(int atmId, int days, CancellationToken ct = default);
    Task<decimal> GetOverallForecastAccuracyAsync(CancellationToken ct = default);
}
