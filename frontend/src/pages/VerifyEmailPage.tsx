import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { CheckCircle2, Columns3, LogIn, Mail, XCircle } from 'lucide-react';
import { ApiError } from '../api/client';
import { authApi } from '../api/endpoints';
import { Button } from '../components/ui/Button';
import { FormField } from '../components/ui/FormField';
import { useToast } from '../components/ui/ToastProvider';

type Status = 'pending' | 'verifying' | 'success' | 'error';

export function VerifyEmailPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const toast = useToast();
  const token = params.get('token');

  const [status, setStatus] = useState<Status>(token ? 'verifying' : 'pending');
  const [message, setMessage] = useState<string | null>(null);
  const [resendEmail, setResendEmail] = useState('');
  const [resending, setResending] = useState(false);

  useEffect(() => {
    if (!token) return;
    let cancelled = false;
    authApi.verifyEmail(token)
      .then((res) => { if (!cancelled) { setStatus('success'); setMessage(res.message); } })
      .catch((err) => {
        if (!cancelled) {
          setStatus('error');
          setMessage(err instanceof ApiError ? err.message : 'Verification failed.');
        }
      });
    return () => { cancelled = true; };
  }, [token]);

  async function handleResend(e: React.FormEvent) {
    e.preventDefault();
    setResending(true);
    try {
      const res = await authApi.resendVerification(resendEmail.trim());
      toast.success(res.message, 'Verification email sent');
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Unable to resend.');
    } finally {
      setResending(false);
    }
  }

  return (
    <div className="auth-wrap">
      <div className="card auth-card center">
        <div className="auth-brand" style={{ justifyContent: 'center' }}>
          <span className="brand-mark"><Columns3 size={22} /></span>
          <span className="auth-brand-text">Ticket Tracker</span>
        </div>

        {status === 'verifying' && (
          <>
            <div className="verify-icon wait"><span className="spinner" /></div>
            <h1>Verifying your email…</h1>
            <p className="auth-lead">This will only take a moment.</p>
          </>
        )}

        {status === 'success' && (
          <>
            <div className="verify-icon ok"><CheckCircle2 size={44} /></div>
            <h1>Email verified</h1>
            <p className="auth-lead">{message ?? 'Your account is ready to use.'}</p>
            <Button block icon={<LogIn size={16} />} onClick={() => navigate('/login')}>
              Continue to login
            </Button>
          </>
        )}

        {(status === 'pending' || status === 'error') && (
          <>
            <div className={`verify-icon ${status === 'error' ? 'err' : 'wait'}`}>
              {status === 'error' ? <XCircle size={44} /> : <Mail size={40} />}
            </div>
            <h1>{status === 'error' ? 'Link expired or invalid' : 'Check your inbox'}</h1>
            <p className="auth-lead">
              {status === 'error'
                ? (message ?? 'This verification link is no longer valid.')
                : 'We sent a verification link to your email. Open it to activate your account.'}
            </p>

            <div className="auth-divider">Need a new link?</div>
            <form onSubmit={handleResend}>
              <FormField>
                <div className="input-icon-wrap">
                  <Mail size={16} className="input-icon" />
                  <input className="input" type="email" placeholder="name@example.com"
                    value={resendEmail} onChange={(e) => setResendEmail(e.target.value)} required />
                </div>
              </FormField>
              <Button type="submit" variant="secondary" block loading={resending}>
                Resend verification email
              </Button>
            </form>

            <p className="auth-foot">
              <Link className="link" to="/login">← Back to login</Link>
            </p>
          </>
        )}
      </div>
    </div>
  );
}
