using ATMCashForecasting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATMCashForecasting.Infrastructure.Persistence.Configurations;

public class AtmTransactionConfiguration : IEntityTypeConfiguration<AtmTransaction>
{
    public void Configure(EntityTypeBuilder<AtmTransaction> builder)
    {
        builder.ToTable("AtmTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.WithdrawalAmount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.DepositAmount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.RemainingCash).HasColumnType("decimal(18,2)");
        builder.Property(t => t.CashLoaded).HasColumnType("decimal(18,2)");
        builder.Property(t => t.ValidationNotes).HasMaxLength(500);

        // One aggregated row per ATM per day.
        builder.HasIndex(t => new { t.AtmId, t.TransactionDate }).IsUnique();
        builder.HasIndex(t => t.ImportBatchId);

        builder.HasOne(t => t.Atm)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AtmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AtmCashLoadConfiguration : IEntityTypeConfiguration<AtmCashLoad>
{
    public void Configure(EntityTypeBuilder<AtmCashLoad> builder)
    {
        builder.ToTable("AtmCashLoads");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.AmountLoaded).HasColumnType("decimal(18,2)");
        builder.Property(c => c.CashBeforeLoad).HasColumnType("decimal(18,2)");
        builder.Property(c => c.CashAfterLoad).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.HasIndex(c => new { c.AtmId, c.LoadDateUtc });

        builder.HasOne(c => c.Atm)
            .WithMany(a => a.CashLoads)
            .HasForeignKey(c => c.AtmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Recommendation)
            .WithMany()
            .HasForeignKey(c => c.RecommendationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AtmStatusHistoryConfiguration : IEntityTypeConfiguration<AtmStatusHistory>
{
    public void Configure(EntityTypeBuilder<AtmStatusHistory> builder)
    {
        builder.ToTable("AtmStatusHistory");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Reason).HasMaxLength(300);
        builder.HasIndex(s => new { s.AtmId, s.ChangedAtUtc });

        builder.HasOne(s => s.Atm)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(s => s.AtmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
