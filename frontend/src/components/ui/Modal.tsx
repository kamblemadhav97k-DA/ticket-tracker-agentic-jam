import { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import type { ReactNode } from 'react';

interface ModalProps {
  open: boolean;
  onClose: () => void;
  title?: ReactNode;
  subtitle?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
  size?: 'sm' | 'md';
}

export function Modal({ open, onClose, title, subtitle, children, footer, size = 'md' }: ModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);

  // Keep the latest onClose in a ref so the effects below don't depend on its
  // identity. Passing an inline `onClose={() => ...}` (as every caller does)
  // creates a new function each render; if the effect depended on it, it would
  // re-run on every keystroke and steal focus back to the dialog.
  const onCloseRef = useRef(onClose);
  onCloseRef.current = onClose;

  // Escape-to-close: bound once per open, never re-run mid-edit.
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onCloseRef.current(); };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open]);

  // Move focus to the dialog only when it first opens — not on every render.
  useEffect(() => {
    if (open) modalRef.current?.focus();
  }, [open]);

  if (!open) return null;

  return createPortal(
    <div
      className="modal-overlay"
      onMouseDown={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div
        ref={modalRef}
        className={`modal modal-${size}`}
        role="dialog"
        aria-modal="true"
        tabIndex={-1}
      >
        {(title || subtitle) && (
          <div className="modal-header">
            <div>
              {title && <h2>{title}</h2>}
              {subtitle && <p className="modal-sub">{subtitle}</p>}
            </div>
            <button className="modal-close" onClick={onClose} aria-label="Close dialog">
              <X size={18} />
            </button>
          </div>
        )}
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-footer">{footer}</div>}
      </div>
    </div>,
    document.body,
  );
}
