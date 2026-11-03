using Microsoft.EntityFrameworkCore;
using UserService.Repositories.Configurations;
using UserService.Repositories.Entities;

namespace UserService.Repositories.Context;

/// <summary>
/// Service-specific DbContext. Strictly isolated to this service's own database (FAlumni_User_Db).
/// Other services must NEVER connect to this DbContext.
/// </summary>
public class UserServiceDbContext : DbContext
{
    public UserServiceDbContext(DbContextOptions<UserServiceDbContext> options) : base(options)
    {
    }

    public DbSet<SampleItem> SampleItems => Set<SampleItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new SampleItemConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new VerificationTokenConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new BlockedUserConfiguration());
    }
}
