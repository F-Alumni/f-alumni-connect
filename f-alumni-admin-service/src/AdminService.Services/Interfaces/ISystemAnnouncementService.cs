using AdminService.Services.DTOs.Announcements;
using AdminService.Services.DTOs.Common;

namespace AdminService.Services.Interfaces;

/// <summary>
/// Service contract for publishing and managing system-wide announcements to all students and alumni (AD-08).
/// </summary>
public interface ISystemAnnouncementService
{
    Task<PagedResponse<SystemAnnouncementResponse>> GetAnnouncementsAsync(
        AnnouncementFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<SystemAnnouncementResponse?> GetAnnouncementByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SystemAnnouncementResponse> CreateAnnouncementAsync(
        Guid adminId,
        string adminEmail,
        CreateAnnouncementRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<SystemAnnouncementResponse> UpdateAnnouncementAsync(
        Guid adminId,
        string adminEmail,
        Guid id,
        UpdateAnnouncementRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAnnouncementAsync(
        Guid adminId,
        string adminEmail,
        Guid id,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
