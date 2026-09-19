using ATMCashForecasting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATMCashForecasting.Infrastructure.Persistence.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Message).HasMaxLength(1000).IsRequired();
        builder.HasIndex(a => new { a.Status, a.Severity });
        builder.HasIndex(a => a.TriggeredAtUtc);

        builder.HasOne(a => a.Atm)
            .WithMany()
            .HasForeignKey(a => a.AtmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Recipient).HasMaxLength(200).IsRequired();
        builder.Property(n => n.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(n => n.Status);

        builder.HasOne(n => n.Alert)
            .WithMany(a => a.Notifications)
            .HasForeignKey(n => n.AlertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();
        builder.Property(a => a.EntityName).HasMaxLength(150).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.UserName).HasMaxLength(150);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.UserAgent).HasMaxLength(300);
        builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)");
        builder.HasIndex(a => a.TimestampUtc);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
    }
}
