namespace TicketTracker.Domain.Enums;

/// <summary>
/// Ticket classification label. Canonical API values are <c>bug | feature | fix</c>.
/// These are labels only; no workflow behaviour differs between them.
/// </summary>
public enum TicketType
{
    Bug,
    Feature,
    Fix
}
