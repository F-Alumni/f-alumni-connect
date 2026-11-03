using Microsoft.EntityFrameworkCore;
using Template.Repositories.Configurations;
using Template.Repositories.Entities;

namespace Template.Repositories.Context;

/// <summary>
/// Service-specific DbContext. Strictly isolated to this service's own database.
/// Other services must NEVER connect to this DbContext.
/// </summary>
public class TemplateDbContext : DbContext
{
    public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options)
    {
    }

    public DbSet<SampleItem> SampleItems => Set<SampleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new SampleItemConfiguration());
    }
}
