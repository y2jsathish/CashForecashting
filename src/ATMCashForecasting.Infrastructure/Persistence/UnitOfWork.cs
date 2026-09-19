using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Domain.Entities;

namespace ATMCashForecasting.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Regions = new Repository<Region>(context);
        Branches = new Repository<Branch>(context);
        Atms = new Repository<AtmMaster>(context);
        Transactions = new Repository<AtmTransaction>(context);
        CashLoads = new Repository<AtmCashLoad>(context);
        AtmStatusHistories = new Repository<AtmStatusHistory>(context);
        ForecastResults = new Repository<ForecastResult>(context);
        ForecastHistories = new Repository<ForecastHistory>(context);
        Recommendations = new Repository<ReplenishmentRecommendation>(context);
        Alerts = new Repository<Alert>(context);
        NotificationLogs = new Repository<NotificationLog>(context);
        Holidays = new Repository<HolidayMaster>(context);
        AuditLogs = new Repository<AuditLog>(context);
    }

    public IRepository<Region> Regions { get; }
    public IRepository<Branch> Branches { get; }
    public IRepository<AtmMaster> Atms { get; }
    public IRepository<AtmTransaction> Transactions { get; }
    public IRepository<AtmCashLoad> CashLoads { get; }
    public IRepository<AtmStatusHistory> AtmStatusHistories { get; }
    public IRepository<ForecastResult> ForecastResults { get; }
    public IRepository<ForecastHistory> ForecastHistories { get; }
    public IRepository<ReplenishmentRecommendation> Recommendations { get; }
    public IRepository<Alert> Alerts { get; }
    public IRepository<NotificationLog> NotificationLogs { get; }
    public IRepository<HolidayMaster> Holidays { get; }
    public IRepository<AuditLog> AuditLogs { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) => await _context.SaveChangesAsync(ct);
}
