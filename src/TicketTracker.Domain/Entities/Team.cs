using TicketTracker.Domain.Common;

namespace TicketTracker.Domain.Entities;

/// <summary>
/// A team groups epics and tickets. A team cannot be deleted while it still
/// contains epics or tickets (enforced by the application / referential integrity).
/// </summary>
public class Team : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Epic> Epics { get; set; } = new List<Epic>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
