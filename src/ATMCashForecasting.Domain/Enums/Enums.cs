namespace ATMCashForecasting.Domain.Enums;

public enum AtmOperationalStatus
{
    Active = 1,
    Inactive = 2,
    Offline = 3,
    UnderMaintenance = 4,
    Decommissioned = 5
}

public enum AtmType
{
    OnSite = 1,
    OffSite = 2,
    ThroughTheWall = 3,
    MobileAtm = 4,
    CashRecycler = 5
}

public enum ForecastMethod
{
    MovingAverage = 1,
    WeightedMovingAverage = 2,
    TrendForecasting = 3,
    SeasonalForecasting = 4,
    MLNet = 5,
    Prophet = 6,
    XGBoost = 7,
    Lstm = 8
}

public enum ForecastHorizon
{
    NextDay = 1,
    ThreeDays = 3,
    SevenDays = 7,
    FifteenDays = 15,
    ThirtyDays = 30
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum ReplenishmentPriority
{
    Routine = 1,
    Scheduled = 2,
    Urgent = 3,
    Emergency = 4
}

public enum AlertType
{
    CashDepletion = 1,
    AtmOffline = 2,
    ForecastFailure = 3,
    DataImportFailure = 4,
    LowConfidenceForecast = 5,
    ReplenishmentOverdue = 6
}

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    High = 3,
    Critical = 4
}

public enum AlertStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3,
    Suppressed = 4
}

public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    Teams = 3,
    InApp = 4
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Retried = 4
}

public enum TransactionImportSource
{
    CsvUpload = 1,
    ExcelUpload = 2,
    ApiIntegration = 3,
    ScheduledImport = 4
}

public enum ImportValidationStatus
{
    Valid = 1,
    Warning = 2,
    Rejected = 3
}

public enum AuditAction
{
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    Create = 4,
    Update = 5,
    Delete = 6,
    DataUpload = 7,
    ForecastRun = 8,
    ConfigurationChange = 9,
    ReportExport = 10
}
