using Microsoft.EntityFrameworkCore;
using UserService.Repositories.Context;
using UserService.Repositories.Entities;
using UserService.Services.Implementations;

namespace UserService.Api.Data;

/// <summary>
/// Seeds deterministic sample users (Matching guideline IDs 11111111-..., 22222222-..., 33333333-... and an Admin user)
/// so teammates (Chat, Forum, Admin, Web, Mobile) can immediately test login, directory, and gRPC lookups.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedDefaultUsersAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();

        try
        {
            if (dbContext.Database.IsRelational())
            {
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            if (await dbContext.Users.AnyAsync())
            {
                return;
            }

            var user1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var user3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            var defaultPasswordHash = PasswordHasher.HashPassword("Password@123");

            var seedUsers = new List<User>
            {
                new()
                {
                    Id = user1Id,
                    Email = "nguyenvana@fpt.edu.vn",
                    PasswordHash = defaultPasswordHash,
                    FullName = "Nguyen Van A",
                    Role = "USER",
                    AccountType = "Alumni",
                    Status = "Active",
                    EmailVerified = true,
                    Profile = new UserProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user1Id,
                        DisplayName = "Nguyen Van A",
                        AvatarUrl = "https://cdn.falumni.local/avatars/nguyenvana.jpg",
                        StudentCode = "SE170001",
                        GraduationYear = 2024,
                        Major = "Software Engineering",
                        CurrentCompany = "FPT Software",
                        JobTitle = "Backend Engineer",
                        Bio = "FPT University Alumni K17",
                        Skills = "C#, .NET 10, Microservices, Kafka, gRPC"
                    }
                },
                new()
                {
                    Id = user2Id,
                    Email = "tranthib@fpt.edu.vn",
                    PasswordHash = defaultPasswordHash,
                    FullName = "Tran Thi B",
                    Role = "USER",
                    AccountType = "Student",
                    Status = "Active",
                    EmailVerified = true,
                    Profile = new UserProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user2Id,
                        DisplayName = "Tran Thi B",
                        AvatarUrl = "https://cdn.falumni.local/avatars/tranthib.jpg",
                        StudentCode = "SE190002",
                        GraduationYear = 2026,
                        Major = "Artificial Intelligence",
                        CurrentCompany = "FPT University",
                        JobTitle = "Final Year Student",
                        Bio = "Passionate about AI and Mentorship",
                        Skills = "Python, React, Flutter"
                    }
                },
                new()
                {
                    Id = user3Id,
                    Email = "levanc@fpt.edu.vn",
                    PasswordHash = defaultPasswordHash,
                    FullName = "Le Van C",
                    Role = "USER",
                    AccountType = "Alumni",
                    Status = "Suspended",
                    SuspensionReason = "Spamming forum posts",
                    EmailVerified = true,
                    Profile = new UserProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user3Id,
                        DisplayName = "Le Van C",
                        AvatarUrl = "https://cdn.falumni.local/avatars/levanc.jpg",
                        StudentCode = "SE160003",
                        GraduationYear = 2023,
                        Major = "Information Assurance"
                    }
                },
                new()
                {
                    Id = adminId,
                    Email = "admin@fpt.edu.vn",
                    PasswordHash = defaultPasswordHash,
                    FullName = "FPT Alumni Admin",
                    Role = "ADMIN",
                    AccountType = "Admin",
                    Status = "Active",
                    EmailVerified = true,
                    Profile = new UserProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = adminId,
                        DisplayName = "FPT Alumni Admin",
                        AvatarUrl = "https://cdn.falumni.local/avatars/admin.jpg",
                        StudentCode = "ADMIN01",
                        Major = "Administration",
                        CurrentCompany = "FPT University",
                        JobTitle = "System Administrator"
                    }
                }
            };

            await dbContext.Users.AddRangeAsync(seedUsers);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} default FPT Alumni users (Default Password: Password@123).", seedUsers.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database seeding skipped or encountered non-fatal error: {Message}", ex.Message);
        }
    }
}
