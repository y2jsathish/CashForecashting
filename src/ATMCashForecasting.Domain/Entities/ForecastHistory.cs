using ATMCashForecasting.Domain.Common;
using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Domain.Entities;

/// <summary>Append-only archive of every forecast ever produced, used for accuracy tracking (MAPE/WAPE trends).</summary>
public class ForecastHistory : BaseEntity
{
    public int AtmId { get; set; }
    public AtmMaster Atm { get; set; } = null!;

    public Guid ForecastRunId { get; set; }
    public DateTime RunAtUtc { get; set; }
    public ForecastMethod Method { get; set; }
    public ForecastHorizon Horizon { get; set; }
    public DateOnly TargetDate { get; set; }

    public decimal ForecastedWithdrawalAmount { get; set; }
    public decimal ConfidenceScore { get; set; }
    public decimal? ActualWithdrawalAmount { get; set; }
    public decimal? AbsolutePercentageError { get; set; }
}
