using Microsoft.EntityFrameworkCore;
using AiService.Repositories.Configurations;
using AiService.Repositories.Entities;

namespace AiService.Repositories.Context;

/// <summary>
/// Service-specific DbContext for AI Service (FAlumni_AI_Db).
/// Strictly isolated to this service's own database.
/// Other services must NEVER connect to this DbContext.
/// </summary>
public class AiServiceDbContext : DbContext
{
    public AiServiceDbContext(DbContextOptions<AiServiceDbContext> options) : base(options)
    {
    }

    public DbSet<SampleItem> SampleItems => Set<SampleItem>();
    public DbSet<RagDocument> RagDocuments => Set<RagDocument>();
    public DbSet<RagChunk> RagChunks => Set<RagChunk>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<MentorProfile> MentorProfiles => Set<MentorProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new SampleItemConfiguration());
        var aiConfig = new AiConfiguration();
        modelBuilder.ApplyConfiguration<RagDocument>(aiConfig);
        modelBuilder.ApplyConfiguration<RagChunk>(aiConfig);
        modelBuilder.ApplyConfiguration<AiConversation>(aiConfig);
        modelBuilder.ApplyConfiguration<AiMessage>(aiConfig);
        modelBuilder.ApplyConfiguration<MentorProfile>(aiConfig);
    }
}
