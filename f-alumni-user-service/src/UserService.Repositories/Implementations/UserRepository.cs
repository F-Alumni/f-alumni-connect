using Microsoft.EntityFrameworkCore;
using UserService.Repositories.Context;
using UserService.Repositories.Entities;
using UserService.Repositories.Interfaces;

namespace UserService.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly UserServiceDbContext _dbContext;

    public UserRepository(UserServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _dbContext.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAtUtc = DateTime.UtcNow;
        if (user.Profile is not null)
        {
            user.Profile.UpdatedAtUtc = DateTime.UtcNow;
        }
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetDirectoryPagedAsync(
        string? search,
        string? campus,
        int? graduationYear,
        string? major,
        string? company,
        string? status,
        string? role,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                (u.Profile != null && (
                    u.Profile.DisplayName.ToLower().Contains(term) ||
                    u.Profile.StudentCode.ToLower().Contains(term) ||
                    u.Profile.Campus.ToLower().Contains(term) ||
                    u.Profile.Skills.ToLower().Contains(term) ||
                    u.Profile.CurrentCompany.ToLower().Contains(term) ||
                    u.Profile.Major.ToLower().Contains(term))));
        }

        if (!string.IsNullOrWhiteSpace(campus))
        {
            var cp = campus.Trim().ToLowerInvariant();
            query = query.Where(u => u.Profile != null && u.Profile.Campus.ToLower() == cp);
        }

        if (graduationYear.HasValue)
        {
            query = query.Where(u => u.Profile != null && u.Profile.GraduationYear == graduationYear.Value);
        }

        if (!string.IsNullOrWhiteSpace(major))
        {
            var m = major.Trim().ToLowerInvariant();
            query = query.Where(u => u.Profile != null && u.Profile.Major.ToLower().Contains(m));
        }

        if (!string.IsNullOrWhiteSpace(company))
        {
            var c = company.Trim().ToLowerInvariant();
            query = query.Where(u => u.Profile != null && u.Profile.CurrentCompany.ToLower().Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            query = query.Where(u => u.Status.ToLower() == s);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim().ToLowerInvariant();
            query = query.Where(u => u.Role.ToLower() == r || u.AccountType.ToLower() == r);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<VerificationToken> CreateVerificationTokenAsync(VerificationToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.VerificationTokens.AddAsync(token, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<VerificationToken?> GetValidVerificationTokenAsync(
        Guid userId,
        string otpCode,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.VerificationTokens
            .Where(t => t.UserId == userId
                     && t.OtpCode == otpCode
                     && t.Purpose == purpose
                     && !t.IsUsed
                     && t.ExpiresAtUtc >= now)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task MarkVerificationTokenUsedAsync(VerificationToken token, CancellationToken cancellationToken = default)
    {
        token.IsUsed = true;
        _dbContext.VerificationTokens.Update(token);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens
            .Include(r => r.User)
            .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        refreshToken.IsRevoked = true;
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
        }

        if (tokens.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<BlockedUser> BlockUserAsync(Guid blockerUserId, Guid targetUserId, string reason, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.BlockedUsers
            .FirstOrDefaultAsync(b => b.BlockerUserId == blockerUserId && b.BlockedTargetUserId == targetUserId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var block = new BlockedUser
        {
            Id = Guid.NewGuid(),
            BlockerUserId = blockerUserId,
            BlockedTargetUserId = targetUserId,
            Reason = reason,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.BlockedUsers.AddAsync(block, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return block;
    }

    public async Task<bool> UnblockUserAsync(Guid blockerUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.BlockedUsers
            .FirstOrDefaultAsync(b => b.BlockerUserId == blockerUserId && b.BlockedTargetUserId == targetUserId, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        _dbContext.BlockedUsers.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<BlockedUser>> GetBlockedUsersAsync(Guid blockerUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BlockedUsers
            .AsNoTracking()
            .Where(b => b.BlockerUserId == blockerUserId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
