using System.Text.Json;
using AdminService.Repositories.Context;
using AdminService.Repositories.Implementations;
using AdminService.Services.Implementations;
using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AdminService.Tests.Services;

public class ReportEventProcessorTests : IDisposable
{
    private readonly AdminServiceDbContext _dbContext;
    private readonly ReportEventProcessor _processor;

    public ReportEventProcessorTests()
    {
        var options = new DbContextOptionsBuilder<AdminServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AdminServiceDbContext(options);
        var repository = new ModerationReportRepository(_dbContext);
        _processor = new ReportEventProcessor(repository, NullLogger<ReportEventProcessor>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task ProcessRawEventAsync_ValidReportCreatedEnvelope_PersistsIdempotently()
    {
        var reportId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc);

        var envelope = new EventEnvelope<ReportCreatedEventV1>(
            eventType: ReportCreatedEventV1.EventTypeName,
            version: ReportCreatedEventV1.CurrentVersion,
            producer: "f-alumni-forum-service",
            data: new ReportCreatedEventV1
            {
                ReportId = reportId,
                TargetType = "Post",
                TargetId = targetId,
                ReporterId = reporterId,
                ReporterName = "Nguyen Van A",
                Reason = "Spam",
                Details = "Posting commercial links repeatedly",
                Status = "Pending",
                CreatedAt = createdAt
            },
            correlationId: reportId.ToString());

        var json = JsonSerializer.Serialize(envelope);

        // First delivery persists the report
        var insertedFirst = await _processor.ProcessRawEventAsync(ReportCreatedEventV1.DefaultTopic, json);
        Assert.True(insertedFirst);

        // Replayed delivery is ignored idempotently without duplicate IDs
        var insertedSecond = await _processor.ProcessRawEventAsync(ReportCreatedEventV1.DefaultTopic, json);
        Assert.False(insertedSecond);

        var reports = await _dbContext.ModerationReports.ToListAsync();
        Assert.Single(reports);

        var saved = reports[0];
        Assert.Equal(reportId, saved.Id);
        Assert.Equal("Post", saved.TargetType);
        Assert.Equal(targetId, saved.TargetId);
        Assert.Equal(reporterId, saved.ReporterUserId);
        Assert.Equal("Nguyen Van A", saved.ReporterEmail);
        Assert.Equal("Spam", saved.Reason);
        Assert.Equal("Posting commercial links repeatedly", saved.Details);
        Assert.Equal("Pending", saved.Status);
        Assert.Equal(createdAt, saved.CreatedAtUtc);
    }

    [Fact]
    public async Task ProcessRawEventAsync_MalformedOrInvalidPayloads_SkipWithoutThrowing()
    {
        Assert.False(await _processor.ProcessRawEventAsync(ReportCreatedEventV1.DefaultTopic, ""));
        Assert.False(await _processor.ProcessRawEventAsync(ReportCreatedEventV1.DefaultTopic, "{invalid-json"));

        var wrongEventType = JsonSerializer.Serialize(new EventEnvelope<ReportCreatedEventV1>(
            eventType: "forum.post_created.v1",
            version: "1.0",
            producer: "f-alumni-forum-service",
            data: new ReportCreatedEventV1
            {
                ReportId = Guid.NewGuid(),
                TargetId = Guid.NewGuid(),
                ReporterId = Guid.NewGuid(),
                Reason = "Spam"
            }));
        Assert.False(await _processor.ProcessRawEventAsync(ReportCreatedEventV1.DefaultTopic, wrongEventType));

        var missingTargetId = JsonSerializer.Serialize(new EventEnvelope<ReportCreatedEventV1>(
            eventType: ReportCreatedEventV1.EventTypeName,
            version: "1.0",
            producer: "f-alumni-forum-service",
            data: new ReportCreatedEventV1
            {
                ReportId = Guid.NewGuid(),
                TargetId = Guid.Empty,
                ReporterId = Guid.NewGuid(),
                Reason = "Spam"
            }));
        Assert.False(await _processor.ProcessRawEventAsync(ReportCreatedEventV1.DefaultTopic, missingTargetId));

        Assert.Empty(await _dbContext.ModerationReports.ToListAsync());
    }
}
