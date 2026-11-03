using AdminService.Repositories.Entities;
using AdminService.Repositories.Interfaces;
using AdminService.Services.DTOs.Announcements;
using AdminService.Services.DTOs.Common;
using AdminService.Services.Exceptions;
using AdminService.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminService.Services.Implementations;

public class SystemAnnouncementService : ISystemAnnouncementService
{
    private readonly ISystemAnnouncementRepository _announcementRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SystemAnnouncementService> _logger;

    public SystemAnnouncementService(
        ISystemAnnouncementRepository announcementRepository,
        IAuditLogService auditLogService,
        ILogger<SystemAnnouncementService> logger)
    {
        _announcementRepository = announcementRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<PagedResponse<SystemAnnouncementResponse>> GetAnnouncementsAsync(
        AnnouncementFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _announcementRepository.GetPagedAsync(
            includeUnpublished: query.IncludeUnpublished,
            type: query.Type,
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            cancellationToken: cancellationToken);

        return new PagedResponse<SystemAnnouncementResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SystemAnnouncementResponse?> GetAnnouncementByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var announcement = await _announcementRepository.GetByIdAsync(id, cancellationToken);
        return announcement is null ? null : MapToResponse(announcement);
    }

    public async Task<SystemAnnouncementResponse> CreateAnnouncementAsync(
        Guid adminId,
        string adminEmail,
        CreateAnnouncementRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var announcement = new SystemAnnouncement
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "General" : request.Type,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority,
            CreatedByAdminId = adminId,
            CreatedByAdminEmail = adminEmail,
            IsPublished = request.IsPublished,
            PublishedAtUtc = request.IsPublished ? DateTime.UtcNow : null,
            ExpiresAtUtc = request.ExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        var saved = await _announcementRepository.AddAsync(announcement, cancellationToken);

        await _auditLogService.LogActionAsync(
            adminId: adminId,
            adminEmail: adminEmail,
            action: "CREATE_ANNOUNCEMENT",
            targetEntity: "SystemAnnouncement",
            targetId: saved.Id.ToString(),
            reason: $"Created announcement: {saved.Title}",
            details: $"Type: {saved.Type}, Priority: {saved.Priority}, Published: {saved.IsPublished}",
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        _logger.LogInformation("System announcement {Id} '{Title}' created by admin {Email}",
            saved.Id, saved.Title, adminEmail);

        return MapToResponse(saved);
    }

    public async Task<SystemAnnouncementResponse> UpdateAnnouncementAsync(
        Guid adminId,
        string adminEmail,
        Guid id,
        UpdateAnnouncementRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var announcement = await _announcementRepository.GetByIdAsync(id, cancellationToken);
        if (announcement is null)
        {
            throw new NotFoundException($"System announcement '{id}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Title)) announcement.Title = request.Title;
        if (!string.IsNullOrWhiteSpace(request.Content)) announcement.Content = request.Content;
        if (!string.IsNullOrWhiteSpace(request.Type)) announcement.Type = request.Type;
        if (!string.IsNullOrWhiteSpace(request.Priority)) announcement.Priority = request.Priority;
        if (request.ExpiresAtUtc.HasValue) announcement.ExpiresAtUtc = request.ExpiresAtUtc.Value;

        if (request.IsPublished.HasValue)
        {
            announcement.IsPublished = request.IsPublished.Value;
            if (announcement.IsPublished && announcement.PublishedAtUtc is null)
            {
                announcement.PublishedAtUtc = DateTime.UtcNow;
            }
        }

        announcement.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await _announcementRepository.UpdateAsync(announcement, cancellationToken);

        await _auditLogService.LogActionAsync(
            adminId: adminId,
            adminEmail: adminEmail,
            action: "UPDATE_ANNOUNCEMENT",
            targetEntity: "SystemAnnouncement",
            targetId: id.ToString(),
            reason: $"Updated announcement: {updated.Title}",
            details: null,
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        _logger.LogInformation("System announcement {Id} updated by admin {Email}", id, adminEmail);

        return MapToResponse(updated);
    }

    public async Task<bool> DeleteAnnouncementAsync(
        Guid adminId,
        string adminEmail,
        Guid id,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var announcement = await _announcementRepository.GetByIdAsync(id, cancellationToken);
        if (announcement is null)
        {
            throw new NotFoundException($"System announcement '{id}' was not found.");
        }

        var deleted = await _announcementRepository.DeleteAsync(id, cancellationToken);
        if (deleted)
        {
            await _auditLogService.LogActionAsync(
                adminId: adminId,
                adminEmail: adminEmail,
                action: "DELETE_ANNOUNCEMENT",
                targetEntity: "SystemAnnouncement",
                targetId: id.ToString(),
                reason: $"Deleted announcement: {announcement.Title}",
                details: null,
                ipAddress: ipAddress,
                cancellationToken: cancellationToken);

            _logger.LogInformation("System announcement {Id} deleted by admin {Email}", id, adminEmail);
        }

        return deleted;
    }

    private static SystemAnnouncementResponse MapToResponse(SystemAnnouncement a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Content = a.Content,
        Type = a.Type,
        Priority = a.Priority,
        CreatedByAdminId = a.CreatedByAdminId,
        CreatedByAdminEmail = a.CreatedByAdminEmail,
        IsPublished = a.IsPublished,
        PublishedAtUtc = a.PublishedAtUtc,
        ExpiresAtUtc = a.ExpiresAtUtc,
        CreatedAtUtc = a.CreatedAtUtc,
        UpdatedAtUtc = a.UpdatedAtUtc
    };
}
