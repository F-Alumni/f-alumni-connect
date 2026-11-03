using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AdminService.Repositories.Context;
using AdminService.Repositories.Implementations;
using AdminService.Services.DTOs.Announcements;
using AdminService.Services.Exceptions;
using AdminService.Services.Implementations;
using Xunit;

namespace AdminService.Tests.Services;

public class SystemAnnouncementServiceTests : IDisposable
{
    private readonly AdminServiceDbContext _dbContext;
    private readonly SystemAnnouncementService _service;
    private readonly Guid _adminId = Guid.NewGuid();

    public SystemAnnouncementServiceTests()
    {
        var options = new DbContextOptionsBuilder<AdminServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AdminServiceDbContext(options);

        var announcementRepo = new SystemAnnouncementRepository(_dbContext);
        var auditRepo = new AuditLogRepository(_dbContext);
        var auditService = new AuditLogService(auditRepo, NullLogger<AuditLogService>.Instance);

        _service = new SystemAnnouncementService(announcementRepo, auditService, NullLogger<SystemAnnouncementService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateAnnouncementAsync_CreatesAnnouncement_AndLogsAuditAction()
    {
        // Arrange
        var request = new CreateAnnouncementRequest
        {
            Title = "Alumni Homecoming 2026",
            Content = "Grand gathering at Hoa Lac Campus",
            Type = "Event",
            Priority = "High",
            IsPublished = true
        };

        // Act
        var result = await _service.CreateAnnouncementAsync(
            _adminId, "admin@fpt.edu.vn", request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Alumni Homecoming 2026", result.Title);
        Assert.True(result.IsPublished);

        // Verify database persistence
        var entity = await _dbContext.SystemAnnouncements.FindAsync(result.Id);
        Assert.NotNull(entity);
        Assert.Equal("Event", entity.Type);

        // Verify audit log
        var logs = await _dbContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("CREATE_ANNOUNCEMENT", logs[0].Action);
        Assert.Equal(result.Id.ToString(), logs[0].TargetId);
    }

    [Fact]
    public async Task GetAnnouncementsAsync_ReturnsPagedList()
    {
        // Arrange
        await _service.CreateAnnouncementAsync(_adminId, "admin@fpt.edu.vn", new CreateAnnouncementRequest
        {
            Title = "Announcement 1",
            Content = "Content 1"
        });
        await _service.CreateAnnouncementAsync(_adminId, "admin@fpt.edu.vn", new CreateAnnouncementRequest
        {
            Title = "Announcement 2",
            Content = "Content 2"
        });

        // Act
        var result = await _service.GetAnnouncementsAsync(new AnnouncementFilterQuery());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task UpdateAnnouncementAsync_UpdatesProperties_AndLogsAuditAction()
    {
        // Arrange
        var created = await _service.CreateAnnouncementAsync(_adminId, "admin@fpt.edu.vn", new CreateAnnouncementRequest
        {
            Title = "Old Title",
            Content = "Old Content"
        });

        // Act
        var updated = await _service.UpdateAnnouncementAsync(_adminId, "admin@fpt.edu.vn", created.Id, new UpdateAnnouncementRequest
        {
            Title = "Updated Title",
            Content = "Updated Content"
        });

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated Content", updated.Content);

        // Verify audit log has 2 entries (create + update)
        var logs = await _dbContext.AuditLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Action == "UPDATE_ANNOUNCEMENT");
    }

    [Fact]
    public async Task DeleteAnnouncementAsync_RemovesAnnouncement_AndLogsAuditAction()
    {
        // Arrange
        var created = await _service.CreateAnnouncementAsync(_adminId, "admin@fpt.edu.vn", new CreateAnnouncementRequest
        {
            Title = "Temporary notice",
            Content = "Will be deleted"
        });

        // Act
        var success = await _service.DeleteAnnouncementAsync(_adminId, "admin@fpt.edu.vn", created.Id);

        // Assert
        Assert.True(success);
        var retrieved = await _service.GetAnnouncementByIdAsync(created.Id);
        Assert.Null(retrieved);

        // Verify delete audit log
        var logs = await _dbContext.AuditLogs.ToListAsync();
        Assert.Contains(logs, l => l.Action == "DELETE_ANNOUNCEMENT");
    }
}
