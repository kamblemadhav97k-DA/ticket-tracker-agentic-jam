using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketTracker.API.Extensions;
using TicketTracker.Application.Tickets;

namespace TicketTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public class TicketsController(ITicketService tickets) : ControllerBase
{
    /// <summary>
    /// Board query for one team. Optional filters (type, epicId, search over title)
    /// combine with AND; results are ordered most-recently-modified first.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetBoard(
        [FromQuery] Guid teamId,
        [FromQuery] string? type,
        [FromQuery] Guid? epicId,
        [FromQuery] string? search,
        CancellationToken ct) =>
        Ok(await tickets.GetBoardAsync(new TicketFilter(teamId, type, epicId, search), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await tickets.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create(CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = await tickets.CreateAsync(request, User.GetUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TicketDto>> Update(Guid id, UpdateTicketRequest request, CancellationToken ct) =>
        Ok(await tickets.UpdateAsync(id, request, ct));

    /// <summary>Persist a state change (e.g. Kanban drag-and-drop).</summary>
    [HttpPatch("{id:guid}/state")]
    public async Task<ActionResult<TicketDto>> UpdateState(Guid id, UpdateTicketStateRequest request, CancellationToken ct) =>
        Ok(await tickets.UpdateStateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tickets.DeleteAsync(id, ct);
        return NoContent();
    }

    // ---- Work-item links ------------------------------------------------------

    /// <summary>Parent, children, and related work items for a ticket.</summary>
    [HttpGet("{id:guid}/links")]
    public async Task<ActionResult<TicketLinksDto>> GetLinks(Guid id, CancellationToken ct) =>
        Ok(await tickets.GetLinksAsync(id, ct));

    /// <summary>Set (or clear, when ParentId is null) the ticket's parent work item.</summary>
    [HttpPut("{id:guid}/parent")]
    public async Task<ActionResult<TicketDto>> SetParent(Guid id, SetParentRequest request, CancellationToken ct) =>
        Ok(await tickets.SetParentAsync(id, request, ct));

    /// <summary>Add a symmetric "related" link to another ticket.</summary>
    [HttpPost("{id:guid}/links")]
    public async Task<ActionResult<TicketLinksDto>> AddRelated(Guid id, AddRelatedRequest request, CancellationToken ct) =>
        Ok(await tickets.AddRelatedAsync(id, request, ct));

    /// <summary>Remove a related link between two tickets.</summary>
    [HttpDelete("{id:guid}/links/{targetId:guid}")]
    public async Task<IActionResult> RemoveRelated(Guid id, Guid targetId, CancellationToken ct)
    {
        await tickets.RemoveRelatedAsync(id, targetId, ct);
        return NoContent();
    }
}
