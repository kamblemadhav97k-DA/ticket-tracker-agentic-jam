import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  DndContext, PointerSensor, useDraggable, useDroppable, useSensor, useSensors,
} from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import { Inbox, Layers, Plus, Search, X } from 'lucide-react';
import { epicsApi, teamsApi, ticketsApi, usersApi } from '../api/endpoints';
import type { Epic, Team, Ticket, TicketState, UserSummary } from '../types';
import { TICKET_STATES, TICKET_TYPES, relativeTime, stateLabel, typeLabel } from '../lib/format';
import { useErrorHandler } from '../lib/useErrorHandler';
import { Button } from '../components/ui/Button';
import { TypeBadge } from '../components/ui/Badge';
import { EmptyState, Skeleton } from '../components/ui/Feedback';
import { useToast } from '../components/ui/ToastProvider';

/** Sentinel value for the "Unassigned" option in the assignee filter. */
const UNASSIGNED = '__unassigned__';

export function BoardPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const handleError = useErrorHandler();
  const toast = useToast();

  const [teams, setTeams] = useState<Team[]>([]);
  // Initial team comes from the ?teamId= param so navigating back to the board
  // (e.g. after deleting a ticket) stays on the same team instead of the first one.
  const [teamId, setTeamId] = useState(searchParams.get('teamId') ?? '');
  const [epics, setEpics] = useState<Epic[]>([]);
  const [tickets, setTickets] = useState<Ticket[] | null>(null);
  const [users, setUsers] = useState<UserSummary[]>([]);

  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [epicFilter, setEpicFilter] = useState('');
  const [assigneeFilter, setAssigneeFilter] = useState('');

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 6 } }));

  useEffect(() => {
    (async () => {
      try {
        const [list, userList] = await Promise.all([teamsApi.list(), usersApi.list()]);
        setTeams(list);
        setUsers(userList);
        if (list.length) {
          // Keep the team from the URL param if it still exists; otherwise first team.
          setTeamId((prev) => (list.some((t) => t.id === prev) ? prev : list[0].id));
        } else {
          setTickets([]);
        }
      } catch (e) { toast.error(handleError(e)); }
    })();
  }, []);

  async function loadBoard(id: string) {
    if (!id) { setTickets([]); setEpics([]); return; }
    try {
      const [t, e] = await Promise.all([ticketsApi.board({ teamId: id }), epicsApi.list(id)]);
      setTickets(t);
      setEpics(e);
    } catch (e) { toast.error(handleError(e)); }
  }

  useEffect(() => {
    if (!teamId) return;
    setTickets(null);
    setSearch(''); setTypeFilter(''); setEpicFilter(''); setAssigneeFilter('');
    void loadBoard(teamId);
  }, [teamId]);

  const epicMap = useMemo(() => new Map(epics.map((e) => [e.id, e.title])), [epics]);
  const hasFilters = Boolean(search || typeFilter || epicFilter || assigneeFilter);

  const filtered = useMemo(() => {
    if (!tickets) return [];
    const term = search.trim().toLowerCase();
    return tickets.filter((t) =>
      (!typeFilter || t.type === typeFilter) &&
      (!epicFilter || t.epicId === epicFilter) &&
      (!assigneeFilter
        || (assigneeFilter === UNASSIGNED ? !t.assignedToId : t.assignedToId === assigneeFilter)) &&
      (!term || t.title.toLowerCase().includes(term)));
  }, [tickets, search, typeFilter, epicFilter, assigneeFilter]);

  const byState = useMemo(() => {
    const groups: Record<TicketState, Ticket[]> = {
      new: [], ready_for_implementation: [], in_progress: [], ready_for_acceptance: [], done: [],
    };
    for (const t of filtered) groups[t.state].push(t);
    return groups;
  }, [filtered]);

  async function onDragEnd(event: DragEndEvent) {
    const ticketId = String(event.active.id);
    const target = event.over?.id as TicketState | undefined;
    if (!target || !tickets) return;
    const ticket = tickets.find((t) => t.id === ticketId);
    if (!ticket || ticket.state === target) return;

    const previous = ticket.state;
    setTickets((prev) => prev!.map((t) => (t.id === ticketId ? { ...t, state: target } : t)));
    try {
      const updated = await ticketsApi.updateState(ticketId, target);
      setTickets((prev) => prev!.map((t) => (t.id === ticketId ? updated : t)));
    } catch (e) {
      setTickets((prev) => prev!.map((t) => (t.id === ticketId ? { ...t, state: previous } : t)));
      toast.error(handleError(e), 'Could not move ticket');
    }
  }

  function selectTeam(id: string) {
    setTeamId(id);
    // Reflect the selected team in the URL so it survives navigating away and back.
    setSearchParams(id ? { teamId: id } : {}, { replace: true });
  }

  function clearFilters() { setSearch(''); setTypeFilter(''); setEpicFilter(''); setAssigneeFilter(''); }

  return (
    <>
      <div className="board-toolbar">
        <div className="field team-select" style={{ marginBottom: 0 }}>
          <label className="field-label" htmlFor="team">Team</label>
          <select id="team" className="select" value={teamId} onChange={(e) => selectTeam(e.target.value)}>
            {teams.length === 0 && <option value="">No teams yet</option>}
            {teams.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
        </div>
        <Button icon={<Plus size={16} />} disabled={!teamId} onClick={() => navigate('/tickets/new')}>
          New ticket
        </Button>
      </div>

      {teamId && (
        <div className="filter-bar">
          <div className="field grow" style={{ marginBottom: 0 }}>
            <label className="field-label" htmlFor="search">Search</label>
            <div className="input-icon-wrap">
              <Search size={16} className="input-icon" />
              <input id="search" className="input" placeholder="Search by title…"
                value={search} onChange={(e) => setSearch(e.target.value)} />
            </div>
          </div>
          <div className="field" style={{ marginBottom: 0, minWidth: 170 }}>
            <label className="field-label" htmlFor="type">Type</label>
            <select id="type" className="select" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
              <option value="">All types</option>
              {TICKET_TYPES.map((t) => <option key={t} value={t}>{typeLabel(t)}</option>)}
            </select>
          </div>
          <div className="field" style={{ marginBottom: 0, minWidth: 190 }}>
            <label className="field-label" htmlFor="epic">Epic</label>
            <select id="epic" className="select" value={epicFilter} onChange={(e) => setEpicFilter(e.target.value)}>
              <option value="">All epics</option>
              {epics.map((e) => <option key={e.id} value={e.id}>{e.title}</option>)}
            </select>
          </div>
          <div className="field" style={{ marginBottom: 0, minWidth: 200 }}>
            <label className="field-label" htmlFor="assignee">Assigned to</label>
            <select id="assignee" className="select" value={assigneeFilter} onChange={(e) => setAssigneeFilter(e.target.value)}>
              <option value="">All assignees</option>
              <option value={UNASSIGNED}>Unassigned</option>
              {users.map((u) => <option key={u.id} value={u.id}>{u.email ?? u.id}</option>)}
            </select>
          </div>
          <Button variant="secondary" icon={<X size={15} />} onClick={clearFilters} disabled={!hasFilters}>
            Clear
          </Button>
          <div className="filter-count">{filtered.length} {filtered.length === 1 ? 'ticket' : 'tickets'}</div>
        </div>
      )}

      {tickets === null ? (
        <BoardSkeleton />
      ) : !teamId ? (
        <div className="card">
          <EmptyState icon={<Layers size={26} />} title="No teams yet"
            description="Create a team to start tracking tickets on the board." />
        </div>
      ) : (
        <DndContext sensors={sensors} onDragEnd={onDragEnd}>
          <div className="board">
            {TICKET_STATES.map((state) => (
              <Column key={state} state={state} count={byState[state].length}>
                {byState[state].length === 0 ? (
                  <div className="column-empty"><Inbox size={20} /><span>No tickets</span></div>
                ) : (
                  byState[state].map((ticket) => (
                    <TicketCard key={ticket.id} ticket={ticket}
                      epicTitle={ticket.epicId ? epicMap.get(ticket.epicId) : undefined}
                      onOpen={() => navigate(`/tickets/${ticket.id}`)} />
                  ))
                )}
              </Column>
            ))}
          </div>
        </DndContext>
      )}
    </>
  );
}

function Column({ state, count, children }: { state: TicketState; count: number; children: React.ReactNode }) {
  const { setNodeRef, isOver } = useDroppable({ id: state });
  return (
    <div ref={setNodeRef} className={`column${isOver ? ' drop-over' : ''}`} data-state={state}>
      <div className="column-head">
        <span className="column-title">{stateLabel(state)}</span>
        <span className="count-pill">{count}</span>
      </div>
      <div className="column-body">{children}</div>
    </div>
  );
}

/** Short, Azure-style work-item id derived from the ticket guid. */
function shortId(id: string): string {
  return '#' + id.replace(/-/g, '').slice(0, 5).toUpperCase();
}

function TicketCard({ ticket, epicTitle, onOpen }: { ticket: Ticket; epicTitle?: string; onOpen: () => void }) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({ id: ticket.id });
  const assignee = ticket.assignedToEmail;
  const initial = assignee ? assignee.charAt(0).toUpperCase() : '?';
  return (
    <div ref={setNodeRef} {...attributes} {...listeners}
      className={`ticket-card${isDragging ? ' dragging' : ''}`} data-type={ticket.type}
      onClick={onOpen} role="button" tabIndex={0}
      onKeyDown={(e) => { if (e.key === 'Enter') onOpen(); }}>
      <div className="wi-top">
        <TypeBadge type={ticket.type} />
        <span className="wi-id">{shortId(ticket.id)}</span>
        <span className={`wi-assignee${assignee ? '' : ' wi-unassigned'}`}
          title={assignee ? `Assigned to ${assignee}` : 'Unassigned'}>{initial}</span>
      </div>
      <div className="tc-title">{ticket.title}</div>
      {epicTitle && <div className="tc-epic"><Layers size={12} /> {epicTitle}</div>}
      <div className="tc-meta"><span /><span>{relativeTime(ticket.modifiedAt)}</span></div>
    </div>
  );
}

function BoardSkeleton() {
  return (
    <div className="board">
      {TICKET_STATES.map((state) => (
        <div key={state} className="column" data-state={state}>
          <div className="column-head">
            <span className="column-title"><span className="column-dot" />{stateLabel(state)}</span>
            <span className="count-pill">–</span>
          </div>
          <div className="column-body">
            <Skeleton height={72} radius={12} />
            <Skeleton height={72} radius={12} />
          </div>
        </div>
      ))}
    </div>
  );
}
