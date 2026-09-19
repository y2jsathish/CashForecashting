using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

public class Alert : BaseEntity
{
    public int? AtmId { get; set; }
    public AtmMaster? Atm { get; set; }

    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Open;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    public ICollection<NotificationLog> Notifications { get; set; } = new List<NotificationLog>();
}
