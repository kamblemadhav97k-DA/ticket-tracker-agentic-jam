namespace TicketTracker.Domain.Common;

/// <summary>
/// Base type for entities that track creation and last-modification timestamps.
/// Timestamps are set by the server in UTC.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
