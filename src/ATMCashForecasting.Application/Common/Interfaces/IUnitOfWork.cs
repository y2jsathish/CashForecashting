using ATMCashForecasting.Domain.Entities;

namespace ATMCashForecasting.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IRepository<Region> Regions { get; }
    IRepository<Branch> Branches { get; }
    IRepository<AtmMaster> Atms { get; }
    IRepository<AtmTransaction> Transactions { get; }
    IRepository<AtmCashLoad> CashLoads { get; }
    IRepository<AtmStatusHistory> AtmStatusHistories { get; }
    IRepository<ForecastResult> ForecastResults { get; }
    IRepository<ForecastHistory> ForecastHistories { get; }
    IRepository<ReplenishmentRecommendation> Recommendations { get; }
    IRepository<Alert> Alerts { get; }
    IRepository<NotificationLog> NotificationLogs { get; }
    IRepository<HolidayMaster> Holidays { get; }
    IRepository<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
