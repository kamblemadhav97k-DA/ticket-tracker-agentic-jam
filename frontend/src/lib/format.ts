import type { TicketState, TicketType } from '../types';

/** The five Kanban states in fixed workflow order. */
export const TICKET_STATES: TicketState[] = [
  'new',
  'ready_for_implementation',
  'in_progress',
  'ready_for_acceptance',
  'done',
];

export const TICKET_TYPES: TicketType[] = ['bug', 'feature', 'fix'];

const STATE_LABELS: Record<TicketState, string> = {
  new: 'New',
  ready_for_implementation: 'Ready for Implementation',
  in_progress: 'In Progress',
  ready_for_acceptance: 'Ready for Acceptance',
  done: 'Done',
};

/** Canonical API state -> human-readable label with spaces. */
export function stateLabel(state: TicketState): string {
  return STATE_LABELS[state] ?? state;
}

/** Ticket type -> display label (Bug / Feature / Fix). */
export function typeLabel(type: TicketType): string {
  return type.charAt(0).toUpperCase() + type.slice(1);
}

/** Compact relative time such as "2h ago", "3d ago", or a date for older items. */
export function relativeTime(iso: string, now: Date = new Date()): string {
  const then = new Date(iso);
  const diffMs = now.getTime() - then.getTime();
  const sec = Math.floor(diffMs / 1000);
  if (sec < 60) return 'just now';
  const min = Math.floor(sec / 60);
  if (min < 60) return `${min}m ago`;
  const hr = Math.floor(min / 60);
  if (hr < 24) return `${hr}h ago`;
  const day = Math.floor(hr / 24);
  if (day < 7) return `${day}d ago`;
  return then.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

/** Absolute UTC timestamp for detail views, e.g. "Jun 22, 09:15 UTC". */
export function utcTimestamp(iso: string): string {
  const d = new Date(iso);
  const date = d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', timeZone: 'UTC' });
  const time = d.toLocaleTimeString('en-GB', {
    hour: '2-digit', minute: '2-digit', timeZone: 'UTC',
  });
  return `${date}, ${time} UTC`;
}
