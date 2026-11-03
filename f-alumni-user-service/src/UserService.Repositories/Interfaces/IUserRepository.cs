using UserService.Repositories.Entities;

namespace UserService.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetDirectoryPagedAsync(
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
    Task<VerificationToken> CreateVerificationTokenAsync(VerificationToken token, CancellationToken cancellationToken = default);
    Task<VerificationToken?> GetValidVerificationTokenAsync(Guid userId, string otpCode, string purpose, CancellationToken cancellationToken = default);
    Task MarkVerificationTokenUsedAsync(VerificationToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BlockedUser> BlockUserAsync(Guid blockerUserId, Guid targetUserId, string reason, CancellationToken cancellationToken = default);
    Task<bool> UnblockUserAsync(Guid blockerUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlockedUser>> GetBlockedUsersAsync(Guid blockerUserId, CancellationToken cancellationToken = default);
}
