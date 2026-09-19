using ATMCashForecasting.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ATMCashForecasting.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<AtmMaster> Atms => Set<AtmMaster>();
    public DbSet<AtmTransaction> Transactions => Set<AtmTransaction>();
    public DbSet<AtmCashLoad> CashLoads => Set<AtmCashLoad>();
    public DbSet<AtmStatusHistory> AtmStatusHistories => Set<AtmStatusHistory>();
    public DbSet<ForecastResult> ForecastResults => Set<ForecastResult>();
    public DbSet<ForecastHistory> ForecastHistories => Set<ForecastHistory>();
    public DbSet<ReplenishmentRecommendation> Recommendations => Set<ReplenishmentRecommendation>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<HolidayMaster> Holidays => Set<HolidayMaster>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global soft-delete filter for entities deriving from BaseEntity's IsDeleted flag.
        builder.Entity<Region>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Branch>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AtmMaster>().HasQueryFilter(e => !e.IsDeleted);
    }
}
