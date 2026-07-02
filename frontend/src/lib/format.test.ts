import { describe, expect, it } from 'vitest';
import { TICKET_STATES, relativeTime, stateLabel, typeLabel } from './format';

describe('format helpers', () => {
  it('exposes the five states in workflow order', () => {
    expect(TICKET_STATES).toEqual([
      'new',
      'ready_for_implementation',
      'in_progress',
      'ready_for_acceptance',
      'done',
    ]);
  });

  it('renders human-readable state labels with spaces', () => {
    expect(stateLabel('ready_for_implementation')).toBe('Ready for Implementation');
    expect(stateLabel('in_progress')).toBe('In Progress');
    expect(stateLabel('done')).toBe('Done');
  });

  it('capitalises type labels', () => {
    expect(typeLabel('bug')).toBe('Bug');
    expect(typeLabel('feature')).toBe('Feature');
  });

  it('formats compact relative times', () => {
    const now = new Date('2026-07-01T12:00:00Z');
    expect(relativeTime('2026-07-01T10:00:00Z', now)).toBe('2h ago');
    expect(relativeTime('2026-06-29T12:00:00Z', now)).toBe('2d ago');
    expect(relativeTime('2026-07-01T11:59:30Z', now)).toBe('just now');
  });
});
