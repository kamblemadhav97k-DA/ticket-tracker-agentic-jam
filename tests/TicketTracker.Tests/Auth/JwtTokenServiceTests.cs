using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketTracker.Infrastructure.Options;
using TicketTracker.Infrastructure.Services;

namespace TicketTracker.Tests.Auth;

public class JwtTokenServiceTests
{
    private const string Secret = "unit-test-secret-key-that-is-long-enough-32b!!";
    private readonly JwtTokenService _service;
    private readonly JwtOptions _options;

    public JwtTokenServiceTests()
    {
        _options = new JwtOptions
        {
            Issuer = "TicketTracker",
            Audience = "TicketTracker",
            Secret = Secret,
            ExpiryMinutes = 60,
        };
        _service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(_options));
    }

    [Fact]
    public void GenerateToken_ProducesTokenValidatableWithSamePparameters_AndCarriesClaims()
    {
        var userId = Guid.NewGuid();
        const string email = "user@example.com";

        var token = _service.GenerateToken(userId, email);

        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token.Token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ClockSkew = TimeSpan.Zero,
        }, out _);

        Assert.Equal(userId.ToString(), principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(email, principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value);
    }

    [Fact]
    public void GenerateToken_WithWrongKey_FailsValidation()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), "user@example.com");
        var handler = new JwtSecurityTokenHandler();

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(token.Token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("a-completely-different-secret-key-32-bytes!!")),
                ClockSkew = TimeSpan.Zero,
            }, out _));
    }
}
