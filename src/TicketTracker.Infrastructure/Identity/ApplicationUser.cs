using Microsoft.AspNetCore.Identity;

namespace TicketTracker.Infrastructure.Identity;

/// <summary>
/// Application user backed by ASP.NET Core Identity, keyed by <see cref="Guid"/>.
/// Email-verification, teams/tickets relationships, etc. are added in later milestones.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
}
