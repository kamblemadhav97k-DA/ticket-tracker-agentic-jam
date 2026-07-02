using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TicketTracker.Domain.Entities;
using TicketTracker.Infrastructure.Identity;

namespace TicketTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core database context: ASP.NET Core Identity schema plus the business
/// entities (teams, epics, tickets, comments) and email-verification tokens.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Epic> Epics => Set<Epic>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketLink> TicketLinks => Set<TicketLink>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
