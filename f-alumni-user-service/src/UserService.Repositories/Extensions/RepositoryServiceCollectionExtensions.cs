using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Repositories.Context;
using UserService.Repositories.Implementations;
using UserService.Repositories.Interfaces;

namespace UserService.Repositories.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core DbContext and repository implementations.
    /// Supports both SQL Server and In-Memory database providers for testing and cloud demo deployments.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var connectionString = configuration?.GetConnectionString("DefaultConnection")
            ?? "Server=localhost\\SQLEXPRESS;Database=FAlumni_User_Db;Trusted_Connection=True;TrustServerCertificate=True;";

        var dbProvider = configuration?["DatabaseProvider"] ?? "SqlServer";
        var useInMemory = dbProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuration?["UseInMemoryDatabase"], "true", StringComparison.OrdinalIgnoreCase);

        if (useInMemory)
        {
            services.AddDbContext<UserServiceDbContext>(options =>
            {
                options.UseInMemoryDatabase("FAlumni_User_InMemoryDb");
            });
        }
        else
        {
            services.AddDbContext<UserServiceDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        }

        services.AddScoped<ISampleItemRepository, SampleItemRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
