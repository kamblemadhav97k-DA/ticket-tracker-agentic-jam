using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Common;
using TicketTracker.Application.Common.Exceptions;
using TicketTracker.Application.Tickets;
using TicketTracker.Domain.Entities;
using TicketTracker.Domain.Enums;
using TicketTracker.Infrastructure.Persistence;

namespace TicketTracker.Infrastructure.Services;

public class TicketService(ApplicationDbContext db) : ITicketService
{
    public async Task<IReadOnlyList<TicketDto>> GetBoardAsync(TicketFilter filter, CancellationToken ct = default)
    {
        var query = db.Tickets.Where(t => t.TeamId == filter.TeamId);

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            if (!TicketEnums.TryParseType(filter.Type, out var type))
            {
                throw new ValidationException($"Type must be one of: {TicketEnums.AllowedTypes}.");
            }
            query = query.Where(t => t.Type == type);
        }

        if (filter.EpicId is not null)
        {
            query = query.Where(t => t.EpicId == filter.EpicId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term));
        }

        // Within a column, most recently modified first.
        query = query.OrderByDescending(t => t.ModifiedAt);

        return await ProjectAsync(query, ct);
    }

    public async Task<TicketDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await ProjectAsync(db.Tickets.Where(t => t.Id == id), ct)).FirstOrDefault();
        return dto ?? throw NotFound(id);
    }

    public async Task<TicketDto> CreateAsync(CreateTicketRequest request, Guid currentUserId, CancellationToken ct = default)
    {
        await EnsureTeamExistsAsync(request.TeamId, ct);
        var type = ParseType(request.Type);
        var state = request.State is null ? TicketState.New : ParseState(request.State);
        var title = RequireNonEmpty(request.Title, "Ticket title");
        var body = RequireNonEmpty(request.Body, "Ticket body");
        await ValidateEpicAsync(request.EpicId, request.TeamId, ct);
        await ValidateAssigneeAsync(request.AssignedToId, ct);

        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TeamId = request.TeamId,
            EpicId = request.EpicId,
            Type = type,
            State = state,
            Title = title,
            Body = body,
            CreatedById = currentUserId,
            AssignedToId = request.AssignedToId,
            CreatedAt = now,
            ModifiedAt = now,
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(ticket.Id, ct);
    }

    public async Task<TicketDto> UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct) ?? throw NotFound(id);

        await EnsureTeamExistsAsync(request.TeamId, ct);
        var type = ParseType(request.Type);
        var state = ParseState(request.State);
        var title = RequireNonEmpty(request.Title, "Ticket title");
        var body = RequireNonEmpty(request.Body, "Ticket body");
        // Epic must belong to the (possibly new) team.
        await ValidateEpicAsync(request.EpicId, request.TeamId, ct);
        await ValidateAssigneeAsync(request.AssignedToId, ct);

        var changed =
            ticket.TeamId != request.TeamId ||
            ticket.EpicId != request.EpicId ||
            ticket.Type != type ||
            ticket.State != state ||
            ticket.AssignedToId != request.AssignedToId ||
            !string.Equals(ticket.Title, title, StringComparison.Ordinal) ||
            !string.Equals(ticket.Body, body, StringComparison.Ordinal);

        if (changed)
        {
            ticket.TeamId = request.TeamId;
            ticket.EpicId = request.EpicId;
            ticket.Type = type;
            ticket.State = state;
            ticket.Title = title;
            ticket.Body = body;
            ticket.AssignedToId = request.AssignedToId;
            // Modified timestamp advances only on an actual field/state change.
            ticket.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(ticket.Id, ct);
    }

    public async Task<TicketDto> UpdateStateAsync(Guid id, UpdateTicketStateRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct) ?? throw NotFound(id);
        var state = ParseState(request.State);

        if (ticket.State != state)
        {
            ticket.State = state;
            ticket.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(ticket.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct) ?? throw NotFound(id);

        // Orphan any children (clear their parent) so the Restrict FK does not block delete.
        var children = await db.Tickets.Where(t => t.ParentId == id).ToListAsync(ct);
        foreach (var child in children)
        {
            child.ParentId = null;
        }

        // Remove related links pointing either way at this ticket.
        var links = await db.TicketLinks
            .Where(l => l.SourceTicketId == id || l.TargetTicketId == id)
            .ToListAsync(ct);
        db.TicketLinks.RemoveRange(links);

        // Deleting a ticket also deletes its comments (explicit for provider-independence;
        // the relational schema also cascades).
        var comments = await db.Comments.Where(c => c.TicketId == id).ToListAsync(ct);
        db.Comments.RemoveRange(comments);
        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync(ct);
    }

    // ---- Work-item links --------------------------------------------------

    public async Task<TicketLinksDto> GetLinksAsync(Guid id, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct) ?? throw NotFound(id);

        LinkedTicketDto? parent = null;
        if (ticket.ParentId is not null)
        {
            var p = await db.Tickets.FindAsync([ticket.ParentId.Value], ct);
            if (p is not null)
            {
                parent = MapCompact(p);
            }
        }

        var children = await db.Tickets
            .Where(t => t.ParentId == id)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        var relatedIds = await db.TicketLinks
            .Where(l => l.SourceTicketId == id || l.TargetTicketId == id)
            .Select(l => l.SourceTicketId == id ? l.TargetTicketId : l.SourceTicketId)
            .ToListAsync(ct);

        var related = await db.Tickets
            .Where(t => relatedIds.Contains(t.Id))
            .OrderBy(t => t.Title)
            .ToListAsync(ct);

        return new TicketLinksDto(
            parent,
            children.Select(MapCompact).ToList(),
            related.Select(MapCompact).ToList());
    }

    public async Task<TicketDto> SetParentAsync(Guid id, SetParentRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct) ?? throw NotFound(id);

        if (request.ParentId is null)
        {
            if (ticket.ParentId is not null)
            {
                ticket.ParentId = null;
                ticket.ModifiedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            return await GetByIdAsync(id, ct);
        }

        var parentId = request.ParentId.Value;
        if (parentId == id)
        {
            throw new ValidationException("A work item cannot be its own parent.");
        }

        var parent = await db.Tickets.FindAsync([parentId], ct)
            ?? throw new ValidationException($"Parent work item '{parentId}' does not exist.");

        if (parent.TeamId != ticket.TeamId)
        {
            throw new ValidationException("The parent work item must belong to the same team.");
        }

        // Walk the proposed parent's ancestry; reaching this ticket means a cycle.
        var cursor = parent.ParentId;
        while (cursor is not null)
        {
            if (cursor == id)
            {
                throw new ConflictException("That link would create a circular parent/child relationship.");
            }
            cursor = await db.Tickets.Where(t => t.Id == cursor).Select(t => t.ParentId).FirstOrDefaultAsync(ct);
        }

        if (ticket.ParentId != parentId)
        {
            ticket.ParentId = parentId;
            ticket.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task<TicketLinksDto> AddRelatedAsync(Guid id, AddRelatedRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct) ?? throw NotFound(id);
        var targetId = request.TargetId;

        if (targetId == id)
        {
            throw new ValidationException("A work item cannot be linked to itself.");
        }

        var target = await db.Tickets.FindAsync([targetId], ct)
            ?? throw new ValidationException($"Work item '{targetId}' does not exist.");

        if (target.TeamId != ticket.TeamId)
        {
            throw new ValidationException("Related work items must belong to the same team.");
        }

        var exists = await db.TicketLinks.AnyAsync(
            l => (l.SourceTicketId == id && l.TargetTicketId == targetId) ||
                 (l.SourceTicketId == targetId && l.TargetTicketId == id), ct);
        if (exists)
        {
            throw new ConflictException("These work items are already linked.");
        }

        db.TicketLinks.Add(new TicketLink
        {
            Id = Guid.NewGuid(),
            SourceTicketId = id,
            TargetTicketId = targetId,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        return await GetLinksAsync(id, ct);
    }

    public async Task RemoveRelatedAsync(Guid id, Guid targetId, CancellationToken ct = default)
    {
        var link = await db.TicketLinks.FirstOrDefaultAsync(
            l => (l.SourceTicketId == id && l.TargetTicketId == targetId) ||
                 (l.SourceTicketId == targetId && l.TargetTicketId == id), ct)
            ?? throw new NotFoundException("The link between these work items was not found.");

        db.TicketLinks.Remove(link);
        await db.SaveChangesAsync(ct);
    }

    // ---- helpers ----------------------------------------------------------

    private async Task EnsureTeamExistsAsync(Guid teamId, CancellationToken ct)
    {
        if (!await db.Teams.AnyAsync(t => t.Id == teamId, ct))
        {
            throw new ValidationException($"Team '{teamId}' does not exist.");
        }
    }

    private async Task ValidateEpicAsync(Guid? epicId, Guid teamId, CancellationToken ct)
    {
        if (epicId is null)
        {
            return;
        }

        var epic = await db.Epics.FindAsync([epicId.Value], ct)
            ?? throw new ValidationException($"Epic '{epicId}' does not exist.");

        if (epic.TeamId != teamId)
        {
            throw new ValidationException("The epic must belong to the same team as the ticket.");
        }
    }

    private async Task ValidateAssigneeAsync(Guid? assignedToId, CancellationToken ct)
    {
        if (assignedToId is null)
        {
            return;
        }

        if (!await db.Users.AnyAsync(u => u.Id == assignedToId.Value, ct))
        {
            throw new ValidationException("The assigned user does not exist.");
        }
    }

    private static TicketType ParseType(string? value)
    {
        if (!TicketEnums.TryParseType(value, out var type))
        {
            throw new ValidationException($"Type must be one of: {TicketEnums.AllowedTypes}.");
        }
        return type;
    }

    private static TicketState ParseState(string? value)
    {
        if (!TicketEnums.TryParseState(value, out var state))
        {
            throw new ValidationException($"State must be one of: {TicketEnums.AllowedStates}.");
        }
        return state;
    }

    private static string RequireNonEmpty(string? value, string field)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ValidationException($"{field} must not be empty.");
        }
        return trimmed;
    }

    private async Task<List<TicketDto>> ProjectAsync(IQueryable<Ticket> query, CancellationToken ct)
    {
        var rows = await (
            from t in query
            join u in db.Users on t.CreatedById equals u.Id into g
            from u in g.DefaultIfEmpty()
            select new
            {
                Ticket = t,
                Email = u != null ? u.Email : null,
                ParentTitle = t.ParentId != null
                    ? db.Tickets.Where(p => p.Id == t.ParentId).Select(p => p.Title).FirstOrDefault()
                    : null,
                ChildCount = db.Tickets.Count(c => c.ParentId == t.Id),
                AssignedToEmail = t.AssignedToId != null
                    ? db.Users.Where(a => a.Id == t.AssignedToId).Select(a => a.Email).FirstOrDefault()
                    : null,
            }).ToListAsync(ct);

        return rows.Select(r => Map(r.Ticket, r.Email, r.ParentTitle, r.ChildCount, r.AssignedToEmail)).ToList();
    }

    private static TicketDto Map(Ticket t, string? createdByEmail, string? parentTitle, int childCount, string? assignedToEmail) => new(
        t.Id, t.TeamId, t.EpicId,
        t.Type.ToApiValue(), t.State.ToApiValue(),
        t.Title, t.Body,
        t.CreatedById, createdByEmail,
        t.CreatedAt, t.ModifiedAt,
        t.ParentId, parentTitle, childCount,
        t.AssignedToId, assignedToEmail);

    private static LinkedTicketDto MapCompact(Ticket t) =>
        new(t.Id, t.Type.ToApiValue(), t.State.ToApiValue(), t.Title);

    private static NotFoundException NotFound(Guid id) => new($"Ticket '{id}' was not found.");
}
