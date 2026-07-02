namespace TicketTracker.Domain.Entities;

/// <summary>
/// A comment on a ticket. Immutable after creation (mandatory scope). Adding a
/// comment does not advance the ticket's modified timestamp.
/// </summary>
public class Comment
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>Id of the comment author (AspNetUsers.Id).</summary>
    public Guid AuthorId { get; set; }

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
