using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

public class ReplenishmentRecommendation : BaseEntity
{
    public int AtmId { get; set; }
    public AtmMaster Atm { get; set; } = null!;

    public int ForecastResultId { get; set; }
    public ForecastResult ForecastResult { get; set; } = null!;

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public decimal CurrentCash { get; set; }
    public decimal ForecastDemand { get; set; }
    public decimal SafetyBuffer { get; set; }
    public decimal AtmCapacity { get; set; }

    /// <summary>RecommendedLoad = ForecastDemand + SafetyBuffer - CurrentCash (clamped to [0, Capacity - CurrentCash]).</summary>
    public decimal RecommendedLoadAmount { get; set; }

    public ReplenishmentPriority Priority { get; set; }
    public DateOnly ProjectedDepletionDate { get; set; }
    public RiskLevel RiskLevel { get; set; }

    public bool IsFulfilled { get; set; }
    public int? FulfilledByCashLoadId { get; set; }
}
