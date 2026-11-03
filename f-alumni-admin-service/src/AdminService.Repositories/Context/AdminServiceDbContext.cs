using Microsoft.EntityFrameworkCore;
using AdminService.Repositories.Configurations;
using AdminService.Repositories.Entities;

namespace AdminService.Repositories.Context;

/// <summary>
/// Service-specific DbContext for FAlumni_Admin_Db. Strictly isolated to Admin Service.
/// Other services must NEVER connect to this DbContext.
/// </summary>
public class AdminServiceDbContext : DbContext
{
    public AdminServiceDbContext(DbContextOptions<AdminServiceDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemAnnouncement> SystemAnnouncements => Set<SystemAnnouncement>();
    public DbSet<ModerationReport> ModerationReports => Set<ModerationReport>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<SampleItem> SampleItems => Set<SampleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new SampleItemConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new SystemAnnouncementConfiguration());
        modelBuilder.ApplyConfiguration(new ModerationReportConfiguration());
        modelBuilder.ApplyConfiguration(new SystemSettingConfiguration());
    }
}
