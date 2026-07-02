using TicketTracker.Domain.Common;
using TicketTracker.Domain.Enums;

namespace TicketTracker.Domain.Entities;

/// <summary>
/// A ticket belongs to a team and moves through the fixed Kanban workflow. It may
/// optionally reference an epic that belongs to the same team (enforced server-side).
/// </summary>
public class Ticket : AuditableEntity
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public Guid? EpicId { get; set; }
    public Epic? Epic { get; set; }

    /// <summary>
    /// Optional parent work item (Azure DevOps–style hierarchy). A ticket has at
    /// most one parent and may have many children; the parent must be in the same team.
    /// </summary>
    public Guid? ParentId { get; set; }
    public Ticket? Parent { get; set; }
    public ICollection<Ticket> Children { get; set; } = new List<Ticket>();

    public TicketType Type { get; set; }
    public TicketState State { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Id of the user who created the ticket (AspNetUsers.Id).</summary>
    public Guid CreatedById { get; set; }

    /// <summary>Optional teammate the work item is assigned to (AspNetUsers.Id).</summary>
    public Guid? AssignedToId { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
