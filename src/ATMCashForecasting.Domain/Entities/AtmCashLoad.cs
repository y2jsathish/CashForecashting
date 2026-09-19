using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

/// <summary>A physical cash replenishment event, either recommended-and-executed or ad-hoc.</summary>
public class AtmCashLoad : BaseEntity
{
    public int AtmId { get; set; }
    public AtmMaster Atm { get; set; } = null!;

    public DateTime LoadDateUtc { get; set; }
    public decimal AmountLoaded { get; set; }
    public decimal CashBeforeLoad { get; set; }
    public decimal CashAfterLoad { get; set; }

    public ReplenishmentPriority Priority { get; set; }
    public int? RecommendationId { get; set; }
    public ReplenishmentRecommendation? Recommendation { get; set; }

    public string? LoadedByUserId { get; set; }
    public bool IsPlanned { get; set; }
    public string? Notes { get; set; }
}
