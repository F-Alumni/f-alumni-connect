using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;
using AdminService.Services.HttpClients;
using AdminService.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminService.Services.Implementations;

public class UserModerationService : IUserModerationService
{
    private readonly IUserServiceClient _userServiceClient;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UserModerationService> _logger;

    public UserModerationService(
        IUserServiceClient userServiceClient,
        IAuditLogService auditLogService,
        ILogger<UserModerationService> logger)
    {
        _userServiceClient = userServiceClient;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<PagedResponse<AdminUserSummaryDto>> GetUsersListAsync(
        UserFilterQuery query,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        return await _userServiceClient.GetUsersDirectoryAsync(query, bearerToken, cancellationToken);
    }

    public async Task<AdminUserSummaryDto?> GetUserDetailAsync(
        Guid userId,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        return await _userServiceClient.GetUserByIdAsync(userId, bearerToken, cancellationToken);
    }

    public async Task<bool> SuspendUserAsync(
        Guid adminId,
        string adminEmail,
        Guid userId,
        SuspendUserRequest request,
        string? ipAddress = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin {Email} initiating suspension for user {UserId}. Reason: {Reason}",
            adminEmail, userId, request.Reason);

        var success = await _userServiceClient.SuspendUserAsync(userId, request, bearerToken, cancellationToken);
        if (!success)
        {
            _logger.LogWarning("User Service rejected or failed suspension for user {UserId}", userId);
            return false;
        }

        var details = request.DurationDays.HasValue ? $"DurationDays: {request.DurationDays.Value}" : null;
        await _auditLogService.LogActionAsync(
            adminId: adminId,
            adminEmail: adminEmail,
            action: "USER_SUSPEND",
            targetEntity: "User",
            targetId: userId.ToString(),
            reason: request.Reason,
            details: details,
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> UnsuspendUserAsync(
        Guid adminId,
        string adminEmail,
        Guid userId,
        UnsuspendUserRequest? request = null,
        string? ipAddress = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin {Email} initiating unsuspension for user {UserId}", adminEmail, userId);

        var success = await _userServiceClient.UnsuspendUserAsync(userId, request, bearerToken, cancellationToken);
        if (!success)
        {
            _logger.LogWarning("User Service rejected or failed unsuspension for user {UserId}", userId);
            return false;
        }

        var reason = !string.IsNullOrWhiteSpace(request?.Reason)
            ? request.Reason
            : "Account unsuspended by administrator";

        await _auditLogService.LogActionAsync(
            adminId: adminId,
            adminEmail: adminEmail,
            action: "USER_UNSUSPEND",
            targetEntity: "User",
            targetId: userId.ToString(),
            reason: reason,
            details: null,
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        return true;
    }
}
