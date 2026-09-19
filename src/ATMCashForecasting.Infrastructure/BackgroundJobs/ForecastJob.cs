using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Services;
using ATMCashForecasting.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ATMCashForecasting.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire-recurring job (Module 4 + Module 5 automation): runs the forecasting engine for every
/// active ATM across all five horizons, then regenerates replenishment recommendations. Scheduled
/// nightly via Program.cs (RecurringJob.AddOrUpdate) so operations users see fresh numbers each morning.
/// </summary>
public class ForecastJob
{
    private readonly IForecastService _forecastService;
    private readonly IReplenishmentService _replenishmentService;
    private readonly ILogger<ForecastJob> _logger;

    public ForecastJob(IForecastService forecastService, IReplenishmentService replenishmentService, ILogger<ForecastJob> logger)
    {
        _forecastService = forecastService;
        _replenishmentService = replenishmentService;
        _logger = logger;
    }

    public async Task RunNightlyForecastAsync()
    {
        _logger.LogInformation("Nightly forecast job starting");

        foreach (var horizon in new[] { ForecastHorizon.NextDay, ForecastHorizon.ThreeDays, ForecastHorizon.SevenDays, ForecastHorizon.FifteenDays, ForecastHorizon.ThirtyDays })
        {
            var summary = await _forecastService.RunForecastAsync(new RunForecastRequest(null, ForecastMethod.WeightedMovingAverage, horizon));
            _logger.LogInformation(
                "Forecast run {RunId} ({Horizon}) processed {Processed} ATMs, {Failed} failed, took {Duration}",
                summary.ForecastRunId, horizon, summary.AtmsProcessed, summary.AtmsFailed, summary.Duration);
        }

        var recommendationCount = await _replenishmentService.GenerateRecommendationsAsync();
        _logger.LogInformation("Generated {Count} replenishment recommendations", recommendationCount);
    }
}
