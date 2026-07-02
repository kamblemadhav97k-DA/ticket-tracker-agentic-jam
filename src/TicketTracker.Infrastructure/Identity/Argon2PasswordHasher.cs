using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Identity;

namespace TicketTracker.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity password hasher backed by Argon2id (via Isopoh). Replaces
/// the default PBKDF2 hasher so passwords are hashed with Argon2id as required.
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher<ApplicationUser>
{
    public string HashPassword(ApplicationUser user, string password)
        => Argon2.Hash(password);

    public PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user, string hashedPassword, string providedPassword)
        => Argon2.Verify(hashedPassword, providedPassword)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
}
