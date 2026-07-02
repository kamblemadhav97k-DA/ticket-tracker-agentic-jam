using Microsoft.EntityFrameworkCore;
using TicketTracker.Infrastructure.Persistence;
using TicketTracker.Infrastructure.Services;

namespace TicketTracker.Tests.Auth;

public class EmailVerificationServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"evs-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task ConsumeToken_ReturnsUserId_ForFreshToken_ThenRejectsReuse()
    {
        await using var db = NewContext();
        var service = new EmailVerificationService(db);
        var userId = Guid.NewGuid();

        var raw = await service.IssueTokenAsync(userId);

        var first = await service.ConsumeTokenAsync(raw);
        var second = await service.ConsumeTokenAsync(raw);

        Assert.Equal(userId, first);
        Assert.Null(second); // single-use
    }

    [Fact]
    public async Task IssuingNewToken_InvalidatesEarlierUnusedTokens()
    {
        await using var db = NewContext();
        var service = new EmailVerificationService(db);
        var userId = Guid.NewGuid();

        var firstRaw = await service.IssueTokenAsync(userId);
        var secondRaw = await service.IssueTokenAsync(userId);

        Assert.Null(await service.ConsumeTokenAsync(firstRaw)); // invalidated by reissue
        Assert.Equal(userId, await service.ConsumeTokenAsync(secondRaw));
    }

    [Fact]
    public async Task ConsumeToken_RejectsExpiredToken()
    {
        await using var db = NewContext();
        var service = new EmailVerificationService(db);
        var userId = Guid.NewGuid();

        var raw = await service.IssueTokenAsync(userId);

        // Force the stored token to be expired.
        var stored = await db.EmailVerificationTokens.SingleAsync();
        stored.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();

        Assert.Null(await service.ConsumeTokenAsync(raw));
    }

    [Fact]
    public async Task ConsumeToken_ReturnsNull_ForUnknownOrEmptyToken()
    {
        await using var db = NewContext();
        var service = new EmailVerificationService(db);

        Assert.Null(await service.ConsumeTokenAsync("does-not-exist"));
        Assert.Null(await service.ConsumeTokenAsync(""));
    }
}
