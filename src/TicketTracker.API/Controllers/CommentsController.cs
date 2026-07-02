using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketTracker.API.Extensions;
using TicketTracker.Application.Comments;

namespace TicketTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets/{ticketId:guid}/comments")]
public class CommentsController(ICommentService comments) : ControllerBase
{
    /// <summary>Lists a ticket's comments, oldest first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetForTicket(Guid ticketId, CancellationToken ct) =>
        Ok(await comments.GetForTicketAsync(ticketId, ct));

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Add(Guid ticketId, CreateCommentRequest request, CancellationToken ct)
    {
        var comment = await comments.AddAsync(ticketId, request, User.GetUserId(), ct);
        return CreatedAtAction(nameof(GetForTicket), new { ticketId }, comment);
    }
}
