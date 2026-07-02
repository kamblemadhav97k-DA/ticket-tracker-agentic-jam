using System.ComponentModel.DataAnnotations;

namespace TicketTracker.Application.Comments;

public record CommentDto(
    Guid Id, Guid TicketId, Guid AuthorId, string? AuthorEmail, string Body, DateTime CreatedAt);

public record CreateCommentRequest([Required] string Body);

public interface ICommentService
{
    // Comments are returned chronologically, oldest first.
    Task<IReadOnlyList<CommentDto>> GetForTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task<CommentDto> AddAsync(Guid ticketId, CreateCommentRequest request, Guid currentUserId, CancellationToken ct = default);
}
