using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;
using AdminService.Services.HttpClients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AdminService.Tests.Endpoints;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Deterministic signing key strictly reserved for automated testing (xUnit test fixture).
    /// WARNING: DO NOT USE IN RUNTIME, DEVELOPMENT, OR PRODUCTION ENVIRONMENTS.
    /// Real runtime signing keys must be randomly generated and supplied via User Secrets or environment variables.
    /// </summary>
    public const string TestJwtSecret = "TestFixtureDeterministicKey_AtLeast32BytesLong_PRN232_ASM_2026!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DatabaseProvider", "InMemory");
        builder.UseSetting("Jwt:Key", TestJwtSecret);
        builder.UseSetting("Kafka:BootstrapServers", "");
        builder.UseSetting("Services:UserService:Url", "http://127.0.0.1:59999");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserServiceClient>();
            services.AddSingleton<IUserServiceClient, InMemoryTestUserServiceClient>();
        });
    }

    private sealed class InMemoryTestUserServiceClient : IUserServiceClient
    {
        private readonly List<AdminUserSummaryDto> _users =
        [
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "alumni.test@fpt.edu.vn",
                FullName = "Alumni Test User",
                Role = "ALUMNI",
                AccountType = "Alumni",
                Campus = "FU-HCM",
                Major = "SE",
                GraduationYear = 2022,
                Status = "Active",
                EmailVerified = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30)
            }
        ];

        public Task<PagedResponse<AdminUserSummaryDto>> GetUsersDirectoryAsync(
            UserFilterQuery query,
            string? bearerToken = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResponse<AdminUserSummaryDto>
            {
                Items = _users,
                TotalCount = _users.Count,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<AdminUserSummaryDto?> GetUserByIdAsync(
            Guid userId,
            string? bearerToken = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));
        }

        public Task<bool> SuspendUserAsync(
            Guid userId,
            SuspendUserRequest request,
            string? bearerToken = null,
            CancellationToken cancellationToken = default)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Task.FromResult(false);
            user.Status = "Suspended";
            user.SuspensionReason = request.Reason;
            return Task.FromResult(true);
        }

        public Task<bool> UnsuspendUserAsync(
            Guid userId,
            UnsuspendUserRequest? request = null,
            string? bearerToken = null,
            CancellationToken cancellationToken = default)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Task.FromResult(false);
            user.Status = "Active";
            user.SuspensionReason = null;
            return Task.FromResult(true);
        }
    }
}
