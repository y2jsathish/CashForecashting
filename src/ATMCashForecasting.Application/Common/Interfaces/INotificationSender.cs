using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Common.Interfaces;

/// <summary>Abstraction over the three notification channels (Email/SMS/Teams). Implemented in Infrastructure.</summary>
public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken ct = default);
}
