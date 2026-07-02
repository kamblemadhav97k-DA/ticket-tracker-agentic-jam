using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TicketTracker.API.Contracts.Auth;
using TicketTracker.Application.Common.Interfaces;
using TicketTracker.Infrastructure.Identity;
using TicketTracker.Infrastructure.Options;

namespace TicketTracker.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    IEmailVerificationService emailVerification,
    IEmailSender emailSender,
    IJwtTokenService jwtTokenService,
    IOptions<AppOptions> appOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    private readonly AppOptions _appOptions = appOptions.Value;

    /// <summary>Sign up with email and password. Sends a verification email.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            EmailConfirmed = false,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await SendVerificationEmailAsync(user, ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Registration successful. Check your email to verify your account."
        });
    }

    /// <summary>Verify an email address using a single-use token.</summary>
    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
    {
        var userId = await emailVerification.ConsumeTokenAsync(request.Token, ct);
        if (userId is null)
        {
            return BadRequest(new { message = "Invalid or expired verification token." });
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return BadRequest(new { message = "Invalid or expired verification token." });
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        return Ok(new { message = "Email verified. You can now log in." });
    }

    /// <summary>Request a fresh verification email (invalidates earlier unused tokens).</summary>
    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && !user.EmailConfirmed)
        {
            await SendVerificationEmailAsync(user, ct);
        }

        // Do not reveal whether the account exists.
        return Ok(new
        {
            message = "If the account exists and is unverified, a new verification email has been sent."
        });
    }

    /// <summary>Log in with local credentials and receive a JWT bearer token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (!user.EmailConfirmed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Email not verified. Please verify your email before logging in."
            });
        }

        var token = jwtTokenService.GenerateToken(user.Id, user.Email!);
        return Ok(new LoginResponse(token.Token, token.ExpiresAtUtc, user.Email!));
    }

    /// <summary>Log out. Stateless JWT — the client discards the token.</summary>
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();

    private async Task SendVerificationEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        var token = await emailVerification.IssueTokenAsync(user.Id, ct);
        var link = $"{_appOptions.ClientUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";

        var html =
            "<p>Welcome to Ticket Tracker.</p>" +
            "<p>Please verify your email address by opening the link below (valid for 24 hours):</p>" +
            $"<p><a href=\"{link}\">{link}</a></p>";

        await emailSender.SendAsync(user.Email!, "Verify your Ticket Tracker email", html, ct);
        logger.LogInformation("Issued verification email for user {UserId}", user.Id);
    }
}
