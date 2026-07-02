using System.ComponentModel.DataAnnotations;

namespace TicketTracker.API.Contracts.Auth;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record VerifyEmailRequest(
    [Required] string Token);

public record ResendVerificationRequest(
    [Required, EmailAddress] string Email);

public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string Email);
