using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

public class NotificationLog : BaseEntity
{
    public int AlertId { get; set; }
    public Alert Alert { get; set; } = null!;

    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public DateTime? SentAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
