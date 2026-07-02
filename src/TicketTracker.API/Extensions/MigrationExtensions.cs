using Microsoft.EntityFrameworkCore;
using TicketTracker.Infrastructure.Persistence;

namespace TicketTracker.API.Extensions;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations on startup, retrying while the database
    /// becomes available (e.g. under docker compose). Schema only — no seed data.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Non-relational providers (e.g. EF InMemory used in tests) do not support
        // migrations; create the schema directly instead.
        if (!db.Database.IsRelational())
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex,
                    "Database not ready (attempt {Attempt}/{Max}); retrying in 3s.",
                    attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}
