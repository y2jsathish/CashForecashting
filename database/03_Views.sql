/*
    ATM Cash Forecasting & Replenishment Management System
    03_Views.sql
*/

SET NOCOUNT ON;
GO

CREATE OR ALTER VIEW dbo.vw_AtmLatestCashPosition
AS
    SELECT
        t.AtmId,
        t.TransactionDate AS AsOfDate,
        t.RemainingCash
    FROM dbo.AtmTransactions t
    INNER JOIN (
        SELECT AtmId, MAX(TransactionDate) AS MaxDate
        FROM dbo.AtmTransactions
        GROUP BY AtmId
    ) latest ON latest.AtmId = t.AtmId AND latest.MaxDate = t.TransactionDate;
GO

CREATE OR ALTER VIEW dbo.vw_AtmRiskOverview
AS
    SELECT
        a.Id AS AtmId,
        a.AtmCode,
        a.AtmName,
        rg.RegionName,
        br.BranchName,
        a.[Status] AS AtmStatus,
        cash.RemainingCash AS CurrentCash,
        a.Capacity,
        rec.RiskLevel,
        rec.Priority,
        rec.RecommendedLoadAmount,
        rec.ProjectedDepletionDate
    FROM dbo.AtmMaster a
    INNER JOIN dbo.Regions rg ON rg.Id = a.RegionId
    INNER JOIN dbo.Branches br ON br.Id = a.BranchId
    LEFT JOIN dbo.vw_AtmLatestCashPosition cash ON cash.AtmId = a.Id
    OUTER APPLY (
        SELECT TOP (1) r.RiskLevel, r.Priority, r.RecommendedLoadAmount, r.ProjectedDepletionDate
        FROM dbo.ReplenishmentRecommendations r
        WHERE r.AtmId = a.Id AND r.IsFulfilled = 0
        ORDER BY r.GeneratedAtUtc DESC
    ) rec
    WHERE a.IsDeleted = 0;
GO

CREATE OR ALTER VIEW dbo.vw_RegionalCashSummary
AS
    SELECT
        rg.Id AS RegionId,
        rg.RegionName,
        COUNT(DISTINCT a.Id) AS AtmCount,
        SUM(ISNULL(cash.RemainingCash, 0)) AS TotalCash,
        SUM(CASE WHEN rec.RiskLevel IN (3, 4) THEN 1 ELSE 0 END) AS AtRiskCount
    FROM dbo.Regions rg
    LEFT JOIN dbo.AtmMaster a ON a.RegionId = rg.Id AND a.IsDeleted = 0
    LEFT JOIN dbo.vw_AtmLatestCashPosition cash ON cash.AtmId = a.Id
    OUTER APPLY (
        SELECT TOP (1) r.RiskLevel
        FROM dbo.ReplenishmentRecommendations r
        WHERE r.AtmId = a.Id AND r.IsFulfilled = 0
        ORDER BY r.GeneratedAtUtc DESC
    ) rec
    GROUP BY rg.Id, rg.RegionName;
GO

CREATE OR ALTER VIEW dbo.vw_OpenAlertsSummary
AS
    SELECT
        al.Id,
        al.AtmId,
        a.AtmCode,
        al.AlertType,
        al.Severity,
        al.[Status],
        al.Title,
        al.TriggeredAtUtc
    FROM dbo.Alerts al
    LEFT JOIN dbo.AtmMaster a ON a.Id = al.AtmId
    WHERE al.[Status] IN (1, 2); -- Open, Acknowledged
GO
