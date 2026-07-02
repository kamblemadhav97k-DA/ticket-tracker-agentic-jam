namespace TicketTracker.Application.Common.Models;

/// <summary>A signed JWT access token and its UTC expiry.</summary>
public record AccessToken(string Token, DateTime ExpiresAtUtc);
