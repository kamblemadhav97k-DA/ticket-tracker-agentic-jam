namespace TicketTracker.Application.Common.Interfaces;

/// <summary>
/// Manages single-use, 24-hour email-verification tokens. Issuing a new token
/// invalidates any earlier unused tokens for the same user.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Issues a fresh verification token for the user, invalidating earlier unused
    /// tokens, and returns the raw token to embed in the verification link.
    /// </summary>
    Task<string> IssueTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes a raw token. Returns the owning user id when the token is valid,
    /// unused, and unexpired (marking it used); otherwise returns <c>null</c>.
    /// </summary>
    Task<Guid?> ConsumeTokenAsync(string rawToken, CancellationToken cancellationToken = default);
}
