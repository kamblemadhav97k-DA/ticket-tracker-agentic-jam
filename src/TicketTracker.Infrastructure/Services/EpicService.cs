using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Common.Exceptions;
using TicketTracker.Application.Epics;
using TicketTracker.Domain.Entities;
using TicketTracker.Infrastructure.Persistence;

namespace TicketTracker.Infrastructure.Services;

public class EpicService(ApplicationDbContext db) : IEpicService
{
    public async Task<IReadOnlyList<EpicDto>> GetAllAsync(Guid? teamId, CancellationToken ct = default)
    {
        var query = db.Epics.AsQueryable();
        if (teamId is not null)
        {
            query = query.Where(e => e.TeamId == teamId);
        }

        return await query
            .OrderBy(e => e.Title)
            .Select(e => new EpicDto(
                e.Id, e.TeamId, e.Title, e.Description,
                db.Tickets.Count(t => t.EpicId == e.Id),
                e.CreatedAt, e.ModifiedAt))
            .ToListAsync(ct);
    }

    public async Task<EpicDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var epic = await db.Epics.FindAsync([id], ct) ?? throw NotFound(id);
        return await ToDtoAsync(epic, ct);
    }

    public async Task<EpicDto> CreateAsync(CreateEpicRequest request, CancellationToken ct = default)
    {
        if (!await db.Teams.AnyAsync(t => t.Id == request.TeamId, ct))
        {
            throw new ValidationException($"Team '{request.TeamId}' does not exist.");
        }

        var title = (request.Title ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            throw new ValidationException("Epic title must not be empty.");
        }

        var now = DateTime.UtcNow;
        var epic = new Epic
        {
            Id = Guid.NewGuid(),
            TeamId = request.TeamId,
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = now,
            ModifiedAt = now,
        };
        db.Epics.Add(epic);
        await db.SaveChangesAsync(ct);
        return new EpicDto(epic.Id, epic.TeamId, epic.Title, epic.Description, 0, epic.CreatedAt, epic.ModifiedAt);
    }

    public async Task<EpicDto> UpdateAsync(Guid id, UpdateEpicRequest request, CancellationToken ct = default)
    {
        var epic = await db.Epics.FindAsync([id], ct) ?? throw NotFound(id);

        var title = (request.Title ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            throw new ValidationException("Epic title must not be empty.");
        }

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (!string.Equals(title, epic.Title, StringComparison.Ordinal) ||
            !string.Equals(description, epic.Description, StringComparison.Ordinal))
        {
            epic.Title = title;
            epic.Description = description;
            epic.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(epic, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var epic = await db.Epics.FindAsync([id], ct) ?? throw NotFound(id);

        // An epic cannot be deleted while tickets reference it.
        if (await db.Tickets.AnyAsync(t => t.EpicId == id, ct))
        {
            throw new ConflictException("The epic cannot be deleted while tickets reference it.");
        }

        db.Epics.Remove(epic);
        await db.SaveChangesAsync(ct);
    }

    private async Task<EpicDto> ToDtoAsync(Epic e, CancellationToken ct) => new(
        e.Id, e.TeamId, e.Title, e.Description,
        await db.Tickets.CountAsync(t => t.EpicId == e.Id, ct),
        e.CreatedAt, e.ModifiedAt);

    private static NotFoundException NotFound(Guid id) => new($"Epic '{id}' was not found.");
}
