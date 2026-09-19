/*
    ATM Cash Forecasting & Replenishment Management System
    02_StoredProcedures.sql
*/

SET NOCOUNT ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAtmDailyForecastReport
    @AsOfDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @AsOfDate = ISNULL(@AsOfDate, CAST(SYSUTCDATETIME() AS DATE));

    SELECT
        a.AtmCode,
        a.AtmName,
        r.CurrentCash,
        r.ForecastDemand,
        r.RecommendedLoadAmount,
        r.Priority,
        r.RiskLevel,
        r.ProjectedDepletionDate
    FROM dbo.ReplenishmentRecommendations r
    INNER JOIN dbo.AtmMaster a ON a.Id = r.AtmId
    WHERE r.IsFulfilled = 0
      AND CAST(r.GeneratedAtUtc AS DATE) <= @AsOfDate
    ORDER BY r.RiskLevel DESC, r.ProjectedDepletionDate ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAtmsAtRisk
    @RiskLevel TINYINT = NULL -- NULL = Critical or High
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.Id AS AtmId, a.AtmCode, a.AtmName, br.BranchName, rg.RegionName,
        r.CurrentCash, r.RecommendedLoadAmount, r.RiskLevel, r.ProjectedDepletionDate
    FROM dbo.ReplenishmentRecommendations r
    INNER JOIN dbo.AtmMaster a ON a.Id = r.AtmId
    INNER JOIN dbo.Branches br ON br.Id = a.BranchId
    INNER JOIN dbo.Regions rg ON rg.Id = a.RegionId
    WHERE r.IsFulfilled = 0
      AND (
            (@RiskLevel IS NULL AND r.RiskLevel IN (3, 4))
            OR r.RiskLevel = @RiskLevel
          )
    ORDER BY r.RiskLevel DESC, r.ProjectedDepletionDate ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetForecastAccuracySummary
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.Id AS AtmId,
        a.AtmCode,
        AVG(fh.AbsolutePercentageError) AS AverageAbsolutePercentageError,
        (1 - AVG(fh.AbsolutePercentageError)) * 100 AS AccuracyPercent,
        COUNT(*) AS SampleCount
    FROM dbo.ForecastHistory fh
    INNER JOIN dbo.AtmMaster a ON a.Id = fh.AtmId
    WHERE fh.TargetDate BETWEEN @FromDate AND @ToDate
      AND fh.AbsolutePercentageError IS NOT NULL
    GROUP BY a.Id, a.AtmCode
    ORDER BY AccuracyPercent DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_ArchiveOldAuditLogs
    @RetentionDays INT = 2555 -- ~7 years, typical banking retention
AS
BEGIN
    SET NOCOUNT ON;
    -- Deliberately no DELETE here: audit rows are immutable. This proc only reports the
    -- candidate row count so an operator can trigger an out-of-band archive-to-cold-storage job.
    SELECT COUNT(*) AS CandidateRowCount
    FROM dbo.AuditLogs
    WHERE TimestampUtc < DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_UpsertAtmTransaction
    @AtmId INT,
    @TransactionDate DATE,
    @WithdrawalCount INT,
    @WithdrawalAmount DECIMAL(18,2),
    @DepositAmount DECIMAL(18,2),
    @RemainingCash DECIMAL(18,2),
    @CashLoaded DECIMAL(18,2),
    @Source TINYINT,
    @ImportBatchId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.AtmTransactions AS target
    USING (SELECT @AtmId AS AtmId, @TransactionDate AS TransactionDate) AS src
        ON target.AtmId = src.AtmId AND target.TransactionDate = src.TransactionDate
    WHEN MATCHED THEN
        UPDATE SET
            WithdrawalCount = @WithdrawalCount,
            WithdrawalAmount = @WithdrawalAmount,
            DepositAmount = @DepositAmount,
            RemainingCash = @RemainingCash,
            CashLoaded = @CashLoaded,
            [Source] = @Source,
            ImportBatchId = @ImportBatchId,
            ModifiedAtUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (AtmId, TransactionDate, WithdrawalCount, WithdrawalAmount, DepositAmount, RemainingCash, CashLoaded, [Source], ImportBatchId)
        VALUES (@AtmId, @TransactionDate, @WithdrawalCount, @WithdrawalAmount, @DepositAmount, @RemainingCash, @CashLoaded, @Source, @ImportBatchId);
END
GO
