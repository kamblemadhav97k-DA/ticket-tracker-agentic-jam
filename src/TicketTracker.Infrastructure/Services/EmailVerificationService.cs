using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TicketTracker.Application.Common.Interfaces;
using TicketTracker.Infrastructure.Identity;
using TicketTracker.Infrastructure.Persistence;

namespace TicketTracker.Infrastructure.Services;

public class EmailVerificationService(ApplicationDbContext db) : IEmailVerificationService
{
    public const int ExpiryHours = 24;

    public async Task<string> IssueTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Invalidate earlier unused tokens for this user.
        var outstanding = await db.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in outstanding)
        {
            token.ConsumedAt = now;
        }

        var raw = GenerateRawToken();
        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(raw),
            CreatedAt = now,
            ExpiresAt = now.AddHours(ExpiryHours),
        });

        await db.SaveChangesAsync(cancellationToken);
        return raw;
    }

    public async Task<Guid?> ConsumeTokenAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = Hash(rawToken);
        var token = await db.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || token.ConsumedAt is not null || token.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        token.ConsumedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return token.UserId;
    }

    private static string GenerateRawToken()
    {
        // 256 bits of entropy, URL-safe.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
