import { AlertCircle } from 'lucide-react';
import type { ReactNode } from 'react';

interface FormFieldProps {
  label?: ReactNode;
  htmlFor?: string;
  hint?: ReactNode;
  error?: string | null;
  children: ReactNode;
}

/** Label + hint + validation-error wrapper for a form control. */
export function FormField({ label, htmlFor, hint, error, children }: FormFieldProps) {
  return (
    <div className="field">
      {label && <label className="field-label" htmlFor={htmlFor}>{label}</label>}
      {children}
      {error ? (
        <div className="field-error"><AlertCircle size={13} /> {error}</div>
      ) : hint ? (
        <div className="field-hint">{hint}</div>
      ) : null}
    </div>
  );
}
