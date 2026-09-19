using ATMCashForecasting.Domain.Common;

namespace ATMCashForecasting.Domain.Entities;

public class HolidayMaster : BaseEntity
{
    public DateOnly HolidayDate { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public int? RegionId { get; set; }
    public Region? Region { get; set; }

    /// <summary>Historical demand multiplier observed on/around this holiday, used to bias seasonal forecasts.</summary>
    public decimal DemandUpliftFactor { get; set; } = 1.0m;
}
