using Microsoft.EntityFrameworkCore;
using NotificationService.Repositories.Entities;

namespace NotificationService.Repositories.Context;

public class NotificationServiceDbContext : DbContext
{
    public NotificationServiceDbContext(DbContextOptions<NotificationServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Type).HasMaxLength(50).IsRequired();
            entity.Property(n => n.ActorName).HasMaxLength(200).IsRequired();
            entity.Property(n => n.Title).HasMaxLength(300).IsRequired();
            entity.Property(n => n.Message).HasMaxLength(1000).IsRequired();
            entity.Property(n => n.Link).HasMaxLength(500).IsRequired();
            entity.Property(n => n.ReferenceId).HasMaxLength(100);
            entity.Property(n => n.SourceEventId).HasMaxLength(150);

            entity.HasIndex(n => new { n.RecipientUserId, n.CreatedAt });
            entity.HasIndex(n => new { n.RecipientUserId, n.IsRead });
            entity.HasIndex(n => new { n.SourceEventId, n.RecipientUserId });
        });
    }
}
