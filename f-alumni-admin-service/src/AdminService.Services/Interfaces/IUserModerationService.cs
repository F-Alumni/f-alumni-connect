using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;

namespace AdminService.Services.Interfaces;

/// <summary>
/// Service contract for administrative user directory management and moderation (AD-02, AD-03, AD-04).
/// Orchestrates calls to the User microservice and records auditable administrative actions.
/// </summary>
public interface IUserModerationService
{
    Task<PagedResponse<AdminUserSummaryDto>> GetUsersListAsync(
        UserFilterQuery query,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);

    Task<AdminUserSummaryDto?> GetUserDetailAsync(
        Guid userId,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);

    Task<bool> SuspendUserAsync(
        Guid adminId,
        string adminEmail,
        Guid userId,
        SuspendUserRequest request,
        string? ipAddress = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);

    Task<bool> UnsuspendUserAsync(
        Guid adminId,
        string adminEmail,
        Guid userId,
        UnsuspendUserRequest? request = null,
        string? ipAddress = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);
}
