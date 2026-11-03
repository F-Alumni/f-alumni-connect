using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AiService.Api.Data;
using AiService.Repositories.Context;
using Xunit;

namespace AiService.Tests.Database;

public class AiDatabaseSeederTests
{
    private (IServiceProvider Services, AiServiceDbContext Context) CreateTestScope()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<AiServiceDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddLogging(logging => logging.AddDebug());

        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<AiServiceDbContext>();

        return (serviceProvider, context);
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_SeedsAllFixtures()
    {
        // Arrange
        var (services, context) = CreateTestScope();

        // Act
        await AiDatabaseSeeder.SeedAsync(services);

        // Assert - Mentor Profiles
        var mentors = await context.MentorProfiles.ToListAsync();
        Assert.Equal(3, mentors.Count);
        Assert.Contains(mentors, m => m.Major == "SE" && m.CurrentCompany == "FPT Software");
        Assert.Contains(mentors, m => m.Major == "AI" && m.CurrentCompany == "VinAI Research");
        Assert.Contains(mentors, m => m.Major == "IA" && m.CurrentCompany == "Viettel Cyber Security");

        // Assert - RAG Documents & Chunks
        var documents = await context.RagDocuments.Include(d => d.Chunks).ToListAsync();
        Assert.Equal(3, documents.Count);
        var totalChunks = documents.SelectMany(d => d.Chunks).Count();
        Assert.Equal(4, totalChunks);

        // Assert - Conversations & Messages
        var conversations = await context.AiConversations.Include(c => c.Messages).ToListAsync();
        Assert.Single(conversations);
        var conv1 = conversations.Single(c => c.Id == AiDatabaseSeeder.Conversation1Id);
        Assert.Equal(AiDatabaseSeeder.UserAId, conv1.UserId);
        Assert.Equal(2, conv1.Messages.Count);
        Assert.Contains(conv1.Messages, m => m.Role == "user");
        Assert.Contains(conv1.Messages, m => m.Role == "assistant");
    }

    [Fact]
    public async Task SeedAsync_WhenCalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var (services, context) = CreateTestScope();

        // Act - Call twice
        await AiDatabaseSeeder.SeedAsync(services);
        await AiDatabaseSeeder.SeedAsync(services);

        // Assert
        Assert.Equal(3, await context.MentorProfiles.CountAsync());
        Assert.Equal(3, await context.RagDocuments.CountAsync());
        Assert.Equal(4, await context.RagChunks.CountAsync());
        Assert.Equal(1, await context.AiConversations.CountAsync());
        Assert.Equal(2, await context.AiMessages.CountAsync());
    }
}
