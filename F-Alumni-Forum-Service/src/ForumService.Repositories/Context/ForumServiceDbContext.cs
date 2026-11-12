using Microsoft.EntityFrameworkCore;
using ForumService.Repositories.Configurations;
using ForumService.Repositories.Entities;

namespace ForumService.Repositories.Context;

/// <summary>
/// Service-specific DbContext for Forum Service (FAlumni_Forum_Db).
/// Strictly isolated to this service's own database.
/// Other services must NEVER connect to this DbContext.
/// </summary>
public class ForumServiceDbContext : DbContext
{
    public ForumServiceDbContext(DbContextOptions<ForumServiceDbContext> options) : base(options)
    {
    }

    public DbSet<SampleItem> SampleItems => Set<SampleItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostImage> PostImages => Set<PostImage>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventAttendee> EventAttendees => Set<EventAttendee>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new SampleItemConfiguration());
        var forumConfig = new ForumConfiguration();
        modelBuilder.ApplyConfiguration<Category>(forumConfig);
        modelBuilder.ApplyConfiguration<Post>(forumConfig);
        modelBuilder.ApplyConfiguration<PostImage>(forumConfig);
        modelBuilder.ApplyConfiguration<PostTag>(forumConfig);
        modelBuilder.ApplyConfiguration<Comment>(forumConfig);
        modelBuilder.ApplyConfiguration<Reaction>(forumConfig);
        modelBuilder.ApplyConfiguration<Bookmark>(forumConfig);
        modelBuilder.ApplyConfiguration<Report>(forumConfig);
        modelBuilder.ApplyConfiguration<Event>(forumConfig);
        modelBuilder.ApplyConfiguration<EventAttendee>(forumConfig);
        modelBuilder.ApplyConfiguration<Notification>(forumConfig);
    }
}
