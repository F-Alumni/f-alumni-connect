using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AdminService.Repositories.Entities;

namespace AdminService.Repositories.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdminUserId).IsRequired();
        builder.Property(x => x.AdminEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetEntity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.Details).HasMaxLength(4000);
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => new { x.Action, x.TargetEntity });
        builder.HasIndex(x => x.AdminUserId);
    }
}

public class SystemAnnouncementConfiguration : IEntityTypeConfiguration<SystemAnnouncement>
{
    public void Configure(EntityTypeBuilder<SystemAnnouncement> builder)
    {
        builder.ToTable("SystemAnnouncements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedByAdminEmail).HasMaxLength(256);
        builder.Property(x => x.IsPublished).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.IsPublished, x.PublishedAtUtc });
    }
}

public class ModerationReportConfiguration : IEntityTypeConfiguration<ModerationReport>
{
    public void Configure(EntityTypeBuilder<ModerationReport> builder)
    {
        builder.ToTable("ModerationReports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReporterUserId).IsRequired();
        builder.Property(x => x.ReporterEmail).HasMaxLength(256);
        builder.Property(x => x.TargetType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TargetId).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.TargetType, x.TargetId });
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(x => x.Key);

        builder.Property(x => x.Key).HasMaxLength(100);
        builder.Property(x => x.Value).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
