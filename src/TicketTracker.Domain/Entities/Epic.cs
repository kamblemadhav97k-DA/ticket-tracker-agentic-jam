using TicketTracker.Domain.Common;

namespace TicketTracker.Domain.Entities;

/// <summary>
/// An epic belongs to exactly one team (fixed at creation). It cannot be deleted
/// while tickets reference it.
/// </summary>
public class Epic : AuditableEntity
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
