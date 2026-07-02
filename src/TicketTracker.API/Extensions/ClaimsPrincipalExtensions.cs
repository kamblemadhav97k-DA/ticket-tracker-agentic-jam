using System.Security.Claims;

namespace TicketTracker.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Resolves the authenticated user's id from the JWT. The bearer handler maps
    /// the <c>sub</c> claim to <see cref="ClaimTypes.NameIdentifier"/> by default.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");

        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The authenticated user has no valid identifier claim.");
    }
}
