using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.DTOs;

public record AlertDto(
    int Id,
    int? AtmId,
    string? AtmCode,
    AlertType AlertType,
    AlertSeverity Severity,
    AlertStatus Status,
    string Title,
    string Message,
    DateTime TriggeredAtUtc);

public record AcknowledgeAlertRequest(int AlertId);
