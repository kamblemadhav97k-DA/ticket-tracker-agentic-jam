import { api } from './client';
import type {
  Comment, Epic, LoginResponse, Team, Ticket, TicketLinks, TicketState, TicketType, UserSummary,
} from '../types';

// ---- Auth -------------------------------------------------------------------
export const authApi = {
  register: (email: string, password: string) =>
    api.post<{ message: string }>('/auth/register', { email, password }),
  login: (email: string, password: string) =>
    api.post<LoginResponse>('/auth/login', { email, password }, { auth: false }),
  verifyEmail: (token: string) =>
    api.post<{ message: string }>('/auth/verify-email', { token }, { auth: false }),
  resendVerification: (email: string) =>
    api.post<{ message: string }>('/auth/resend-verification', { email }, { auth: false }),
};

// ---- Teams ------------------------------------------------------------------
export const teamsApi = {
  list: () => api.get<Team[]>('/teams'),
  create: (name: string) => api.post<Team>('/teams', { name }),
  rename: (id: string, name: string) => api.put<Team>(`/teams/${id}`, { name }),
  remove: (id: string) => api.del<void>(`/teams/${id}`),
};

// ---- Epics ------------------------------------------------------------------
export const epicsApi = {
  list: (teamId?: string) => api.get<Epic[]>(`/epics${teamId ? `?teamId=${teamId}` : ''}`),
  create: (teamId: string, title: string, description: string | null) =>
    api.post<Epic>('/epics', { teamId, title, description }),
  update: (id: string, title: string, description: string | null) =>
    api.put<Epic>(`/epics/${id}`, { title, description }),
  remove: (id: string) => api.del<void>(`/epics/${id}`),
};

// ---- Tickets ----------------------------------------------------------------
export interface TicketInput {
  teamId: string;
  epicId: string | null;
  type: TicketType;
  state?: TicketState;
  title: string;
  body: string;
  assignedToId: string | null;
}

export const ticketsApi = {
  board: (params: { teamId: string; type?: string; epicId?: string; search?: string }) => {
    const q = new URLSearchParams({ teamId: params.teamId });
    if (params.type) q.set('type', params.type);
    if (params.epicId) q.set('epicId', params.epicId);
    if (params.search) q.set('search', params.search);
    return api.get<Ticket[]>(`/tickets?${q.toString()}`);
  },
  get: (id: string) => api.get<Ticket>(`/tickets/${id}`),
  create: (input: TicketInput) => api.post<Ticket>('/tickets', input),
  update: (id: string, input: TicketInput) => api.put<Ticket>(`/tickets/${id}`, input),
  updateState: (id: string, state: TicketState) =>
    api.patch<Ticket>(`/tickets/${id}/state`, { state }),
  remove: (id: string) => api.del<void>(`/tickets/${id}`),

  // Work-item links (Azure DevOps–style parent/child + related)
  links: (id: string) => api.get<TicketLinks>(`/tickets/${id}/links`),
  setParent: (id: string, parentId: string | null) =>
    api.put<Ticket>(`/tickets/${id}/parent`, { parentId }),
  addRelated: (id: string, targetId: string) =>
    api.post<TicketLinks>(`/tickets/${id}/links`, { targetId }),
  removeRelated: (id: string, targetId: string) =>
    api.del<void>(`/tickets/${id}/links/${targetId}`),
};

// ---- Users ------------------------------------------------------------------
export const usersApi = {
  list: () => api.get<UserSummary[]>('/users'),
};

// ---- Comments ---------------------------------------------------------------
export const commentsApi = {
  list: (ticketId: string) => api.get<Comment[]>(`/tickets/${ticketId}/comments`),
  add: (ticketId: string, body: string) =>
    api.post<Comment>(`/tickets/${ticketId}/comments`, { body }),
};
