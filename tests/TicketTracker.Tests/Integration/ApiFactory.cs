using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TicketTracker.Application.Common.Interfaces;
using TicketTracker.Infrastructure.Persistence;

namespace TicketTracker.Tests.Integration;

/// <summary>
/// Boots the real API pipeline (routing, JWT auth, controllers, exception handler)
/// with the database swapped to EF InMemory and email delivery stubbed out.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"api-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development loads appsettings.Development.json (JWT secret + connection string).
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Replace the Npgsql DbContext with a shared in-memory database. In
            // EF Core 10 the provider is registered via IDbContextOptionsConfiguration,
            // so all of these descriptors must be removed before re-registering.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(ApplicationDbContext) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")).ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_dbName));

            // Do not attempt real SMTP delivery in tests.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
