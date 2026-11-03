using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ForumService.Repositories.Entities;

namespace ForumService.Repositories.Configurations;

public class ForumConfiguration : 
    IEntityTypeConfiguration<Category>,
    IEntityTypeConfiguration<Post>,
    IEntityTypeConfiguration<PostImage>,
    IEntityTypeConfiguration<PostTag>,
    IEntityTypeConfiguration<Comment>,
    IEntityTypeConfiguration<Reaction>,
    IEntityTypeConfiguration<Bookmark>,
    IEntityTypeConfiguration<Report>,
    IEntityTypeConfiguration<Event>,
    IEntityTypeConfiguration<EventAttendee>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(150).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Content).IsRequired();
        builder.Property(p => p.PostType).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Major).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Campus).HasMaxLength(50).IsRequired();
        builder.Property(p => p.AuthorName).HasMaxLength(200).IsRequired();

        builder.HasIndex(p => p.AuthorId);
        builder.HasIndex(p => p.Campus);
        builder.HasIndex(p => p.Major);
        builder.HasIndex(p => p.PostType);
        builder.HasIndex(p => p.IsAnswered);
        builder.HasIndex(p => p.CreatedAt);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Post)
            .HasForeignKey(i => i.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tags)
            .WithOne(t => t.Post)
            .HasForeignKey(t => t.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Reactions)
            .WithOne(r => r.Post)
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Bookmarks)
            .WithOne(b => b.Post)
            .HasForeignKey(b => b.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<PostImage> builder)
    {
        builder.ToTable("PostImages");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ImageUrl).HasMaxLength(1000).IsRequired();
    }

    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.ToTable("PostTags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TagName).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.TagName);
    }

    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.AuthorName).HasMaxLength(200).IsRequired();

        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.PostId);
        builder.HasIndex(c => c.ParentCommentId);
    }

    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("Reactions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReactionType).HasMaxLength(30).IsRequired();
        builder.HasIndex(r => new { r.PostId, r.UserId }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("Bookmarks");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.PostId, b.UserId }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TargetType).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(30).IsRequired();
        builder.HasIndex(r => r.TargetId);
    }

    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Campus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CreatorName).HasMaxLength(200).IsRequired();

        builder.HasIndex(e => e.Campus);
        builder.HasIndex(e => e.StartTime);

        builder.HasMany(e => e.Attendees)
            .WithOne(a => a.Event)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<EventAttendee> builder)
    {
        builder.ToTable("EventAttendees");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Status).HasMaxLength(30).IsRequired();
        builder.Property(a => a.UserName).HasMaxLength(200).IsRequired();
        builder.HasIndex(a => new { a.EventId, a.UserId }).IsUnique();
    }
}
