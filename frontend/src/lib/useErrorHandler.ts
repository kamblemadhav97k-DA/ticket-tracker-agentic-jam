import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError, UnauthorizedError } from '../api/client';
import { useAuth } from '../auth/AuthContext';

/** Converts a thrown value into a user-facing message, logging out on 401. */
export function useErrorHandler(): (error: unknown) => string {
  const { logout } = useAuth();
  const navigate = useNavigate();

  return useCallback(
    (error: unknown): string => {
      if (error instanceof UnauthorizedError) {
        logout();
        navigate('/login', { replace: true });
        return error.message;
      }
      if (error instanceof ApiError) return error.message;
      if (error instanceof Error) return error.message;
      return 'Something went wrong. Please try again.';
    },
    [logout, navigate],
  );
}
