import type { ButtonHTMLAttributes, ReactNode } from 'react';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
  size?: 'md' | 'sm' | 'icon';
  loading?: boolean;
  block?: boolean;
  icon?: ReactNode;
}

export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  block = false,
  icon,
  className,
  children,
  disabled,
  ...rest
}: ButtonProps) {
  const classes = ['btn', `btn-${variant}`];
  if (size === 'sm') classes.push('btn-sm');
  if (size === 'icon') classes.push('btn-icon');
  if (block) classes.push('btn-block');
  if (className) classes.push(className);

  return (
    <button className={classes.join(' ')} disabled={disabled || loading} {...rest}>
      {loading ? <span className="btn-spinner" aria-hidden /> : icon}
      {children}
    </button>
  );
}
