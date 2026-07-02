using Microsoft.Extensions.DependencyInjection;

namespace TicketTracker.Application;

/// <summary>
/// Composition root for the Application layer (use cases, validators, DTOs).
/// Empty for Milestone 1 — business handlers are registered here in later milestones.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Milestone 1: no application services yet.
        return services;
    }
}
