using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AiService.Repositories.Context;
using AiService.Repositories.Implementations;
using AiService.Repositories.Interfaces;

namespace AiService.Repositories.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core DbContext and repository implementations.
    /// Supports both SQL Server and In-Memory database providers for testing.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var connectionString = configuration?.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,14333;Database=FAlumni_AI_Db;User Id=sa;Password=Password@123;TrustServerCertificate=True;";

        var dbProvider = configuration?["DatabaseProvider"] ?? "SqlServer";
        if (dbProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AiServiceDbContext>(options =>
            {
                options.UseInMemoryDatabase("AiService_TestDb");
            });
        }
        else
        {
            services.AddDbContext<AiServiceDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        }

        services.AddScoped<ISampleItemRepository, SampleItemRepository>();
        services.AddScoped<IRagRepository, RagRepository>();

        return services;
    }
}
