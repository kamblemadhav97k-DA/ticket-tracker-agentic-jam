using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Comments;
using TicketTracker.Application.Common.Exceptions;
using TicketTracker.Domain.Entities;
using TicketTracker.Infrastructure.Persistence;

namespace TicketTracker.Infrastructure.Services;

public class CommentService(ApplicationDbContext db) : ICommentService
{
    public async Task<IReadOnlyList<CommentDto>> GetForTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        await EnsureTicketExistsAsync(ticketId, ct);

        var rows = await (
            from c in db.Comments.Where(c => c.TicketId == ticketId)
            join u in db.Users on c.AuthorId equals u.Id into g
            from u in g.DefaultIfEmpty()
            orderby c.CreatedAt // oldest first
            select new { Comment = c, Email = u != null ? u.Email : null }).ToListAsync(ct);

        return rows.Select(r => Map(r.Comment, r.Email)).ToList();
    }

    public async Task<CommentDto> AddAsync(
        Guid ticketId, CreateCommentRequest request, Guid currentUserId, CancellationToken ct = default)
    {
        await EnsureTicketExistsAsync(ticketId, ct);

        var body = (request.Body ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            throw new ValidationException("Comment body must not be empty.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = currentUserId,
            Body = body,
            CreatedAt = DateTime.UtcNow,
        };
        db.Comments.Add(comment);
        // Note: adding a comment must NOT advance the ticket's ModifiedAt.
        await db.SaveChangesAsync(ct);

        var email = await db.Users
            .Where(u => u.Id == currentUserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);

        return Map(comment, email);
    }

    private async Task EnsureTicketExistsAsync(Guid ticketId, CancellationToken ct)
    {
        if (!await db.Tickets.AnyAsync(t => t.Id == ticketId, ct))
        {
            throw new NotFoundException($"Ticket '{ticketId}' was not found.");
        }
    }

    private static CommentDto Map(Comment c, string? authorEmail) =>
        new(c.Id, c.TicketId, c.AuthorId, authorEmail, c.Body, c.CreatedAt);
}
