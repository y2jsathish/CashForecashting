using ATMCashForecasting.Application.Forecasting;
using ATMCashForecasting.Application.Forecasting.Models;
using ATMCashForecasting.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ATMCashForecasting.Tests;

public class ForecastingEngineTests
{
    private readonly ForecastingEngine _engine = new();

    [Fact]
    public void MovingAverage_WithConstantHistory_ForecastsTheSameConstantWithHighConfidence()
    {
        var asOf = new DateOnly(2026, 1, 10);
        var history = Enumerable.Range(1, 7)
            .Select(i => new HistoricalPoint(asOf.AddDays(-i), 1000m))
            .ToList();

        var output = _engine.Forecast(new ForecastRequest(1, history, ForecastMethod.MovingAverage, ForecastHorizon.SevenDays, asOf));

        output.Points.Should().HaveCount(7);
        output.Points.Should().OnlyContain(p => p.ForecastedAmount == 1000m);
        output.Points.Average(p => p.ConfidenceScore).Should().BeGreaterThan(0.9m);
    }

    [Fact]
    public void WeightedMovingAverage_WeightsRecentDaysMoreHeavilyThanSimpleAverage()
    {
        var asOf = new DateOnly(2026, 1, 10);
        // Rising sequence: 100, 200, 300, ..., 700 (most recent day = 700).
        var history = Enumerable.Range(1, 7)
            .Select(i => new HistoricalPoint(asOf.AddDays(-(7 - i)), i * 100m))
            .ToList();

        var output = _engine.Forecast(new ForecastRequest(1, history, ForecastMethod.WeightedMovingAverage, ForecastHorizon.NextDay, asOf));

        var simpleAverage = history.Average(h => h.WithdrawalAmount); // 400
        output.Points.Single().ForecastedAmount.Should().BeGreaterThan(simpleAverage);
    }

    [Fact]
    public void TrendForecasting_ExtrapolatesALinearSeriesForward()
    {
        var asOf = new DateOnly(2026, 1, 10);
        // y = 100 + 10x for x = 0..9 (oldest to newest), so tomorrow (x=10) should be ~200.
        var history = Enumerable.Range(0, 10)
            .Select(x => new HistoricalPoint(asOf.AddDays(-(9 - x)), 100m + 10m * x))
            .ToList();

        var output = _engine.Forecast(new ForecastRequest(1, history, ForecastMethod.TrendForecasting, ForecastHorizon.NextDay, asOf));

        output.Points.Single().ForecastedAmount.Should().BeApproximately(200m, 1m);
        output.Points.Single().ConfidenceScore.Should().BeGreaterThan(0.9m); // near-perfect linear fit
    }

    [Fact]
    public void SeasonalForecasting_ProjectsHigherDemandOnTheHistoricallyHeavierDayOfWeek()
    {
        var asOf = new DateOnly(2026, 1, 4); // a Sunday
        var history = new List<HistoricalPoint>();

        // 12 weeks of history: Mondays withdraw 2000, every other day withdraws 1000.
        for (var i = 1; i <= 84; i++)
        {
            var date = asOf.AddDays(-i);
            var amount = date.DayOfWeek == DayOfWeek.Monday ? 2000m : 1000m;
            history.Add(new HistoricalPoint(date, amount));
        }

        var output = _engine.Forecast(new ForecastRequest(1, history, ForecastMethod.SeasonalForecasting, ForecastHorizon.SevenDays, asOf));

        var monday = output.Points.Single(p => p.TargetDate.DayOfWeek == DayOfWeek.Monday);
        var tuesday = output.Points.Single(p => p.TargetDate.DayOfWeek == DayOfWeek.Tuesday);

        monday.ForecastedAmount.Should().BeGreaterThan(tuesday.ForecastedAmount);
    }

    [Fact]
    public void Forecast_WithNoHistory_ReturnsEmptyOutput()
    {
        var output = _engine.Forecast(new ForecastRequest(1, Array.Empty<HistoricalPoint>(), ForecastMethod.MovingAverage, ForecastHorizon.NextDay, DateOnly.FromDateTime(DateTime.UtcNow)));

        output.Points.Should().BeEmpty();
    }

    [Fact]
    public void Forecast_WithMlMethod_ThrowsNotSupported_BecausePhase2ModelsAreNotWiredIntoThisEngine()
    {
        var asOf = new DateOnly(2026, 1, 10);
        var history = new[] { new HistoricalPoint(asOf.AddDays(-1), 100m) };

        var act = () => _engine.Forecast(new ForecastRequest(1, history, ForecastMethod.Prophet, ForecastHorizon.NextDay, asOf));

        act.Should().Throw<NotSupportedException>();
    }
}
