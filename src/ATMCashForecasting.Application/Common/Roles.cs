namespace ATMCashForecasting.Application.Common;

/// <summary>The six system user roles (Module 1 / RBAC).</summary>
public static class Roles
{
    public const string SystemAdministrator = "SystemAdministrator";
    public const string AtmOperationsUser = "AtmOperationsUser";
    public const string CashManagementUser = "CashManagementUser";
    public const string RegionalManager = "RegionalManager";
    public const string BranchManager = "BranchManager";
    public const string ExecutiveManagement = "ExecutiveManagement";

    public static readonly IReadOnlyList<string> All = new[]
    {
        SystemAdministrator, AtmOperationsUser, CashManagementUser,
        RegionalManager, BranchManager, ExecutiveManagement
    };
}
