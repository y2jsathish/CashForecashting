using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Services;

public interface IAuditService
{
    Task LogAsync(AuditAction action, string entityName, string? entityId, string? oldValues, string? newValues, string? userId, CancellationToken ct = default);
}
