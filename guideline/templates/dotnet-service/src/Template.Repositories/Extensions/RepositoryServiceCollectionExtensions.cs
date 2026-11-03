using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Template.Repositories.Context;
using Template.Repositories.Implementations;
using Template.Repositories.Interfaces;

namespace Template.Repositories.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core DbContext and repository implementations.
    /// Supports both SQL Server and In-Memory database providers for testing.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var connectionString = configuration?.GetConnectionString("DefaultConnection")
            ?? "Server=localhost\\SQLEXPRESS;Database=FAlumni_Template_Db;Trusted_Connection=True;TrustServerCertificate=True;";

        var dbProvider = configuration?["DatabaseProvider"] ?? "SqlServer";
        if (dbProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<TemplateDbContext>(options =>
            {
                options.UseInMemoryDatabase("Template_TestDb");
            });
        }
        else
        {
            services.AddDbContext<TemplateDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        }

        services.AddScoped<ISampleItemRepository, SampleItemRepository>();

        return services;
    }
}
