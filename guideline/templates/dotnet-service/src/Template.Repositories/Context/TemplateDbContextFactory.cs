using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Template.Repositories.Context;

/// <summary>
/// Design-time factory enabling EF Core CLI commands (migrations, script generation).
/// </summary>
public class TemplateDbContextFactory : IDesignTimeDbContextFactory<TemplateDbContext>
{
    public TemplateDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TemplateDbContext>();
        
        // Connection string priority:
        // 1. CLI argument after '--' if provided
        // 2. ConnectionStrings__DefaultConnection environment variable
        // 3. SQLSERVER_CONNECTION_STRING environment variable
        // 4. Default local SQL Server Express connection string for migrations design-time
        var connectionString = (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            ? args[0]
            : (Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")
                ?? "Server=localhost\\SQLEXPRESS;Database=FAlumni_Template_Db;Trusted_Connection=True;TrustServerCertificate=True;");

        optionsBuilder.UseSqlServer(connectionString);

        return new TemplateDbContext(optionsBuilder.Options);
    }
}
