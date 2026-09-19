using ATMCashForecasting.Application.Risk;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Replenishment;

/// <summary>
/// Implements the formula mandated by the business spec:
///   Recommended Load = Forecast Demand + Safety Buffer - Current Cash
/// clamped to [0, Capacity - CurrentCash] so a recommendation never exceeds physical capacity
/// or goes negative. Depletion date/time is derived by walking the forecast curve forward from
/// the current cash balance; risk level is delegated to IRiskClassifier per the 24/48/72h bands.
/// </summary>
public class ReplenishmentEngine : IReplenishmentEngine
{
    private readonly IRiskClassifier _riskClassifier;

    public ReplenishmentEngine(IRiskClassifier riskClassifier)
    {
        _riskClassifier = riskClassifier;
    }

    public ReplenishmentOutput Recommend(ReplenishmentInput input, DateOnly asOfDate)
    {
        if (input.ForecastPoints.Count == 0)
        {
            throw new InvalidOperationException("Cannot compute a replenishment recommendation without forecast points.");
        }

        var forecastDemand = input.ForecastPoints.Sum(p => p.ForecastedAmount);

        var rawRecommendedLoad = forecastDemand + input.SafetyBuffer - input.CurrentCash;
        var maxLoadable = Math.Max(0, input.AtmCapacity - input.CurrentCash);
        var recommendedLoad = Math.Clamp(rawRecommendedLoad, 0, maxLoadable);

        var (depletionDate, hoursToDepletion) = ProjectDepletion(input, asOfDate);
        var riskLevel = _riskClassifier.Classify(hoursToDepletion);
        var priority = MapPriority(riskLevel);

        return new ReplenishmentOutput(
            input.AtmId,
            forecastDemand,
            Math.Round(recommendedLoad, 2),
            priority,
            riskLevel,
            depletionDate);
    }

    private static (DateOnly DepletionDate, double HoursToDepletion) ProjectDepletion(ReplenishmentInput input, DateOnly asOfDate)
    {
        var remaining = input.CurrentCash;
        var dailyRunRate = input.AverageDailyWithdrawal > 0
            ? input.AverageDailyWithdrawal
            : (input.ForecastPoints.Count > 0 ? input.ForecastPoints[0].ForecastedAmount : 0);

        foreach (var point in input.ForecastPoints.OrderBy(p => p.TargetDate))
        {
            var dayDemand = point.ForecastedAmount > 0 ? point.ForecastedAmount : dailyRunRate;

            if (dayDemand <= 0)
            {
                continue;
            }

            if (remaining <= dayDemand)
            {
                // Interpolate the fraction of the day at which the balance crosses zero.
                var fractionOfDay = remaining <= 0 ? 0m : Math.Clamp(remaining / dayDemand, 0m, 1m);
                var daysFromAsOf = point.TargetDate.DayNumber - asOfDate.DayNumber - 1 + (double)fractionOfDay;
                var hours = daysFromAsOf * 24.0;
                return (point.TargetDate, Math.Max(0, hours));
            }

            remaining -= dayDemand;
        }

        // Cash outlasts the whole forecast horizon: report the horizon end with a >72h (Low risk) marker.
        var lastPoint = input.ForecastPoints.OrderBy(p => p.TargetDate).Last();
        var horizonHours = (lastPoint.TargetDate.DayNumber - asOfDate.DayNumber) * 24.0;
        return (lastPoint.TargetDate, Math.Max(72.0, horizonHours));
    }

    private static ReplenishmentPriority MapPriority(RiskLevel riskLevel) => riskLevel switch
    {
        RiskLevel.Critical => ReplenishmentPriority.Emergency,
        RiskLevel.High => ReplenishmentPriority.Urgent,
        RiskLevel.Medium => ReplenishmentPriority.Scheduled,
        _ => ReplenishmentPriority.Routine
    };
}
