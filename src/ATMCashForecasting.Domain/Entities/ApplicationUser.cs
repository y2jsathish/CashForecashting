using Microsoft.AspNetCore.Identity;

namespace ATMCashForecasting.Domain.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? RegionId { get; set; }
    public Region? Region { get; set; }
    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public bool IsMfaEnabled { get; set; }
    public string? MfaSecretKey { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? PasswordChangedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ApplicationRole : IdentityRole<int>
{
    public string? Description { get; set; }
}
