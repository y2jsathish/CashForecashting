using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Services;

public interface IAlertService
{
    Task<IReadOnlyList<AlertDto>> GetOpenAlertsAsync(CancellationToken ct = default);
    Task RaiseAsync(int? atmId, AlertType type, AlertSeverity severity, string title, string message, CancellationToken ct = default);
    Task AcknowledgeAsync(int alertId, string? userId, CancellationToken ct = default);
    Task ResolveAsync(int alertId, CancellationToken ct = default);
}
