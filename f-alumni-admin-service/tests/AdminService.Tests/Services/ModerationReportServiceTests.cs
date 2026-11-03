using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AdminService.Repositories.Context;
using AdminService.Repositories.Entities;
using AdminService.Repositories.Implementations;
using AdminService.Services.DTOs.Moderation;
using AdminService.Services.Exceptions;
using AdminService.Services.Implementations;
using Xunit;

namespace AdminService.Tests.Services;

public class ModerationReportServiceTests : IDisposable
{
    private readonly AdminServiceDbContext _dbContext;
    private readonly ModerationReportService _service;
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _reportId = Guid.NewGuid();

    public ModerationReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AdminServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AdminServiceDbContext(options);

        // Seed initial moderation report
        _dbContext.ModerationReports.Add(new ModerationReport
        {
            Id = _reportId,
            ReporterUserId = Guid.NewGuid(),
            ReporterEmail = "student@fpt.edu.vn",
            TargetType = "Post",
            TargetId = Guid.NewGuid(),
            Reason = "Spam",
            Details = "Selling crypto tickets",
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var reportRepo = new ModerationReportRepository(_dbContext);
        var auditRepo = new AuditLogRepository(_dbContext);
        var auditService = new AuditLogService(auditRepo, NullLogger<AuditLogService>.Instance);

        _service = new ModerationReportService(reportRepo, auditService, NullLogger<ModerationReportService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetReportsAsync_ReturnsPagedReports()
    {
        // Act
        var result = await _service.GetReportsAsync(new ModerationReportFilterQuery { Status = "Pending" });

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Pending", result.Items[0].Status);
    }

    [Fact]
    public async Task GetReportByIdAsync_ExistingId_ReturnsReport()
    {
        // Act
        var report = await _service.GetReportByIdAsync(_reportId);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(_reportId, report.Id);
    }

    [Fact]
    public async Task ResolveReportAsync_UpdatesStatus_AndLogsAuditAction()
    {
        // Arrange
        var request = new ResolveReportRequest
        {
            ActionTaken = "ContentRemoved",
            ResolutionNotes = "Confirmed spamming rule violation"
        };

        // Act
        var result = await _service.ResolveReportAsync(
            _adminId, "admin@fpt.edu.vn", _reportId, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Resolved", result.Status);
        Assert.Equal(_adminId, result.ResolvedByAdminId);
        Assert.Contains("ContentRemoved", result.ResolutionNotes);

        // Verify audit log was recorded
        var auditLogs = await _dbContext.AuditLogs.ToListAsync();
        Assert.Single(auditLogs);
        Assert.Equal("REPORT_RESOLVE", auditLogs[0].Action);
        Assert.Equal("127.0.0.1", auditLogs[0].IpAddress);
    }

    [Fact]
    public async Task DismissReportAsync_UpdatesStatus_AndLogsAuditAction()
    {
        // Arrange
        var request = new DismissReportRequest
        {
            DismissalReason = "Content does not violate community guidelines"
        };

        // Act
        var result = await _service.DismissReportAsync(
            _adminId, "admin@fpt.edu.vn", _reportId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Dismissed", result.Status);
        Assert.Equal(_adminId, result.ResolvedByAdminId);

        // Verify audit log
        var auditLogs = await _dbContext.AuditLogs.ToListAsync();
        Assert.Single(auditLogs);
        Assert.Equal("REPORT_DISMISS", auditLogs[0].Action);
    }

    [Fact]
    public async Task ResolveReportAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ResolveReportAsync(_adminId, "admin@fpt.edu.vn", Guid.NewGuid(), new ResolveReportRequest
            {
                ActionTaken = "Test",
                ResolutionNotes = "Notes"
            }));
    }
}
