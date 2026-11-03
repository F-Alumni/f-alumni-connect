using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;

namespace AdminService.Services.HttpClients;

/// <summary>
/// HTTP REST client for synchronous inter-service communication with User Service (AD-02, AD-03, AD-04).
/// Calls internal endpoints without violating database isolation.
/// </summary>
public interface IUserServiceClient
{
    Task<PagedResponse<AdminUserSummaryDto>> GetUsersDirectoryAsync(
        UserFilterQuery query,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);

    Task<AdminUserSummaryDto?> GetUserByIdAsync(
        Guid userId,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);

    Task<bool> SuspendUserAsync(
        Guid userId,
        SuspendUserRequest request,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);

    Task<bool> UnsuspendUserAsync(
        Guid userId,
        UnsuspendUserRequest? request = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);
}
