import { Bug, Sparkles, Wrench } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import type { TicketType } from '../../types';
import { typeLabel } from '../../lib/format';

const TYPE_ICON: Record<TicketType, LucideIcon> = {
  bug: Bug,
  feature: Sparkles,
  fix: Wrench,
};

/** Colored ticket-type pill with an icon. */
export function TypeBadge({ type }: { type: TicketType }) {
  const Icon = TYPE_ICON[type];
  return (
    <span className="badge" data-type={type}>
      <Icon size={12} /> {typeLabel(type)}
    </span>
  );
}
