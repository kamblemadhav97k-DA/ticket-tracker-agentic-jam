using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Common.Exceptions;
using TicketTracker.Application.Teams;
using TicketTracker.Domain.Entities;
using TicketTracker.Infrastructure.Persistence;

namespace TicketTracker.Infrastructure.Services;

public class TeamService(ApplicationDbContext db) : ITeamService
{
    public async Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamDto(
                t.Id,
                t.Name,
                db.Tickets.Count(x => x.TeamId == t.Id),
                db.Epics.Count(x => x.TeamId == t.Id),
                t.CreatedAt,
                t.ModifiedAt))
            .ToListAsync(ct);

    public async Task<TeamDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var team = await db.Teams.FindAsync([id], ct) ?? throw NotFound(id);
        return await ToDtoAsync(team, ct);
    }

    public async Task<TeamDto> CreateAsync(CreateTeamRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ValidationException("Team name must not be empty.");
        }

        if (await NameExistsAsync(name, excludeId: null, ct))
        {
            throw new ConflictException($"A team named '{name}' already exists.");
        }

        var now = DateTime.UtcNow;
        var team = new Team { Id = Guid.NewGuid(), Name = name, CreatedAt = now, ModifiedAt = now };
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);
        return new TeamDto(team.Id, team.Name, 0, 0, team.CreatedAt, team.ModifiedAt);
    }

    public async Task<TeamDto> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var team = await db.Teams.FindAsync([id], ct) ?? throw NotFound(id);

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ValidationException("Team name must not be empty.");
        }

        if (!string.Equals(name, team.Name, StringComparison.Ordinal))
        {
            if (await NameExistsAsync(name, excludeId: id, ct))
            {
                throw new ConflictException($"A team named '{name}' already exists.");
            }

            team.Name = name;
            team.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(team, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var team = await db.Teams.FindAsync([id], ct) ?? throw NotFound(id);

        // A team cannot be deleted while it contains epics or tickets (no cascade).
        if (await db.Epics.AnyAsync(e => e.TeamId == id, ct) ||
            await db.Tickets.AnyAsync(t => t.TeamId == id, ct))
        {
            throw new ConflictException("The team cannot be deleted while it contains epics or tickets.");
        }

        db.Teams.Remove(team);
        await db.SaveChangesAsync(ct);
    }

    private Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var lower = name.ToLower();
        return db.Teams.AnyAsync(
            t => t.Name.ToLower() == lower && (excludeId == null || t.Id != excludeId), ct);
    }

    private async Task<TeamDto> ToDtoAsync(Team t, CancellationToken ct) => new(
        t.Id,
        t.Name,
        await db.Tickets.CountAsync(x => x.TeamId == t.Id, ct),
        await db.Epics.CountAsync(x => x.TeamId == t.Id, ct),
        t.CreatedAt,
        t.ModifiedAt);

    private static NotFoundException NotFound(Guid id) => new($"Team '{id}' was not found.");
}
