using System.ComponentModel.DataAnnotations;

namespace TicketTracker.Application.Tickets;

public record TicketDto(
    Guid Id,
    Guid TeamId,
    Guid? EpicId,
    string Type,
    string State,
    string Title,
    string Body,
    Guid CreatedById,
    string? CreatedByEmail,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    Guid? ParentId,
    string? ParentTitle,
    int ChildCount,
    Guid? AssignedToId,
    string? AssignedToEmail);

/// <summary>Compact view of a linked work item for the links panel.</summary>
public record LinkedTicketDto(
    Guid Id,
    string Type,
    string State,
    string Title);

/// <summary>All links for a ticket: its parent, children, and symmetric related items.</summary>
public record TicketLinksDto(
    LinkedTicketDto? Parent,
    IReadOnlyList<LinkedTicketDto> Children,
    IReadOnlyList<LinkedTicketDto> Related);

/// <summary>Set (or clear, when null) a ticket's parent work item.</summary>
public record SetParentRequest(Guid? ParentId);

/// <summary>Add a symmetric "related" link to another ticket.</summary>
public record AddRelatedRequest([Required] Guid TargetId);

public record CreateTicketRequest(
    [Required] Guid TeamId,
    Guid? EpicId,
    [Required] string Type,
    string? State,
    [Required] string Title,
    [Required] string Body,
    Guid? AssignedToId = null);

public record UpdateTicketRequest(
    [Required] Guid TeamId,
    Guid? EpicId,
    [Required] string Type,
    [Required] string State,
    [Required] string Title,
    [Required] string Body,
    Guid? AssignedToId = null);

public record UpdateTicketStateRequest([Required] string State);

/// <summary>Board query for one team; filters combine with AND.</summary>
public record TicketFilter(Guid TeamId, string? Type, Guid? EpicId, string? Search);

public interface ITicketService
{
    Task<IReadOnlyList<TicketDto>> GetBoardAsync(TicketFilter filter, CancellationToken ct = default);
    Task<TicketDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TicketDto> CreateAsync(CreateTicketRequest request, Guid currentUserId, CancellationToken ct = default);
    Task<TicketDto> UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct = default);
    Task<TicketDto> UpdateStateAsync(Guid id, UpdateTicketStateRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // ---- Work-item links --------------------------------------------------
    Task<TicketLinksDto> GetLinksAsync(Guid id, CancellationToken ct = default);
    Task<TicketDto> SetParentAsync(Guid id, SetParentRequest request, CancellationToken ct = default);
    Task<TicketLinksDto> AddRelatedAsync(Guid id, AddRelatedRequest request, CancellationToken ct = default);
    Task RemoveRelatedAsync(Guid id, Guid targetId, CancellationToken ct = default);
}
