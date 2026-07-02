import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { getToken, setToken } from '../api/client';
import { authApi } from '../api/endpoints';

const EMAIL_KEY = 'tt.email';

interface AuthState {
  email: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem(EMAIL_KEY));
  const [token, setTokenState] = useState<string | null>(() => getToken());

  const login = useCallback(async (emailInput: string, password: string) => {
    const result = await authApi.login(emailInput, password);
    setToken(result.accessToken);
    localStorage.setItem(EMAIL_KEY, result.email);
    setTokenState(result.accessToken);
    setEmail(result.email);
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    localStorage.removeItem(EMAIL_KEY);
    setTokenState(null);
    setEmail(null);
  }, []);

  const value = useMemo<AuthState>(
    () => ({ email, isAuthenticated: Boolean(token), login, logout }),
    [email, token, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
