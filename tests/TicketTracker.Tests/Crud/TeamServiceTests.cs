using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Common.Exceptions;
using TicketTracker.Application.Teams;
using TicketTracker.Domain.Entities;
using TicketTracker.Infrastructure.Persistence;
using TicketTracker.Infrastructure.Services;

namespace TicketTracker.Tests.Crud;

public class TeamServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"team-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task Create_TrimsName_AndPersists()
    {
        await using var db = NewContext();
        var service = new TeamService(db);

        var team = await service.CreateAsync(new CreateTeamRequest("  Payments Team  "));

        Assert.Equal("Payments Team", team.Name);
        Assert.Equal(team.CreatedAt, team.ModifiedAt);
    }

    [Fact]
    public async Task Create_RejectsEmptyName()
    {
        await using var db = NewContext();
        var service = new TeamService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreateTeamRequest("   ")));
    }

    [Fact]
    public async Task Create_RejectsCaseInsensitiveDuplicate()
    {
        await using var db = NewContext();
        var service = new TeamService(db);
        await service.CreateAsync(new CreateTeamRequest("Payments"));

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateTeamRequest("  payments ")));
    }

    [Fact]
    public async Task Delete_Conflicts_WhenTeamHasEpics()
    {
        await using var db = NewContext();
        var service = new TeamService(db);
        var team = await service.CreateAsync(new CreateTeamRequest("Payments"));
        db.Epics.Add(new Epic
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            Title = "Checkout",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(team.Id));
    }

    [Fact]
    public async Task Delete_Succeeds_WhenEmpty()
    {
        await using var db = NewContext();
        var service = new TeamService(db);
        var team = await service.CreateAsync(new CreateTeamRequest("Payments"));

        await service.DeleteAsync(team.Id);

        Assert.False(await db.Teams.AnyAsync());
    }
}
