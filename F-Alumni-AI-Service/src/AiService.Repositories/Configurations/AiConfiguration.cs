using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiService.Repositories.Entities;

namespace AiService.Repositories.Configurations;

public class AiConfiguration :
    IEntityTypeConfiguration<RagDocument>,
    IEntityTypeConfiguration<RagChunk>,
    IEntityTypeConfiguration<AiConversation>,
    IEntityTypeConfiguration<AiMessage>,
    IEntityTypeConfiguration<MentorProfile>
{
    public void Configure(EntityTypeBuilder<RagDocument> builder)
    {
        builder.ToTable("RagDocuments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Title).HasMaxLength(300).IsRequired();
        builder.Property(d => d.SourcePath).HasMaxLength(500).IsRequired();
        builder.Property(d => d.DocumentType).HasMaxLength(50).IsRequired();
        builder.Property(d => d.ProgramCode).HasMaxLength(50);
        builder.Property(d => d.Campus).HasMaxLength(50).IsRequired();

        builder.HasIndex(d => d.DocumentType);
        builder.HasIndex(d => d.ProgramCode);
        builder.HasIndex(d => d.Campus);

        builder.HasMany(d => d.Chunks)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<RagChunk> builder)
    {
        builder.ToTable("RagChunks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ChunkIdentifier).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Section).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Content).IsRequired();

        builder.HasIndex(c => c.DocumentId);
        builder.HasIndex(c => c.ChunkIdentifier);
    }

    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("AiConversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(250).IsRequired();
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.LastMessageAt);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("AiMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Role).HasMaxLength(20).IsRequired();
        builder.Property(m => m.Content).IsRequired();
        builder.HasIndex(m => m.ConversationId);
    }

    public void Configure(EntityTypeBuilder<MentorProfile> builder)
    {
        builder.ToTable("MentorProfiles");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.FullName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Major).HasMaxLength(50).IsRequired();
        builder.HasIndex(m => m.Major);
    }
}
