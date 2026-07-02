import { useEffect, useRef, useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { ChevronDown, Columns3, LayoutGrid, LogOut, Users } from 'lucide-react';
import { useAuth } from '../auth/AuthContext';

export function Header() {
  const { email, logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, []);

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  const initial = (email ?? '?').charAt(0).toUpperCase();

  return (
    <header className="app-header">
      <div className="brand">
        <span className="brand-mark"><Columns3 size={18} /></span>
        Ticket Tracker
      </div>
      <nav className="nav">
        <NavLink to="/board"><LayoutGrid size={16} /> Board</NavLink>
        <NavLink to="/teams"><Users size={16} /> Teams</NavLink>
        <NavLink to="/epics"><Columns3 size={16} /> Epics</NavLink>
      </nav>
      <div className="header-spacer" />
      <div className="user-menu" ref={menuRef}>
        <button className="user-menu-button" onClick={() => setOpen((o) => !o)} aria-haspopup="menu" aria-expanded={open}>
          <span className="avatar">{initial}</span>
          <span className="user-menu-email">{email ?? 'Account'}</span>
          <ChevronDown size={15} color="var(--muted)" />
        </button>
        {open && (
          <div className="user-menu-dropdown" role="menu">
            <div className="user-menu-header">
              <div className="label">Signed in as</div>
              <div className="value">{email}</div>
            </div>
            <button className="menu-item danger" role="menuitem" onClick={handleLogout}>
              <LogOut size={16} /> Log out
            </button>
          </div>
        )}
      </div>
    </header>
  );
}
