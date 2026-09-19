using ATMCashForecasting.Application.Forecasting.Models;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Forecasting;

/// <summary>
/// Phase 1 statistical forecasting engine: Moving Average, Weighted Moving Average,
/// Trend (simple linear regression) and Seasonal (day-of-week index) methods.
/// All methods are pure functions of the supplied history so they are trivially unit-testable
/// and safe to run in parallel across thousands of ATMs.
/// </summary>
public class ForecastingEngine : IForecastingEngine
{
    private const int DefaultWindow = 7;

    public ForecastOutput Forecast(ForecastRequest request)
    {
        var history = request.History
            .Where(h => h.Date <= request.AsOfDate)
            .OrderBy(h => h.Date)
            .ToList();

        if (history.Count == 0)
        {
            return new ForecastOutput(request.AtmId, request.Method, request.Horizon, Array.Empty<ForecastPoint>());
        }

        var points = request.Method switch
        {
            ForecastMethod.MovingAverage => MovingAverage(history, request.AsOfDate, (int)request.Horizon),
            ForecastMethod.WeightedMovingAverage => WeightedMovingAverage(history, request.AsOfDate, (int)request.Horizon),
            ForecastMethod.TrendForecasting => TrendForecast(history, request.AsOfDate, (int)request.Horizon),
            ForecastMethod.SeasonalForecasting => SeasonalForecast(history, request.AsOfDate, (int)request.Horizon),
            _ => throw new NotSupportedException(
                $"Method '{request.Method}' is a Phase 2 ML method. Route it through IMLForecastProvider instead of ForecastingEngine.")
        };

        return new ForecastOutput(request.AtmId, request.Method, request.Horizon, points);
    }

    private static List<ForecastPoint> MovingAverage(List<HistoricalPoint> history, DateOnly asOf, int horizonDays)
    {
        var window = Math.Min(DefaultWindow, history.Count);
        var recent = history.TakeLast(window).Select(h => h.WithdrawalAmount).ToList();
        var average = recent.Average();
        var confidence = ConfidenceFromVariance(recent, average);

        return BuildFlatPoints(asOf, horizonDays, average, confidence);
    }

    private static List<ForecastPoint> WeightedMovingAverage(List<HistoricalPoint> history, DateOnly asOf, int horizonDays)
    {
        var window = Math.Min(DefaultWindow, history.Count);
        var recent = history.TakeLast(window).Select(h => h.WithdrawalAmount).ToList();

        // Linearly increasing weights so the most recent day carries the most weight: 1,2,3,...,window
        var weightSum = window * (window + 1) / 2m;
        decimal weighted = 0;
        for (var i = 0; i < recent.Count; i++)
        {
            weighted += recent[i] * (i + 1);
        }
        var wma = weighted / weightSum;
        var confidence = ConfidenceFromVariance(recent, recent.Average());

        return BuildFlatPoints(asOf, horizonDays, wma, confidence);
    }

    private static List<ForecastPoint> TrendForecast(List<HistoricalPoint> history, DateOnly asOf, int horizonDays)
    {
        // Ordinary least squares over up to the last 30 days: y = a + b*x, x = day index.
        var window = Math.Min(30, history.Count);
        var recent = history.TakeLast(window).ToList();

        var n = recent.Count;
        var xs = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        var ys = recent.Select(h => (double)h.WithdrawalAmount).ToArray();

        var xMean = xs.Average();
        var yMean = ys.Average();

        var numerator = 0.0;
        var denominator = 0.0;
        for (var i = 0; i < n; i++)
        {
            numerator += (xs[i] - xMean) * (ys[i] - yMean);
            denominator += (xs[i] - xMean) * (xs[i] - xMean);
        }

        var slope = denominator == 0 ? 0 : numerator / denominator;
        var intercept = yMean - slope * xMean;

        var residuals = new double[n];
        for (var i = 0; i < n; i++)
        {
            residuals[i] = ys[i] - (intercept + slope * xs[i]);
        }
        var stdError = Math.Sqrt(residuals.Sum(r => r * r) / Math.Max(1, n - 2));
        var confidence = ConfidenceFromStdError(stdError, yMean);

        var points = new List<ForecastPoint>();
        for (var d = 1; d <= horizonDays; d++)
        {
            var x = n - 1 + d;
            var predicted = (decimal)Math.Max(0, intercept + slope * x);
            var margin = (decimal)stdError * 1.28m; // ~80% interval
            points.Add(new ForecastPoint(
                asOf.AddDays(d),
                Math.Round(predicted, 2),
                confidence,
                Math.Max(0, Math.Round(predicted - margin, 2)),
                Math.Round(predicted + margin, 2)));
        }
        return points;
    }

    private static List<ForecastPoint> SeasonalForecast(List<HistoricalPoint> history, DateOnly asOf, int horizonDays)
    {
        // Day-of-week seasonal index against the overall mean, using up to the last 90 days.
        var window = Math.Min(90, history.Count);
        var recent = history.TakeLast(window).ToList();
        var overallMean = recent.Average(h => h.WithdrawalAmount);

        var byDow = recent
            .GroupBy(h => h.Date.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Average(h => h.WithdrawalAmount));

        var variances = recent.Select(h => (double)(h.WithdrawalAmount - overallMean)).ToArray();
        var stdDev = Math.Sqrt(variances.Sum(v => v * v) / Math.Max(1, recent.Count - 1));
        var confidence = ConfidenceFromStdError(stdDev, (double)overallMean);

        var points = new List<ForecastPoint>();
        for (var d = 1; d <= horizonDays; d++)
        {
            var targetDate = asOf.AddDays(d);
            var seasonalMean = byDow.TryGetValue(targetDate.DayOfWeek, out var v) ? v : overallMean;
            var index = overallMean == 0 ? 1m : seasonalMean / overallMean;
            var predicted = Math.Max(0, overallMean * index);
            var margin = (decimal)stdDev;
            points.Add(new ForecastPoint(
                targetDate,
                Math.Round(predicted, 2),
                confidence,
                Math.Max(0, Math.Round(predicted - margin, 2)),
                Math.Round(predicted + margin, 2)));
        }
        return points;
    }

    private static List<ForecastPoint> BuildFlatPoints(DateOnly asOf, int horizonDays, decimal amount, decimal confidence)
    {
        var margin = amount * (1 - confidence) * 0.5m;
        var points = new List<ForecastPoint>();
        for (var d = 1; d <= horizonDays; d++)
        {
            points.Add(new ForecastPoint(
                asOf.AddDays(d),
                Math.Round(amount, 2),
                confidence,
                Math.Max(0, Math.Round(amount - margin, 2)),
                Math.Round(amount + margin, 2)));
        }
        return points;
    }

    /// <summary>Coefficient-of-variation based confidence: tighter spread around the mean => higher confidence.</summary>
    private static decimal ConfidenceFromVariance(IReadOnlyList<decimal> values, decimal mean)
    {
        if (values.Count < 2 || mean == 0) return 0.5m;

        var variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
        var stdDev = (decimal)Math.Sqrt((double)variance);
        return ConfidenceFromStdError((double)stdDev, (double)mean);
    }

    private static decimal ConfidenceFromStdError(double stdError, double mean)
    {
        if (mean == 0) return 0.5m;
        var coefficientOfVariation = Math.Abs(stdError / mean);
        // CoV of 0 => 99% confidence; CoV of 1+ => floor at 30%.
        var confidence = 0.99 - Math.Min(0.69, coefficientOfVariation);
        return (decimal)Math.Round(Math.Max(0.30, confidence), 2);
    }
}
