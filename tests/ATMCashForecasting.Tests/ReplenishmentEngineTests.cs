using ATMCashForecasting.Application.Forecasting.Models;
using ATMCashForecasting.Application.Replenishment;
using ATMCashForecasting.Application.Risk;
using ATMCashForecasting.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ATMCashForecasting.Tests;

public class ReplenishmentEngineTests
{
    private readonly ReplenishmentEngine _engine = new(new RiskClassifier());
    private readonly DateOnly _asOf = new(2026, 1, 10);

    private static ForecastPoint Point(DateOnly date, decimal amount) => new(date, amount, 0.8m, amount * 0.8m, amount * 1.2m);

    [Fact]
    public void Recommend_AppliesTheMandatedFormula_ForecastDemandPlusSafetyBufferMinusCurrentCash()
    {
        var input = new ReplenishmentInput(
            AtmId: 1,
            CurrentCash: 1000m,
            AtmCapacity: 10000m,
            SafetyBuffer: 500m,
            ForecastPoints: new[]
            {
                Point(_asOf.AddDays(1), 1000m),
                Point(_asOf.AddDays(2), 1000m)
            },
            AverageDailyWithdrawal: 1000m);

        var output = _engine.Recommend(input, _asOf);

        output.ForecastDemand.Should().Be(2000m);
        output.RecommendedLoadAmount.Should().Be(1500m); // demand(2000) + buffer(500) - current(1000) = 1500
    }

    [Fact]
    public void Recommend_ClampsTheRecommendedLoadToWhatPhysicallyFitsInTheAtm()
    {
        var input = new ReplenishmentInput(
            AtmId: 1,
            CurrentCash: 100m,
            AtmCapacity: 1200m,
            SafetyBuffer: 5000m,
            ForecastPoints: new[] { Point(_asOf.AddDays(1), 10000m) },
            AverageDailyWithdrawal: 10000m);

        var output = _engine.Recommend(input, _asOf);

        // Raw formula would be 10000 + 5000 - 100 = 14900, but only 1200 - 100 = 1100 physically fits.
        output.RecommendedLoadAmount.Should().Be(1100m);
    }

    [Fact]
    public void Recommend_NeverReturnsANegativeLoad_WhenCashAlreadyExceedsDemandPlusBuffer()
    {
        var input = new ReplenishmentInput(
            AtmId: 1,
            CurrentCash: 50000m,
            AtmCapacity: 100000m,
            SafetyBuffer: 1000m,
            ForecastPoints: new[] { Point(_asOf.AddDays(1), 500m) },
            AverageDailyWithdrawal: 500m);

        var output = _engine.Recommend(input, _asOf);

        output.RecommendedLoadAmount.Should().Be(0m);
    }

    [Fact]
    public void Recommend_ClassifiesAsCritical_WhenCashRunsOutWithinHoursOfTheForecastHorizon()
    {
        var input = new ReplenishmentInput(
            AtmId: 1,
            CurrentCash: 50m,
            AtmCapacity: 10000m,
            SafetyBuffer: 1000m,
            ForecastPoints: new[] { Point(_asOf.AddDays(1), 1000m) },
            AverageDailyWithdrawal: 1000m);

        var output = _engine.Recommend(input, _asOf);

        output.RiskLevel.Should().Be(RiskLevel.Critical);
        output.Priority.Should().Be(ReplenishmentPriority.Emergency);
    }

    [Fact]
    public void Recommend_ClassifiesAsLow_WhenCashOutlastsTheEntireForecastHorizon()
    {
        var input = new ReplenishmentInput(
            AtmId: 1,
            CurrentCash: 1_000_000m,
            AtmCapacity: 2_000_000m,
            SafetyBuffer: 1000m,
            ForecastPoints: new[]
            {
                Point(_asOf.AddDays(1), 1000m),
                Point(_asOf.AddDays(2), 1000m),
                Point(_asOf.AddDays(3), 1000m)
            },
            AverageDailyWithdrawal: 1000m);

        var output = _engine.Recommend(input, _asOf);

        output.RiskLevel.Should().Be(RiskLevel.Low);
        output.Priority.Should().Be(ReplenishmentPriority.Routine);
    }

    [Fact]
    public void Recommend_ThrowsWhenNoForecastPointsAreSupplied()
    {
        var input = new ReplenishmentInput(1, 100m, 1000m, 100m, Array.Empty<ForecastPoint>(), 0m);

        var act = () => _engine.Recommend(input, _asOf);

        act.Should().Throw<InvalidOperationException>();
    }
}
