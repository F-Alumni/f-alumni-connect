using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ForumService.Repositories.Context;
using ForumService.Repositories.Implementations;
using ForumService.Repositories.Interfaces;

namespace ForumService.Repositories.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core DbContext and repository implementations.
    /// Supports both SQL Server and In-Memory database providers for testing.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var connectionString = configuration?.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,14333;Database=FAlumni_Forum_Db;User Id=sa;Password=Password@123;TrustServerCertificate=True;";

        var dbProvider = configuration?["DatabaseProvider"] ?? "SqlServer";
        if (dbProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ForumServiceDbContext>(options =>
            {
                options.UseInMemoryDatabase("ForumService_TestDb");
            });
        }
        else
        {
            services.AddDbContext<ForumServiceDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        }

        services.AddScoped<ISampleItemRepository, SampleItemRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IInteractionRepository, InteractionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
