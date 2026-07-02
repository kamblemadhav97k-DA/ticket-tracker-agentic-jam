import type { ReactNode } from 'react';

/** Centered spinner with an optional label. */
export function LoadingBlock({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="loading-center">
      <span className="spinner" aria-hidden />
      <span>{label}</span>
    </div>
  );
}

/** Shimmer placeholder block. */
export function Skeleton({ height = 16, width = '100%', radius = 8, style }: {
  height?: number | string;
  width?: number | string;
  radius?: number;
  style?: React.CSSProperties;
}) {
  return <div className="skeleton" style={{ height, width, borderRadius: radius, ...style }} aria-hidden />;
}

interface EmptyStateProps {
  icon: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}

/** Professional empty state with icon, copy and an optional call to action. */
export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="empty-state">
      <div className="empty-icon">{icon}</div>
      <div className="empty-title">{title}</div>
      {description && <div className="empty-desc">{description}</div>}
      {action}
    </div>
  );
}
