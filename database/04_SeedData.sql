/*
    ATM Cash Forecasting & Replenishment Management System
    04_SeedData.sql — reference/master data only. No customer or transaction data.
    Roles and the break-glass admin user are seeded by the application (see
    ATMCashForecasting.Web/Startup/IdentitySeeder.cs) once Identity tables exist, not here.
*/

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions)
BEGIN
    INSERT INTO dbo.Permissions ([Name], [Module], Description) VALUES
        ('atm.view',            'ATM Master',    'View ATM master records'),
        ('atm.manage',          'ATM Master',    'Create/edit/delete ATM master records'),
        ('transactions.import', 'Transactions',  'Upload/import transaction data'),
        ('forecast.run',        'Forecasting',   'Trigger forecast runs'),
        ('forecast.view',       'Forecasting',   'View forecast results'),
        ('replenishment.manage','Replenishment', 'Generate recommendations and record cash loads'),
        ('alerts.manage',       'Alerts',        'Acknowledge/resolve alerts'),
        ('reports.view',        'Reporting',     'View and export reports'),
        ('audit.view',          'Audit',         'View audit logs'),
        ('users.manage',        'User Mgmt',     'Manage users and role assignments');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Regions)
BEGIN
    INSERT INTO dbo.Regions (RegionCode, RegionName) VALUES
        ('NE', 'Northeast'),
        ('SE', 'Southeast'),
        ('MW', 'Midwest'),
        ('SW', 'Southwest'),
        ('WE', 'West');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Branches)
BEGIN
    INSERT INTO dbo.Branches (BranchCode, BranchName, RegionId, Address)
    SELECT 'BR-' + rg.RegionCode + '-01', rg.RegionName + ' Main Branch', rg.Id, rg.RegionName + ' Main Street 1'
    FROM dbo.Regions rg;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.HolidayMaster)
BEGIN
    INSERT INTO dbo.HolidayMaster (HolidayDate, HolidayName, RegionId, DemandUpliftFactor) VALUES
        (DATEFROMPARTS(YEAR(GETDATE()), 12, 24), 'Christmas Eve', NULL, 1.6),
        (DATEFROMPARTS(YEAR(GETDATE()), 12, 31), 'New Year''s Eve', NULL, 1.8),
        (DATEFROMPARTS(YEAR(GETDATE()), 7, 3),   'Independence Day Eve', NULL, 1.4);
END
GO
