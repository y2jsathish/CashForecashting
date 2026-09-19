using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Domain.Entities;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Services;

/// <summary>Writes immutable audit trail rows (Module 10). Entries are append-only: no update/delete path exists.</summary>
public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AuditService(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task LogAsync(AuditAction action, string entityName, string? entityId, string? oldValues, string? newValues, string? userId, CancellationToken ct = default)
    {
        await _uow.AuditLogs.AddAsync(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = userId ?? _currentUser.UserId,
            UserName = _currentUser.UserName,
            IpAddress = _currentUser.IpAddress,
            IsSuccess = true
        }, ct);

        await _uow.SaveChangesAsync(ct);
    }
}
