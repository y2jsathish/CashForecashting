using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

/// <summary>
/// Immutable audit trail row. No BaseEntity (no soft-delete, no modified fields) by design —
/// audit rows must never be edited or deleted, only appended and, per retention policy, archived.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? Details { get; set; }
}
