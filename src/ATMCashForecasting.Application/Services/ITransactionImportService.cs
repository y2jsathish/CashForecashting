using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Services;

public interface ITransactionImportService
{
    Task<TransactionImportResultDto> ImportAsync(IReadOnlyList<TransactionImportRowDto> rows, TransactionImportSource source, string? userId, CancellationToken ct = default);
}
