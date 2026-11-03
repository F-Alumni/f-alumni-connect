using UserService.Services.DTOs.Common;
using UserService.Services.DTOs.Requests;
using UserService.Services.DTOs.Responses;

namespace UserService.Services.Interfaces;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(Guid userId, LogoutRequest? request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<RegisterResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public interface IUserProfileAppService
{
    Task<UserProfileResponse> GetByIdAsync(Guid userId, bool isOwnerOrAdmin = false, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateCalendarLinkAsync(Guid userId, UpdateCalendarLinkRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdatePrivacySettingsAsync(Guid userId, UpdatePrivacySettingsRequest request, CancellationToken cancellationToken = default);
    Task<BlockedUserResponse> BlockUserAsync(Guid blockerUserId, Guid targetUserId, BlockUserRequest request, CancellationToken cancellationToken = default);
    Task UnblockUserAsync(Guid blockerUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlockedUserResponse>> GetBlockedUsersAsync(Guid blockerUserId, CancellationToken cancellationToken = default);
    Task<PagedResponse<UserProfileResponse>> GetDirectoryPagedAsync(
        string? search,
        string? campus,
        int? graduationYear,
        string? major,
        string? company,
        string? status,
        string? role,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<UserProfileResponse> SuspendUserAsync(Guid userId, SuspendUserRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UnsuspendUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
}
