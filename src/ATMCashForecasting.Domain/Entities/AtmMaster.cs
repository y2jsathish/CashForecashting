using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

public class AtmMaster : BaseEntity
{
    public string AtmCode { get; set; } = string.Empty;
    public string AtmName { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;

    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public AtmType AtmType { get; set; }
    public string Currency { get; set; } = "USD";

    /// <summary>Maximum physical cash capacity of the ATM cassettes, in currency units.</summary>
    public decimal Capacity { get; set; }

    /// <summary>Minimum cash level below which the ATM is considered at risk even before demand is netted out.</summary>
    public decimal SafetyBufferAmount { get; set; }

    public AtmOperationalStatus Status { get; set; } = AtmOperationalStatus.Active;

    public DateTime? InstalledDate { get; set; }
    public DateTime? DecommissionedDate { get; set; }

    public ICollection<AtmTransaction> Transactions { get; set; } = new List<AtmTransaction>();
    public ICollection<AtmCashLoad> CashLoads { get; set; } = new List<AtmCashLoad>();
    public ICollection<AtmStatusHistory> StatusHistory { get; set; } = new List<AtmStatusHistory>();
    public ICollection<ForecastResult> ForecastResults { get; set; } = new List<ForecastResult>();
}
