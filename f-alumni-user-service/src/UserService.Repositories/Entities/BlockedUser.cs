namespace UserService.Repositories.Entities;

/// <summary>
/// US-10: Allows a user to block another account to restrict unwanted interactions.
/// </summary>
public class BlockedUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BlockerUserId { get; set; }

    public Guid BlockedTargetUserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
