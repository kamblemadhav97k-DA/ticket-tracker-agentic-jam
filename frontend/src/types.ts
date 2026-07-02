export type TicketType = 'bug' | 'feature' | 'fix';

export type TicketState =
  | 'new'
  | 'ready_for_implementation'
  | 'in_progress'
  | 'ready_for_acceptance'
  | 'done';

export interface Team {
  id: string;
  name: string;
  ticketCount: number;
  epicCount: number;
  createdAt: string;
  modifiedAt: string;
}

export interface Epic {
  id: string;
  teamId: string;
  title: string;
  description: string | null;
  ticketCount: number;
  createdAt: string;
  modifiedAt: string;
}

export interface Ticket {
  id: string;
  teamId: string;
  epicId: string | null;
  type: TicketType;
  state: TicketState;
  title: string;
  body: string;
  createdById: string;
  createdByEmail: string | null;
  createdAt: string;
  modifiedAt: string;
  parentId: string | null;
  parentTitle: string | null;
  childCount: number;
  assignedToId: string | null;
  assignedToEmail: string | null;
}

/** A registered user that a work item can be assigned to. */
export interface UserSummary {
  id: string;
  email: string | null;
}

/** Compact view of a linked work item. */
export interface LinkedTicket {
  id: string;
  type: TicketType;
  state: TicketState;
  title: string;
}

/** A ticket's parent, children, and symmetric related links. */
export interface TicketLinks {
  parent: LinkedTicket | null;
  children: LinkedTicket[];
  related: LinkedTicket[];
}

export interface Comment {
  id: string;
  ticketId: string;
  authorId: string;
  authorEmail: string | null;
  body: string;
  createdAt: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  email: string;
}
