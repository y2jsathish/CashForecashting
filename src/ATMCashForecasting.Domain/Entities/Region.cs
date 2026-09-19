using ATMCashForecasting.Domain.Common;

namespace ATMCashForecasting.Domain.Entities;

public class Region : BaseEntity
{
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public int? RegionalManagerUserId { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
