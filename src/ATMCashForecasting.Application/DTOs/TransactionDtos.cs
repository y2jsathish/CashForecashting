namespace ATMCashForecasting.Application.DTOs;

public record TransactionImportRowDto(
    string AtmCode,
    DateOnly TransactionDate,
    int WithdrawalCount,
    decimal WithdrawalAmount,
    decimal DepositAmount,
    decimal RemainingCash,
    decimal CashLoaded);

public record TransactionImportRowResultDto(
    int RowNumber,
    string AtmCode,
    bool IsValid,
    IReadOnlyList<string> Errors);

public record TransactionImportResultDto(
    Guid BatchId,
    int TotalRows,
    int AcceptedRows,
    int RejectedRows,
    IReadOnlyList<TransactionImportRowResultDto> RowResults);
