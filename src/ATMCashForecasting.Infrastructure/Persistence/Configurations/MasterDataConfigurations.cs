using ATMCashForecasting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATMCashForecasting.Infrastructure.Persistence.Configurations;

public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("Regions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RegionCode).HasMaxLength(20).IsRequired();
        builder.Property(r => r.RegionName).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.RegionCode).IsUnique();
    }
}

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BranchCode).HasMaxLength(20).IsRequired();
        builder.Property(b => b.BranchName).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(300);
        builder.HasIndex(b => b.BranchCode).IsUnique();

        builder.HasOne(b => b.Region)
            .WithMany(r => r.Branches)
            .HasForeignKey(b => b.RegionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AtmMasterConfiguration : IEntityTypeConfiguration<AtmMaster>
{
    public void Configure(EntityTypeBuilder<AtmMaster> builder)
    {
        builder.ToTable("AtmMaster");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AtmCode).HasMaxLength(20).IsRequired();
        builder.Property(a => a.AtmName).HasMaxLength(150).IsRequired();
        builder.Property(a => a.TerminalId).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Currency).HasMaxLength(3).IsRequired();
        builder.Property(a => a.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(a => a.Longitude).HasColumnType("decimal(9,6)");
        builder.Property(a => a.Capacity).HasColumnType("decimal(18,2)");
        builder.Property(a => a.SafetyBufferAmount).HasColumnType("decimal(18,2)");

        builder.HasIndex(a => a.AtmCode).IsUnique();
        builder.HasIndex(a => a.TerminalId).IsUnique();
        builder.HasIndex(a => new { a.RegionId, a.BranchId });
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Region)
            .WithMany()
            .HasForeignKey(a => a.RegionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Branch)
            .WithMany(b => b.Atms)
            .HasForeignKey(a => a.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class HolidayMasterConfiguration : IEntityTypeConfiguration<HolidayMaster>
{
    public void Configure(EntityTypeBuilder<HolidayMaster> builder)
    {
        builder.ToTable("HolidayMaster");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.HolidayName).HasMaxLength(150).IsRequired();
        builder.Property(h => h.DemandUpliftFactor).HasColumnType("decimal(5,2)");
        builder.HasIndex(h => new { h.HolidayDate, h.RegionId });

        builder.HasOne(h => h.Region)
            .WithMany()
            .HasForeignKey(h => h.RegionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
