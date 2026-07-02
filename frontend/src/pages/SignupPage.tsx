import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AlertCircle, CheckCircle2, Columns3, Lock, Mail, UserPlus } from 'lucide-react';
import { ApiError } from '../api/client';
import { authApi } from '../api/endpoints';
import { Button } from '../components/ui/Button';
import { FormField } from '../components/ui/FormField';

export function SignupPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const passwordError = password.length > 0 && password.length < 8 ? 'Minimum 8 characters.' : null;
  const confirmError = confirm.length > 0 && confirm !== password ? 'Passwords do not match.' : null;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setNotice(null);

    if (password.length < 8) { setError('Password must contain at least 8 characters.'); return; }
    if (password !== confirm) { setError('Passwords do not match.'); return; }

    setBusy(true);
    try {
      const res = await authApi.register(email.trim(), password);
      setNotice(res.message);
      setTimeout(() => navigate('/verify-email'), 1200);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to create account.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-wrap">
      <form className="card auth-card" onSubmit={handleSubmit}>
        <div className="auth-brand">
          <span className="brand-mark"><Columns3 size={22} /></span>
          <span className="auth-brand-text">Ticket Tracker</span>
        </div>

        <h1>Create your account</h1>
        <p className="auth-lead">Email verification is required before you can sign in.</p>

        {error && <div className="alert alert-error"><AlertCircle size={16} /><span>{error}</span></div>}
        {notice && <div className="alert alert-success"><CheckCircle2 size={16} /><span>{notice}</span></div>}

        <FormField label="Email" htmlFor="email">
          <div className="input-icon-wrap">
            <Mail size={16} className="input-icon" />
            <input id="email" className="input" type="email" autoComplete="email"
              placeholder="name@example.com" value={email}
              onChange={(e) => setEmail(e.target.value)} required />
          </div>
        </FormField>

        <FormField label="Password" htmlFor="password" error={passwordError}
          hint={!passwordError ? 'At least 8 characters.' : undefined}>
          <div className="input-icon-wrap">
            <Lock size={16} className="input-icon" />
            <input id="password" className={`input${passwordError ? ' has-error' : ''}`} type="password"
              autoComplete="new-password" placeholder="Minimum 8 characters" value={password}
              onChange={(e) => setPassword(e.target.value)} required />
          </div>
        </FormField>

        <FormField label="Confirm password" htmlFor="confirm" error={confirmError}>
          <div className="input-icon-wrap">
            <Lock size={16} className="input-icon" />
            <input id="confirm" className={`input${confirmError ? ' has-error' : ''}`} type="password"
              autoComplete="new-password" placeholder="Re-enter your password" value={confirm}
              onChange={(e) => setConfirm(e.target.value)} required />
          </div>
        </FormField>

        <Button type="submit" block loading={busy} icon={<UserPlus size={16} />}>
          {busy ? 'Creating…' : 'Sign up'}
        </Button>

        <p className="auth-foot">
          Already registered? <Link className="link" to="/login">Log in →</Link>
        </p>
      </form>
    </div>
  );
}
