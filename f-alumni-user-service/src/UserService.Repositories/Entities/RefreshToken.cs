namespace UserService.Repositories.Entities;

/// <summary>
/// Refresh token entity for renewing JWT access tokens and immediate revocation on suspension.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
