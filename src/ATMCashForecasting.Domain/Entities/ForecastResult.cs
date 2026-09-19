using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

/// <summary>The current, actionable forecast for a given ATM + target date. Superseded rows move to ForecastHistory.</summary>
public class ForecastResult : BaseEntity
{
    public int AtmId { get; set; }
    public AtmMaster Atm { get; set; } = null!;

    public Guid ForecastRunId { get; set; }
    public DateTime RunAtUtc { get; set; } = DateTime.UtcNow;

    public ForecastMethod Method { get; set; }
    public ForecastHorizon Horizon { get; set; }
    public DateOnly TargetDate { get; set; }

    public decimal ForecastedWithdrawalAmount { get; set; }
    public decimal ConfidenceScore { get; set; }
    public decimal? LowerBound { get; set; }
    public decimal? UpperBound { get; set; }

    public decimal? ActualWithdrawalAmount { get; set; }
    public decimal? AbsolutePercentageError { get; set; }

    public ICollection<ReplenishmentRecommendation> Recommendations { get; set; } = new List<ReplenishmentRecommendation>();
}
