import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { AlertCircle, AlertTriangle, CheckCircle2, Info, X } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';

type Variant = 'success' | 'error' | 'warning' | 'info';

interface ToastItem {
  id: number;
  variant: Variant;
  title: string;
  message?: string;
  leaving?: boolean;
}

interface ToastApi {
  success: (message: string, title?: string) => void;
  error: (message: string, title?: string) => void;
  warning: (message: string, title?: string) => void;
  info: (message: string, title?: string) => void;
}

const ICONS: Record<Variant, LucideIcon> = {
  success: CheckCircle2,
  error: AlertCircle,
  warning: AlertTriangle,
  info: Info,
};

const DEFAULT_TITLE: Record<Variant, string> = {
  success: 'Success',
  error: 'Something went wrong',
  warning: 'Warning',
  info: 'Notice',
};

const ToastContext = createContext<ToastApi | undefined>(undefined);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const idRef = useRef(0);

  const dismiss = useCallback((id: number) => {
    setToasts((ts) => ts.map((t) => (t.id === id ? { ...t, leaving: true } : t)));
    window.setTimeout(() => setToasts((ts) => ts.filter((t) => t.id !== id)), 180);
  }, []);

  const push = useCallback((variant: Variant, message: string, title?: string) => {
    const id = ++idRef.current;
    setToasts((ts) => [...ts, { id, variant, message, title: title ?? DEFAULT_TITLE[variant] }]);
    window.setTimeout(() => dismiss(id), 4200);
  }, [dismiss]);

  const api = useMemo<ToastApi>(() => ({
    success: (m, t) => push('success', m, t),
    error: (m, t) => push('error', m, t),
    warning: (m, t) => push('warning', m, t),
    info: (m, t) => push('info', m, t),
  }), [push]);

  return (
    <ToastContext.Provider value={api}>
      {children}
      {createPortal(
        <div className="toast-region" role="region" aria-live="polite">
          {toasts.map((t) => {
            const Icon = ICONS[t.variant];
            return (
              <div key={t.id} className={`toast${t.leaving ? ' leaving' : ''}`} data-variant={t.variant}>
                <span className="toast-icon"><Icon size={18} /></span>
                <div className="toast-body">
                  <div className="toast-title">{t.title}</div>
                  {t.message && <div className="toast-msg">{t.message}</div>}
                </div>
                <button className="toast-close" onClick={() => dismiss(t.id)} aria-label="Dismiss">
                  <X size={15} />
                </button>
              </div>
            );
          })}
        </div>,
        document.body,
      )}
    </ToastContext.Provider>
  );
}

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within a ToastProvider');
  return ctx;
}
