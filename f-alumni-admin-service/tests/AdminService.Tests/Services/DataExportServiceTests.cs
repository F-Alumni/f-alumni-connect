using System.Text;
using Microsoft.EntityFrameworkCore;
using AdminService.Repositories.Context;
using AdminService.Repositories.Entities;
using AdminService.Repositories.Implementations;
using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;
using AdminService.Services.HttpClients;
using AdminService.Services.Implementations;
using Xunit;

namespace AdminService.Tests.Services;

public class DataExportServiceTests : IDisposable
{
    private readonly AdminServiceDbContext _dbContext;
    private readonly DataExportService _service;

    public DataExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AdminServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AdminServiceDbContext(options);

        // Seed audit log
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = Guid.NewGuid(),
            AdminEmail = "admin@fpt.edu.vn",
            Action = "USER_SUSPEND",
            TargetEntity = "User",
            TargetId = "11111111-1111-1111-1111-111111111111",
            Reason = "Vi phạm quy tắc cộng đồng",
            CreatedAtUtc = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var auditRepo = new AuditLogRepository(_dbContext);
        var fakeClient = new FakeUserServiceClient();
        _service = new DataExportService(auditRepo, fakeClient);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task ExportAuditLogsCsvAsync_ReturnsValidCsvWithUtf8Bom()
    {
        // Act
        var bytes = await _service.ExportAuditLogsCsvAsync(new AuditLogFilterQuery());

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 3);

        // Verify UTF-8 BOM
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);

        var content = Encoding.UTF8.GetString(bytes[3..]);
        Assert.Contains("Id,AdminEmail,Action,TargetEntity", content);
        Assert.Contains("admin@fpt.edu.vn", content);
        Assert.Contains("Vi phạm quy tắc cộng đồng", content);
    }

    [Fact]
    public async Task ExportUsersSummaryCsvAsync_ReturnsValidCsvWithUtf8Bom()
    {
        // Act
        var bytes = await _service.ExportUsersSummaryCsvAsync(new UserFilterQuery());

        // Assert
        Assert.NotNull(bytes);
        var content = Encoding.UTF8.GetString(bytes[3..]);
        Assert.Contains("Id,Email,FullName,Role", content);
        Assert.Contains("alumni@fpt.edu.vn", content);
    }

    private sealed class FakeUserServiceClient : IUserServiceClient
    {
        public Task<PagedResponse<AdminUserSummaryDto>> GetUsersDirectoryAsync(
            UserFilterQuery query, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResponse<AdminUserSummaryDto>
            {
                Items = new List<AdminUserSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Email = "alumni@fpt.edu.vn",
                        FullName = "Nguyen Alumni",
                        Role = "USER",
                        AccountType = "Alumni",
                        Status = "Active",
                        Campus = "Hoa Lac",
                        CreatedAtUtc = DateTime.UtcNow
                    }
                },
                TotalCount = 1,
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
