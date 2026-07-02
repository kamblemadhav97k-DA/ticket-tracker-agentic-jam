import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import './index.css';
import { AuthProvider } from './auth/AuthContext';
import { RequireAuth } from './auth/RequireAuth';
import { ToastProvider } from './components/ui/ToastProvider';
import { ConfirmProvider } from './components/ui/ConfirmProvider';
import { Layout } from './components/Layout';
import { LoginPage } from './pages/LoginPage';
import { SignupPage } from './pages/SignupPage';
import { VerifyEmailPage } from './pages/VerifyEmailPage';
import { BoardPage } from './pages/BoardPage';
import { TeamsPage } from './pages/TeamsPage';
import { EpicsPage } from './pages/EpicsPage';
import { TicketDetailsPage } from './pages/TicketDetailsPage';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
          <ConfirmProvider>
            <Routes>
              {/* Public */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/signup" element={<SignupPage />} />
              <Route path="/verify-email" element={<VerifyEmailPage />} />

              {/* Authenticated */}
              <Route element={<RequireAuth><Layout /></RequireAuth>}>
                <Route path="/board" element={<BoardPage />} />
                <Route path="/teams" element={<TeamsPage />} />
                <Route path="/epics" element={<EpicsPage />} />
                <Route path="/tickets/new" element={<TicketDetailsPage />} />
                <Route path="/tickets/:id" element={<TicketDetailsPage />} />
              </Route>

              <Route path="/" element={<Navigate to="/board" replace />} />
              <Route path="*" element={<Navigate to="/board" replace />} />
            </Routes>
          </ConfirmProvider>
        </ToastProvider>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
);
