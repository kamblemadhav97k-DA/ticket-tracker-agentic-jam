namespace TicketTracker.Infrastructure.Identity;

/// <summary>
/// A single-use email-verification token. Only a hash of the raw token is stored.
/// A token is valid when it is unused (<see cref="ConsumedAt"/> is null) and the
/// current time is before <see cref="ExpiresAt"/>.
/// </summary>
public class EmailVerificationToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash (Base64) of the raw token; the raw token is never persisted.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
