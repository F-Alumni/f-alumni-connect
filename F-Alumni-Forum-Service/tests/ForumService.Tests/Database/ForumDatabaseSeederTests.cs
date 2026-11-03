using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ForumService.Api.Data;
using ForumService.Repositories.Context;
using Xunit;

namespace ForumService.Tests.Database;

public class ForumDatabaseSeederTests
{
    private (IServiceProvider Services, ForumServiceDbContext Context) CreateTestScope()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<ForumServiceDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddLogging(logging => logging.AddDebug());

        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<ForumServiceDbContext>();

        return (serviceProvider, context);
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_SeedsAllFixtures()
    {
        // Arrange
        var (services, context) = CreateTestScope();

        // Act
        await ForumDatabaseSeeder.SeedAsync(services);

        // Assert
        var categories = await context.Categories.ToListAsync();
        Assert.Equal(5, categories.Count);
        Assert.Contains(categories, c => c.Slug == "software-engineering");
        Assert.Contains(categories, c => c.Slug == "artificial-intelligence");

        var posts = await context.Posts.Include(p => p.Comments).Include(p => p.Tags).Include(p => p.Reactions).ToListAsync();
        Assert.Equal(2, posts.Count);
        var post1 = posts.Single(p => p.Id == ForumDatabaseSeeder.Post1Id);
        Assert.Equal(ForumDatabaseSeeder.UserAId, post1.AuthorId);
        Assert.Equal(2, post1.Comments.Count);
        Assert.Equal(3, post1.Tags.Count);
        Assert.Single(post1.Reactions);

        var events = await context.Events.Include(e => e.Attendees).ToListAsync();
        Assert.Single(events);
        var evt = events.First();
        Assert.Equal(ForumDatabaseSeeder.Event1Id, evt.Id);
        Assert.Equal(3, evt.Attendees.Count);
    }

    [Fact]
    public async Task SeedAsync_WhenCalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var (services, context) = CreateTestScope();

        // Act - Call twice
        await ForumDatabaseSeeder.SeedAsync(services);
        await ForumDatabaseSeeder.SeedAsync(services);

        // Assert
        Assert.Equal(5, await context.Categories.CountAsync());
        Assert.Equal(2, await context.Posts.CountAsync());
        Assert.Equal(1, await context.Events.CountAsync());
    }
}
