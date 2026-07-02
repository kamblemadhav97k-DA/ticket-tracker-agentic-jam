using Microsoft.AspNetCore.Identity;
using TicketTracker.Infrastructure.Identity;

namespace TicketTracker.Tests.Auth;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();
    private readonly ApplicationUser _user = new() { Id = Guid.NewGuid(), Email = "user@example.com" };

    [Fact]
    public void HashPassword_ProducesArgon2idHash_ThatIsNotPlainText()
    {
        var hash = _hasher.HashPassword(_user, "correct horse battery staple");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.DoesNotContain("correct horse battery staple", hash);
        Assert.StartsWith("$argon2id$", hash);
    }

    [Fact]
    public void VerifyHashedPassword_Succeeds_ForCorrectPassword()
    {
        var hash = _hasher.HashPassword(_user, "P@ssw0rd123");

        var result = _hasher.VerifyHashedPassword(_user, hash, "P@ssw0rd123");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyHashedPassword_Fails_ForWrongPassword()
    {
        var hash = _hasher.HashPassword(_user, "P@ssw0rd123");

        var result = _hasher.VerifyHashedPassword(_user, hash, "wrong-password");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }
}
