using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AdminService.Repositories.Context;
using AdminService.Repositories.Implementations;
using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.Implementations;
using Xunit;

namespace AdminService.Tests.Services;

public class AuditLogServiceTests : IDisposable
{
    private readonly AdminServiceDbContext _dbContext;
    private readonly AuditLogService _service;
    private readonly Guid _adminId = Guid.NewGuid();

    public AuditLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AdminServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AdminServiceDbContext(options);
        var repository = new AuditLogRepository(_dbContext);
        _service = new AuditLogService(repository, NullLogger<AuditLogService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task LogActionAsync_PersistsAuditLog_AndReturnsResponse()
    {
        // Act
        var result = await _service.LogActionAsync(
            adminId: _adminId,
            adminEmail: "admin@fpt.edu.vn",
            action: "USER_SUSPEND",
            targetEntity: "User",
            targetId: Guid.NewGuid().ToString(),
            reason: "Violation of terms",
            details: "Duration: 7 days",
            ipAddress: "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_adminId, result.AdminUserId);
        Assert.Equal("admin@fpt.edu.vn", result.AdminEmail);
        Assert.Equal("USER_SUSPEND", result.Action);
        Assert.Equal("User", result.TargetEntity);
        Assert.Equal("Violation of terms", result.Reason);
        Assert.Equal("127.0.0.1", result.IpAddress);

        // Verify database persistence
        var entity = await _dbContext.AuditLogs.FindAsync(result.Id);
        Assert.NotNull(entity);
        Assert.Equal("USER_SUSPEND", entity.Action);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        await _service.LogActionAsync(_adminId, "admin@fpt.edu.vn", "USER_SUSPEND", "User", "user-1", "Spam");
        await _service.LogActionAsync(_adminId, "admin@fpt.edu.vn", "USER_UNSUSPEND", "User", "user-1", "Appeal accepted");
        await _service.LogActionAsync(_adminId, "admin@fpt.edu.vn", "ANNOUNCEMENT_CREATE", "Announcement", "ann-1", "Broadcast");

        // Act - filter by action "USER_SUSPEND"
        var result = await _service.GetAuditLogsAsync(new AuditLogFilterQuery
        {
            Action = "USER_SUSPEND",
            PageNumber = 1,
            PageSize = 10
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("USER_SUSPEND", result.Items[0].Action);
    }

    [Fact]
    public async Task GetAuditLogByIdAsync_ExistingId_ReturnsLog()
    {
        // Arrange
        var created = await _service.LogActionAsync(_adminId, "admin@fpt.edu.vn", "SYSTEM_SETTING_UPDATE", "SystemSetting", "setting-1");

        // Act
        var result = await _service.GetAuditLogByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("SYSTEM_SETTING_UPDATE", result.Action);
    }

    [Fact]
    public async Task GetAuditLogByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetAuditLogByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecentActionsAsync_ReturnsSortedDescending()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            await _service.LogActionAsync(_adminId, "admin@fpt.edu.vn", $"ACTION_{i}", "Target", $"id-{i}");
        }

        // Act
        var recent = await _service.GetRecentActionsAsync(3);

        // Assert
        Assert.NotNull(recent);
        Assert.Equal(3, recent.Count);
        Assert.Equal("ACTION_5", recent[0].Action);
        Assert.Equal("ACTION_4", recent[1].Action);
        Assert.Equal("ACTION_3", recent[2].Action);
    }
}
