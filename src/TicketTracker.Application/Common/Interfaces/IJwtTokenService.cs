using TicketTracker.Application.Common.Models;

namespace TicketTracker.Application.Common.Interfaces;

/// <summary>Issues signed JWT bearer tokens for authenticated users.</summary>
public interface IJwtTokenService
{
    AccessToken GenerateToken(Guid userId, string email);
}
