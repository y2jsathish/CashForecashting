using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

public class AtmStatusHistory : BaseEntity
{
    public int AtmId { get; set; }
    public AtmMaster Atm { get; set; } = null!;

    public AtmOperationalStatus PreviousStatus { get; set; }
    public AtmOperationalStatus NewStatus { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
