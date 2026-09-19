using ATMCashForecasting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATMCashForecasting.Infrastructure.Persistence.Configurations;

public class ForecastResultConfiguration : IEntityTypeConfiguration<ForecastResult>
{
    public void Configure(EntityTypeBuilder<ForecastResult> builder)
    {
        builder.ToTable("ForecastResults");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.ForecastedWithdrawalAmount).HasColumnType("decimal(18,2)");
        builder.Property(f => f.ConfidenceScore).HasColumnType("decimal(5,2)");
        builder.Property(f => f.LowerBound).HasColumnType("decimal(18,2)");
        builder.Property(f => f.UpperBound).HasColumnType("decimal(18,2)");
        builder.Property(f => f.ActualWithdrawalAmount).HasColumnType("decimal(18,2)");
        builder.Property(f => f.AbsolutePercentageError).HasColumnType("decimal(9,4)");

        builder.HasIndex(f => new { f.AtmId, f.TargetDate });
        builder.HasIndex(f => f.ForecastRunId);

        builder.HasOne(f => f.Atm)
            .WithMany(a => a.ForecastResults)
            .HasForeignKey(f => f.AtmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ForecastHistoryConfiguration : IEntityTypeConfiguration<ForecastHistory>
{
    public void Configure(EntityTypeBuilder<ForecastHistory> builder)
    {
        builder.ToTable("ForecastHistory");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.ForecastedWithdrawalAmount).HasColumnType("decimal(18,2)");
        builder.Property(f => f.ConfidenceScore).HasColumnType("decimal(5,2)");
        builder.Property(f => f.ActualWithdrawalAmount).HasColumnType("decimal(18,2)");
        builder.Property(f => f.AbsolutePercentageError).HasColumnType("decimal(9,4)");

        builder.HasIndex(f => new { f.AtmId, f.TargetDate });
        builder.HasIndex(f => f.ForecastRunId);

        builder.HasOne(f => f.Atm)
            .WithMany()
            .HasForeignKey(f => f.AtmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReplenishmentRecommendationConfiguration : IEntityTypeConfiguration<ReplenishmentRecommendation>
{
    public void Configure(EntityTypeBuilder<ReplenishmentRecommendation> builder)
    {
        builder.ToTable("ReplenishmentRecommendations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.CurrentCash).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ForecastDemand).HasColumnType("decimal(18,2)");
        builder.Property(r => r.SafetyBuffer).HasColumnType("decimal(18,2)");
        builder.Property(r => r.AtmCapacity).HasColumnType("decimal(18,2)");
        builder.Property(r => r.RecommendedLoadAmount).HasColumnType("decimal(18,2)");

        builder.HasIndex(r => new { r.AtmId, r.IsFulfilled });
        builder.HasIndex(r => r.RiskLevel);

        builder.HasOne(r => r.Atm)
            .WithMany()
            .HasForeignKey(r => r.AtmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ForecastResult)
            .WithMany(f => f.Recommendations)
            .HasForeignKey(r => r.ForecastResultId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
