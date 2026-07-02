using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Common.Exceptions;
using TicketTracker.Application.Tickets;
using TicketTracker.Domain.Entities;
using TicketTracker.Infrastructure.Persistence;
using TicketTracker.Infrastructure.Services;

namespace TicketTracker.Tests.Crud;

public class TicketServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ticket-{Guid.NewGuid()}")
            .Options);

    private static async Task<(Guid teamId, Guid otherTeamId)> SeedTeamsAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var team = new Team { Id = Guid.NewGuid(), Name = "Payments", CreatedAt = now, ModifiedAt = now };
        var other = new Team { Id = Guid.NewGuid(), Name = "Platform", CreatedAt = now, ModifiedAt = now };
        db.Teams.AddRange(team, other);
        await db.SaveChangesAsync();
        return (team.Id, other.Id);
    }

    [Fact]
    public async Task Create_RejectsInvalidType()
    {
        await using var db = NewContext();
        var (teamId, _) = await SeedTeamsAsync(db);
        var service = new TicketService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CreateTicketRequest(teamId, null, "epic", null, "Title", "Body"), Guid.NewGuid()));
    }

    [Fact]
    public async Task Create_RejectsEpicFromDifferentTeam()
    {
        await using var db = NewContext();
        var (teamId, otherTeamId) = await SeedTeamsAsync(db);
        var epic = new Epic
        {
            Id = Guid.NewGuid(),
            TeamId = otherTeamId,
            Title = "Other",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        };
        db.Epics.Add(epic);
        await db.SaveChangesAsync();
        var service = new TicketService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CreateTicketRequest(teamId, epic.Id, "bug", null, "Title", "Body"), Guid.NewGuid()));
    }

    [Fact]
    public async Task Create_DefaultsStateToNew_AndCanonicalisesEnums()
    {
        await using var db = NewContext();
        var (teamId, _) = await SeedTeamsAsync(db);
        var service = new TicketService(db);

        var ticket = await service.CreateAsync(
            new CreateTicketRequest(teamId, null, "FEATURE", null, "  Title  ", "  Body  "), Guid.NewGuid());

        Assert.Equal("feature", ticket.Type);
        Assert.Equal("new", ticket.State);
        Assert.Equal("Title", ticket.Title);
    }

    [Fact]
    public async Task Update_DoesNotAdvanceModifiedAt_WhenNothingChanges()
    {
        await using var db = NewContext();
        var (teamId, _) = await SeedTeamsAsync(db);
        var service = new TicketService(db);
        var created = await service.CreateAsync(
            new CreateTicketRequest(teamId, null, "bug", "new", "Title", "Body"), Guid.NewGuid());

        var afterNoop = await service.UpdateAsync(created.Id,
            new UpdateTicketRequest(teamId, null, "bug", "new", "Title", "Body"));

        Assert.Equal(created.ModifiedAt, afterNoop.ModifiedAt);
    }

    [Fact]
    public async Task UpdateState_AdvancesModifiedAt_OnlyWhenStateChanges()
    {
        await using var db = NewContext();
        var (teamId, _) = await SeedTeamsAsync(db);
        var service = new TicketService(db);
        var created = await service.CreateAsync(
            new CreateTicketRequest(teamId, null, "bug", "new", "Title", "Body"), Guid.NewGuid());

        var sameState = await service.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("new"));
        Assert.Equal(created.ModifiedAt, sameState.ModifiedAt);

        var moved = await service.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("in_progress"));
        Assert.Equal("in_progress", moved.State);
        Assert.True(moved.ModifiedAt >= created.ModifiedAt);
    }

    [Fact]
    public async Task Board_FiltersByType_AndSearchesTitleCaseInsensitively()
    {
        await using var db = NewContext();
        var (teamId, _) = await SeedTeamsAsync(db);
        var service = new TicketService(db);
        await service.CreateAsync(new CreateTicketRequest(teamId, null, "bug", null, "Payment fails", "b"), Guid.NewGuid());
        await service.CreateAsync(new CreateTicketRequest(teamId, null, "feature", null, "Add retry", "b"), Guid.NewGuid());

        var bugs = await service.GetBoardAsync(new TicketFilter(teamId, "bug", null, null));
        Assert.Single(bugs);

        var search = await service.GetBoardAsync(new TicketFilter(teamId, null, null, "PAYMENT"));
        Assert.Single(search);
        Assert.Equal("Payment fails", search[0].Title);
    }
}
