using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

/// <summary>
/// One aggregated row per ATM per calendar day. Source rows are aggregated at import time
/// so the forecasting engine always operates over a dense daily time series.
/// </summary>
public class AtmTransaction : BaseEntity
{
    public int AtmId { get; set; }
    public AtmMaster Atm { get; set; } = null!;

    public DateOnly TransactionDate { get; set; }

    public int WithdrawalCount { get; set; }
    public decimal WithdrawalAmount { get; set; }
    public decimal DepositAmount { get; set; }

    /// <summary>Cash remaining in the ATM at end of day, as reported by the switch/EJ feed.</summary>
    public decimal RemainingCash { get; set; }

    /// <summary>Cash loaded into the ATM that day, if a replenishment occurred.</summary>
    public decimal CashLoaded { get; set; }

    public TransactionImportSource Source { get; set; }
    public ImportValidationStatus ValidationStatus { get; set; } = ImportValidationStatus.Valid;
    public string? ValidationNotes { get; set; }

    public Guid ImportBatchId { get; set; }
}
