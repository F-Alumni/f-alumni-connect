using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AdminService.Repositories.Context;
using AdminService.Repositories.Implementations;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;
using AdminService.Services.HttpClients;
using AdminService.Services.Implementations;
using Xunit;

namespace AdminService.Tests.Services;

public class UserModerationServiceTests : IDisposable
{
    private readonly AdminServiceDbContext _dbContext;
    private readonly AuditLogService _auditLogService;
    private readonly FakeUserServiceClient _fakeClient;
    private readonly UserModerationService _service;
    private readonly Guid _adminId = Guid.NewGuid();

    public UserModerationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AdminServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AdminServiceDbContext(options);
        var auditRepo = new AuditLogRepository(_dbContext);
        _auditLogService = new AuditLogService(auditRepo, NullLogger<AuditLogService>.Instance);
        _fakeClient = new FakeUserServiceClient();
        _service = new UserModerationService(_fakeClient, _auditLogService, NullLogger<UserModerationService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetUsersListAsync_DelegatesToUserServiceClient()
    {
        // Act
        var result = await _service.GetUsersListAsync(new UserFilterQuery());

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("test@fpt.edu.vn", result.Items[0].Email);
    }

    [Fact]
    public async Task GetUserDetailAsync_DelegatesToUserServiceClient()
    {
        // Arrange
        var targetId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserDetailAsync(targetId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetId, result.Id);
    }

    [Fact]
    public async Task SuspendUserAsync_WhenClientSucceeds_WritesAuditLogAndReturnsTrue()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var request = new SuspendUserRequest
        {
            Reason = "Spamming offensive comments",
            DurationDays = 14
        };

        // Act
        var result = await _service.SuspendUserAsync(
            _adminId, "admin@fpt.edu.vn", targetUserId, request, "192.168.1.1");

        // Assert
        Assert.True(result);
        Assert.Equal(targetUserId, _fakeClient.LastSuspendedUserId);

        // Verify audit trail was persisted
        var logs = await _dbContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("USER_SUSPEND", logs[0].Action);
        Assert.Equal(targetUserId.ToString(), logs[0].TargetId);
        Assert.Equal("Spamming offensive comments", logs[0].Reason);
        Assert.Equal("192.168.1.1", logs[0].IpAddress);
        Assert.Contains("DurationDays: 14", logs[0].Details);
    }

    [Fact]
    public async Task SuspendUserAsync_WhenClientFails_DoesNotWriteAuditLogAndReturnsFalse()
    {
        // Arrange
        _fakeClient.SuspendResult = false;
        var targetUserId = Guid.NewGuid();
        var request = new SuspendUserRequest { Reason = "Spam" };

        // Act
        var result = await _service.SuspendUserAsync(
            _adminId, "admin@fpt.edu.vn", targetUserId, request);

        // Assert
        Assert.False(result);

        // Verify no audit log was created for failed suspension
        var count = await _dbContext.AuditLogs.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task UnsuspendUserAsync_WhenClientSucceeds_WritesAuditLogAndReturnsTrue()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var request = new UnsuspendUserRequest { Reason = "Ban period elapsed" };

        // Act
        var result = await _service.UnsuspendUserAsync(
            _adminId, "admin@fpt.edu.vn", targetUserId, request, "10.0.0.1");

        // Assert
        Assert.True(result);
        Assert.Equal(targetUserId, _fakeClient.LastUnsuspendedUserId);

        var logs = await _dbContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("USER_UNSUSPEND", logs[0].Action);
        Assert.Equal(targetUserId.ToString(), logs[0].TargetId);
        Assert.Equal("Ban period elapsed", logs[0].Reason);
    }

    private sealed class FakeUserServiceClient : IUserServiceClient
    {
        public bool SuspendResult { get; set; } = true;
        public bool UnsuspendResult { get; set; } = true;
        public Guid? LastSuspendedUserId { get; private set; }
        public Guid? LastUnsuspendedUserId { get; private set; }

        public Task<PagedResponse<AdminUserSummaryDto>> GetUsersDirectoryAsync(
            UserFilterQuery query, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResponse<AdminUserSummaryDto>
            {
                Items = new List<AdminUserSummaryDto>
                {
                    new() { Id = Guid.NewGuid(), Email = "test@fpt.edu.vn", Status = "Active" }
                },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            });
        }

        public Task<AdminUserSummaryDto?> GetUserByIdAsync(
            Guid userId, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminUserSummaryDto?>(new AdminUserSummaryDto
            {
                Id = userId,
                Email = "test@fpt.edu.vn",
                Status = "Active"
            });
        }

        public Task<bool> SuspendUserAsync(
            Guid userId, SuspendUserRequest request, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            LastSuspendedUserId = userId;
            return Task.FromResult(SuspendResult);
        }

        public Task<bool> UnsuspendUserAsync(
            Guid userId, UnsuspendUserRequest? request = null, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            LastUnsuspendedUserId = userId;
            return Task.FromResult(UnsuspendResult);
        }
    }
}
