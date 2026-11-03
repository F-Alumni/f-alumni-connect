using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AdminService.Repositories.Context;
using AdminService.Repositories.Entities;
using AdminService.Repositories.Implementations;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;
using AdminService.Services.HttpClients;
using AdminService.Services.Implementations;
using Xunit;

namespace AdminService.Tests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly AdminServiceDbContext _dbContext;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AdminServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AdminServiceDbContext(options);

        // Seed data
        _dbContext.ModerationReports.Add(new ModerationReport
        {
            Id = Guid.NewGuid(),
            Reason = "Spam",
            Status = "Pending"
        });
        _dbContext.SystemAnnouncements.Add(new SystemAnnouncement
        {
            Id = Guid.NewGuid(),
            Title = "Welcome",
            IsPublished = true
        });
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "USER_SUSPEND",
            TargetEntity = "User",
            TargetId = "user-1",
            CreatedAtUtc = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var reportRepo = new ModerationReportRepository(_dbContext);
        var announcementRepo = new SystemAnnouncementRepository(_dbContext);
        var auditRepo = new AuditLogRepository(_dbContext);
        var fakeClient = new FakeUserServiceClient();

        _service = new DashboardService(fakeClient, reportRepo, announcementRepo, auditRepo, NullLogger<DashboardService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetSummaryAsync_AggregatesMetrics()
    {
        // Act
        var summary = await _service.GetSummaryAsync();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(10, summary.TotalUsers);
        Assert.Equal(8, summary.ActiveUsers);
        Assert.Equal(2, summary.SuspendedUsers);
        Assert.Equal(1, summary.PendingReports);
        Assert.Equal(1, summary.PublishedAnnouncements);
        Assert.Equal(1, summary.TotalAuditLogsCount);
        Assert.NotEmpty(summary.RecentActivityFeed);
    }

    [Fact]
    public async Task GetActivityTimelineAsync_ReturnsItemForEveryDay()
    {
        // Act
        var timeline = await _service.GetActivityTimelineAsync(7);

        // Assert
        Assert.NotNull(timeline);
        Assert.Equal(7, timeline.Count);
        Assert.Contains(timeline, t => t.TotalActions > 0);
    }

    private sealed class FakeUserServiceClient : IUserServiceClient
    {
        public Task<PagedResponse<AdminUserSummaryDto>> GetUsersDirectoryAsync(
            UserFilterQuery query, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            var count = query.Status switch
            {
                "Active" => 8,
                "Suspended" => 2,
                _ => 10
            };

            return Task.FromResult(new PagedResponse<AdminUserSummaryDto>
            {
                TotalCount = count,
                PageNumber = 1,
                PageSize = query.PageSize
            });
        }

        public Task<AdminUserSummaryDto?> GetUserByIdAsync(Guid userId, string? bearerToken = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminUserSummaryDto?>(null);

        public Task<bool> SuspendUserAsync(Guid userId, SuspendUserRequest request, string? bearerToken = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> UnsuspendUserAsync(Guid userId, UnsuspendUserRequest? request = null, string? bearerToken = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
