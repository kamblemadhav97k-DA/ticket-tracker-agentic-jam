using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Comments;
using TicketTracker.Application.Common.Exceptions;
using TicketTracker.Application.Tickets;
using TicketTracker.Domain.Entities;
using TicketTracker.Infrastructure.Persistence;
using TicketTracker.Infrastructure.Services;

namespace TicketTracker.Tests.Crud;

public class CommentServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"comment-{Guid.NewGuid()}")
            .Options);

    private static async Task<Guid> SeedTeamAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var team = new Team { Id = Guid.NewGuid(), Name = "Payments", CreatedAt = now, ModifiedAt = now };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team.Id;
    }

    [Fact]
    public async Task Add_DoesNotAdvanceTicketModifiedAt()
    {
        await using var db = NewContext();
        var teamId = await SeedTeamAsync(db);
        var ticketService = new TicketService(db);
        var ticket = await ticketService.CreateAsync(
            new CreateTicketRequest(teamId, null, "bug", "new", "Title", "Body"), Guid.NewGuid());

        var commentService = new CommentService(db);
        await commentService.AddAsync(ticket.Id, new CreateCommentRequest("First comment"), Guid.NewGuid());

        var reloaded = await ticketService.GetByIdAsync(ticket.Id);
        Assert.Equal(ticket.ModifiedAt, reloaded.ModifiedAt);
    }

    [Fact]
    public async Task Add_RejectsEmptyBody()
    {
        await using var db = NewContext();
        var teamId = await SeedTeamAsync(db);
        var ticket = await new TicketService(db).CreateAsync(
            new CreateTicketRequest(teamId, null, "bug", "new", "Title", "Body"), Guid.NewGuid());
        var commentService = new CommentService(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => commentService.AddAsync(ticket.Id, new CreateCommentRequest("   "), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetForTicket_ReturnsOldestFirst()
    {
        await using var db = NewContext();
        var teamId = await SeedTeamAsync(db);
        var ticket = await new TicketService(db).CreateAsync(
            new CreateTicketRequest(teamId, null, "bug", "new", "Title", "Body"), Guid.NewGuid());
        var commentService = new CommentService(db);

        var first = await commentService.AddAsync(ticket.Id, new CreateCommentRequest("first"), Guid.NewGuid());
        var second = await commentService.AddAsync(ticket.Id, new CreateCommentRequest("second"), Guid.NewGuid());

        var list = await commentService.GetForTicketAsync(ticket.Id);

        Assert.Equal(2, list.Count);
        Assert.Equal(first.Id, list[0].Id);
        Assert.Equal(second.Id, list[1].Id);
    }

    [Fact]
    public async Task Add_ThrowsNotFound_ForUnknownTicket()
    {
        await using var db = NewContext();
        var commentService = new CommentService(db);

        await Assert.ThrowsAsync<NotFoundException>(
            () => commentService.AddAsync(Guid.NewGuid(), new CreateCommentRequest("hi"), Guid.NewGuid()));
    }
}
