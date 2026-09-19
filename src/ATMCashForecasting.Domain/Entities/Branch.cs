using ATMCashForecasting.Domain.Common;

namespace ATMCashForecasting.Domain.Entities;

public class Branch : BaseEntity
{
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;
    public int? BranchManagerUserId { get; set; }
    public string Address { get; set; } = string.Empty;

    public ICollection<AtmMaster> Atms { get; set; } = new List<AtmMaster>();
}
