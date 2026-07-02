using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Users;
using TicketTracker.Infrastructure.Persistence;

namespace TicketTracker.Infrastructure.Services;

/// <summary>Lists registered users for assignee pickers.</summary>
public class UserDirectory(ApplicationDbContext db) : IUserDirectory
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default) =>
        await db.Users
            .OrderBy(u => u.Email)
            .Select(u => new UserDto(u.Id, u.Email))
            .ToListAsync(ct);
}
