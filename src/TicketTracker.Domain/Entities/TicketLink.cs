namespace TicketTracker.Domain.Entities;

/// <summary>
/// A symmetric "Related" link between two tickets (Azure DevOps–style related work).
/// Stored once per pair; queried in both directions. Parent/child hierarchy is modelled
/// separately via <see cref="Ticket.ParentId"/>.
/// </summary>
public class TicketLink
{
    public Guid Id { get; set; }
    public Guid SourceTicketId { get; set; }
    public Guid TargetTicketId { get; set; }
    public DateTime CreatedAt { get; set; }
}
