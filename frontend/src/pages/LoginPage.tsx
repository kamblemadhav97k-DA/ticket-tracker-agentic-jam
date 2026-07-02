import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { AlertCircle, Columns3, Lock, LogIn, Mail } from 'lucide-react';
import { ApiError } from '../api/client';
import { authApi } from '../api/endpoints';
import { useAuth } from '../auth/AuthContext';
import { Button } from '../components/ui/Button';
import { FormField } from '../components/ui/FormField';
import { useToast } from '../components/ui/ToastProvider';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const toast = useToast();
  const from = (location.state as { from?: string } | null)?.from ?? '/board';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [resending, setResending] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to log in.');
    } finally {
      setBusy(false);
    }
  }

  async function handleResend() {
    setError(null);
    if (!email.trim()) {
      setError('Enter your email above, then resend the verification message.');
      return;
    }
    setResending(true);
    try {
      const res = await authApi.resendVerification(email.trim());
      toast.success(res.message, 'Verification email sent');
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Unable to resend verification email.');
    } finally {
      setResending(false);
    }
  }

  return (
    <div className="auth-wrap">
      <form className="card auth-card" onSubmit={handleSubmit}>
        <div className="auth-brand">
          <span className="brand-mark"><Columns3 size={22} /></span>
          <span className="auth-brand-text">Ticket Tracker</span>
        </div>

        <h1>Welcome back</h1>
        <p className="auth-lead">Log in to your verified account.</p>

        {error && <div className="alert alert-error"><AlertCircle size={16} /><span>{error}</span></div>}

        <FormField label="Email" htmlFor="email">
          <div className="input-icon-wrap">
            <Mail size={16} className="input-icon" />
            <input id="email" className="input" type="email" autoComplete="email"
              placeholder="name@example.com" value={email}
              onChange={(e) => setEmail(e.target.value)} required />
          </div>
        </FormField>

        <FormField label="Password" htmlFor="password">
          <div className="input-icon-wrap">
            <Lock size={16} className="input-icon" />
            <input id="password" className="input" type="password" autoComplete="current-password"
              placeholder="Your password" value={password}
              onChange={(e) => setPassword(e.target.value)} required />
          </div>
        </FormField>

        <Button type="submit" block loading={busy} icon={<LogIn size={16} />}>
          {busy ? 'Logging in…' : 'Log in'}
        </Button>

        <div className="auth-divider">Account not verified?</div>
        <Button type="button" variant="secondary" block loading={resending} onClick={handleResend}>
          Resend verification email
        </Button>

        <p className="auth-foot">
          Don't have an account? <Link className="link" to="/signup">Create one →</Link>
        </p>
      </form>
    </div>
  );
}
