namespace TicketTracker.Domain.Enums;

/// <summary>
/// Fixed Kanban workflow state. Canonical API values (in workflow order) are
/// <c>new | ready_for_implementation | in_progress | ready_for_acceptance | done</c>.
/// The workflow is fixed; no custom states are supported.
/// </summary>
public enum TicketState
{
    New,
    ReadyForImplementation,
    InProgress,
    ReadyForAcceptance,
    Done
}
