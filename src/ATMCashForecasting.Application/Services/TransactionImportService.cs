using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Validation;
using ATMCashForecasting.Domain.Entities;
using ATMCashForecasting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATMCashForecasting.Application.Services;

/// <summary>
/// Module 3: ingests CSV/Excel/API rows, runs them through the validation engine, resolves ATM
/// codes, and persists one aggregated AtmTransaction per ATM per day. Rejected rows never touch
/// the transaction table; a per-row result is always returned so the UI can show exactly what failed.
/// </summary>
public class TransactionImportService : ITransactionImportService
{
    private readonly IUnitOfWork _uow;
    private readonly TransactionImportRowValidator _validator;
    private readonly IAuditService _auditService;
    private readonly IAlertService _alertService;

    public TransactionImportService(IUnitOfWork uow, IAuditService auditService, IAlertService alertService)
    {
        _uow = uow;
        _validator = new TransactionImportRowValidator();
        _auditService = auditService;
        _alertService = alertService;
    }

    public async Task<TransactionImportResultDto> ImportAsync(IReadOnlyList<TransactionImportRowDto> rows, TransactionImportSource source, string? userId, CancellationToken ct = default)
    {
        var batchId = Guid.NewGuid();
        var rowResults = new List<TransactionImportRowResultDto>();

        var atmCodeToId = await _uow.Atms.Query()
            .Where(a => !a.IsDeleted)
            .ToDictionaryAsync(a => a.AtmCode, a => a.Id, StringComparer.OrdinalIgnoreCase, ct);

        var toInsert = new List<AtmTransaction>();
        var rowNumber = 0;

        foreach (var row in rows)
        {
            rowNumber++;
            var errors = new List<string>();

            var validation = await _validator.ValidateAsync(row, ct);
            if (!validation.IsValid)
            {
                errors.AddRange(validation.Errors.Select(e => e.ErrorMessage));
            }

            if (!atmCodeToId.TryGetValue(row.AtmCode, out var atmId))
            {
                errors.Add($"ATM code '{row.AtmCode}' was not found in the ATM master.");
            }

            var isValid = errors.Count == 0;
            rowResults.Add(new TransactionImportRowResultDto(rowNumber, row.AtmCode, isValid, errors));

            if (isValid)
            {
                toInsert.Add(new AtmTransaction
                {
                    AtmId = atmId,
                    TransactionDate = row.TransactionDate,
                    WithdrawalCount = row.WithdrawalCount,
                    WithdrawalAmount = row.WithdrawalAmount,
                    DepositAmount = row.DepositAmount,
                    RemainingCash = row.RemainingCash,
                    CashLoaded = row.CashLoaded,
                    Source = source,
                    ValidationStatus = ImportValidationStatus.Valid,
                    ImportBatchId = batchId,
                    CreatedBy = userId
                });
            }
        }

        if (toInsert.Count > 0)
        {
            await _uow.Transactions.AddRangeAsync(toInsert, ct);
            await _uow.SaveChangesAsync(ct);
        }

        await _auditService.LogAsync(AuditAction.DataUpload, nameof(AtmTransaction), batchId.ToString(),
            null, $"{toInsert.Count}/{rows.Count} rows accepted", userId, ct);

        var rejectedCount = rows.Count - toInsert.Count;
        if (rejectedCount > 0 && rejectedCount == rows.Count)
        {
            await _alertService.RaiseAsync(null, AlertType.DataImportFailure, AlertSeverity.High,
                "Transaction import batch fully rejected",
                $"Batch {batchId} rejected all {rows.Count} rows during validation. Source: {source}.", ct);
        }

        return new TransactionImportResultDto(batchId, rows.Count, toInsert.Count, rejectedCount, rowResults);
    }
}
