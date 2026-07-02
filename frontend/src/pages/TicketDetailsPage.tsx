import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  ArrowLeft, Clock, CornerDownRight, CornerLeftUp, Link2, MessageSquare,
  Plus, Save, Send, Trash2, User, X,
} from 'lucide-react';
import { commentsApi, epicsApi, teamsApi, ticketsApi, usersApi } from '../api/endpoints';
import type { Comment, Epic, LinkedTicket, Team, Ticket, TicketLinks, TicketState, TicketType, UserSummary } from '../types';
import { TICKET_STATES, TICKET_TYPES, stateLabel, typeLabel, utcTimestamp } from '../lib/format';
import { useErrorHandler } from '../lib/useErrorHandler';
import { Button } from '../components/ui/Button';
import { TypeBadge } from '../components/ui/Badge';
import { FormField } from '../components/ui/FormField';
import { LoadingBlock } from '../components/ui/Feedback';
import { useToast } from '../components/ui/ToastProvider';
import { useConfirm } from '../components/ui/ConfirmProvider';

export function TicketDetailsPage() {
  const { id } = useParams();
  const isNew = !id;
  const navigate = useNavigate();
  const handleError = useErrorHandler();
  const toast = useToast();
  const confirm = useConfirm();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [teams, setTeams] = useState<Team[]>([]);
  const [epics, setEpics] = useState<Epic[]>([]);
  const [users, setUsers] = useState<UserSummary[]>([]);

  const [teamId, setTeamId] = useState('');
  const [epicId, setEpicId] = useState('');
  const [assignedToId, setAssignedToId] = useState('');
  const [type, setType] = useState<TicketType>('bug');
  const [state, setState] = useState<TicketState>('new');
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');

  const [meta, setMeta] = useState<{ createdByEmail: string | null; createdAt: string; modifiedAt: string } | null>(null);
  const [comments, setComments] = useState<Comment[] | null>(null);
  const [newComment, setNewComment] = useState('');
  const [postingComment, setPostingComment] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const [teamList, userList] = await Promise.all([teamsApi.list(), usersApi.list()]);
        setTeams(teamList);
        setUsers(userList);
        if (isNew) {
          const first = teamList[0]?.id ?? '';
          setTeamId(first);
          if (first) setEpics(await epicsApi.list(first));
        } else {
          const t = await ticketsApi.get(id!);
          setTeamId(t.teamId);
          setEpicId(t.epicId ?? '');
          setAssignedToId(t.assignedToId ?? '');
          setType(t.type);
          setState(t.state);
          setTitle(t.title);
          setBody(t.body);
          setMeta({ createdByEmail: t.createdByEmail, createdAt: t.createdAt, modifiedAt: t.modifiedAt });
          setEpics(await epicsApi.list(t.teamId));
          setComments(await commentsApi.list(id!));
        }
      } catch (e) {
        toast.error(handleError(e));
      } finally {
        setLoading(false);
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  async function changeTeam(newTeamId: string) {
    setTeamId(newTeamId);
    setEpicId(''); // team changed -> clear the selected epic (backend enforces same-team)
    try {
      setEpics(newTeamId ? await epicsApi.list(newTeamId) : []);
    } catch (e) { toast.error(handleError(e)); }
  }

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    const input = {
      teamId, epicId: epicId || null, assignedToId: assignedToId || null,
      type, state, title: title.trim(), body: body.trim(),
    };
    try {
      if (isNew) {
        const created = await ticketsApi.create(input);
        toast.success('Ticket created.');
        navigate(`/tickets/${created.id}`, { replace: true });
      } else {
        const updated = await ticketsApi.update(id!, input);
        setMeta({ createdByEmail: updated.createdByEmail, createdAt: updated.createdAt, modifiedAt: updated.modifiedAt });
        setState(updated.state);
        toast.success('Ticket saved.');
      }
    } catch (e) {
      toast.error(handleError(e));
    } finally {
      setSaving(false);
    }
  }

  async function remove() {
    if (!id) return;
    const ok = await confirm({
      title: 'Delete ticket',
      message: 'Delete this ticket? Its comments will also be deleted.',
      confirmText: 'Delete ticket',
      danger: true,
    });
    if (!ok) return;
    try {
      await ticketsApi.remove(id);
      toast.success('Ticket deleted.');
      navigate(boardHref);
    } catch (e) {
      toast.error(handleError(e));
    }
  }

  async function postComment(e: React.FormEvent) {
    e.preventDefault();
    if (!id || !newComment.trim()) return;
    setPostingComment(true);
    try {
      const c = await commentsApi.add(id, newComment.trim());
      setComments((prev) => [...(prev ?? []), c]);
      setNewComment('');
    } catch (e) {
      toast.error(handleError(e));
    } finally {
      setPostingComment(false);
    }
  }

  if (loading) return <LoadingBlock label="Loading ticket…" />;

  const teamName = teams.find((t) => t.id === teamId)?.name ?? 'board';
  // Preserve the team when returning to the board so we land on the same board.
  const boardHref = teamId ? `/board?teamId=${teamId}` : '/board';

  return (
    <>
      <Link to={boardHref} className="back-link"><ArrowLeft size={16} /> Back to {teamName}</Link>

      <div className="detail-top">
        <div className="detail-meta">
          {isNew ? (
            <span>New ticket</span>
          ) : meta ? (
            <>
              <span>#{id!.slice(0, 8)}</span>
              <span className="dot-sep" />
              <span><User size={13} style={{ verticalAlign: '-2px' }} /> {meta.createdByEmail ?? 'unknown'}</span>
              <span className="dot-sep" />
              <span>Created {utcTimestamp(meta.createdAt)}</span>
              <span className="dot-sep" />
              <span><Clock size={13} style={{ verticalAlign: '-2px' }} /> Modified {utcTimestamp(meta.modifiedAt)}</span>
            </>
          ) : null}
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          {!isNew && <Button variant="danger" icon={<Trash2 size={15} />} onClick={remove}>Delete</Button>}
          <Button type="submit" form="ticket-form" loading={saving} icon={<Save size={16} />}>
            {isNew ? 'Create ticket' : 'Save changes'}
          </Button>
        </div>
      </div>

      <h1 className="detail-title">{title.trim() || (isNew ? 'New ticket' : 'Untitled ticket')}</h1>

      <div className="detail-grid">
        <form id="ticket-form" className="card card-pad" onSubmit={save}>
          <div className="row">
            <FormField label="Team" htmlFor="team">
              <select id="team" className="select" value={teamId} onChange={(e) => changeTeam(e.target.value)} required>
                {teams.length === 0 && <option value="">No teams</option>}
                {teams.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </FormField>
            <FormField label="Type" htmlFor="type">
              <select id="type" className="select" value={type} onChange={(e) => setType(e.target.value as TicketType)}>
                {TICKET_TYPES.map((t) => <option key={t} value={t}>{typeLabel(t)}</option>)}
              </select>
            </FormField>
            <FormField label="State" htmlFor="state">
              <select id="state" className="select" value={state} onChange={(e) => setState(e.target.value as TicketState)}>
                {TICKET_STATES.map((s) => <option key={s} value={s}>{stateLabel(s)}</option>)}
              </select>
            </FormField>
          </div>

          <FormField label="Epic" htmlFor="epic" hint="Optional — must belong to the selected team.">
            <select id="epic" className="select" value={epicId} onChange={(e) => setEpicId(e.target.value)}>
              <option value="">— No epic —</option>
              {epics.map((e) => <option key={e.id} value={e.id}>{e.title}</option>)}
            </select>
          </FormField>

          <FormField label="Assigned to" htmlFor="assignee" hint="Assign this work item to a teammate.">
            <select id="assignee" className="select" value={assignedToId} onChange={(e) => setAssignedToId(e.target.value)}>
              <option value="">— Unassigned —</option>
              {users.map((u) => <option key={u.id} value={u.id}>{u.email ?? u.id}</option>)}
            </select>
          </FormField>

          <FormField label="Title" htmlFor="title">
            <input id="title" className="input" value={title} onChange={(e) => setTitle(e.target.value)} required
              placeholder="Short summary of the ticket" />
          </FormField>

          <FormField label="Body" htmlFor="body">
            <textarea id="body" className="textarea" value={body} onChange={(e) => setBody(e.target.value)}
              style={{ minHeight: 200 }} required placeholder="Describe the work, steps to reproduce, acceptance criteria…" />
          </FormField>
        </form>

        <div className="stack">
          {!isNew && id && <LinksPanel ticketId={id} teamId={teamId} />}

          <div className="card card-pad">
          <div className="comments-head">
            <MessageSquare size={18} />
            <h2 style={{ fontSize: 17 }}>Comments</h2>
            <span className="count-pill">{comments?.length ?? 0}</span>
          </div>

          {isNew ? (
            <p className="empty-desc">Save the ticket to add comments.</p>
          ) : (
            <>
              {comments === null ? (
                <LoadingBlock label="Loading…" />
              ) : comments.length === 0 ? (
                <p className="empty-desc">No comments yet. Start the conversation below.</p>
              ) : (
                comments.map((c) => (
                  <div className="comment" key={c.id}>
                    <div className="comment-head">
                      <span className="avatar">{(c.authorEmail ?? '?').charAt(0).toUpperCase()}</span>
                      <span className="comment-author">{c.authorEmail ?? 'Unknown'}</span>
                      <span className="comment-time">{utcTimestamp(c.createdAt)}</span>
                    </div>
                    <div className="comment-body">{c.body}</div>
                  </div>
                ))
              )}

              <form onSubmit={postComment} className="mt">
                <FormField label="Add comment" htmlFor="new-comment">
                  <textarea id="new-comment" className="textarea" placeholder="Write a comment…"
                    value={newComment} onChange={(e) => setNewComment(e.target.value)} style={{ minHeight: 90 }} />
                </FormField>
                <div className="flex-end">
                  <Button type="submit" loading={postingComment} disabled={!newComment.trim()} icon={<Send size={15} />}>
                    Post comment
                  </Button>
                </div>
              </form>
            </>
          )}
          </div>
        </div>
      </div>
    </>
  );
}

/**
 * Azure DevOps–style linked-work-items panel: shows the parent, children and
 * related tickets, and lets the user add or remove links. Self-loads its data
 * and the pool of same-team candidates.
 */
function LinksPanel({ ticketId, teamId }: { ticketId: string; teamId: string }) {
  const handleError = useErrorHandler();
  const toast = useToast();
  const navigate = useNavigate();

  const [links, setLinks] = useState<TicketLinks | null>(null);
  const [candidates, setCandidates] = useState<Ticket[]>([]);
  const [adding, setAdding] = useState(false);
  const [mode, setMode] = useState<'parent' | 'child' | 'related'>('child');
  const [target, setTarget] = useState('');
  const [busy, setBusy] = useState(false);

  async function reload() {
    const [l, board] = await Promise.all([
      ticketsApi.links(ticketId),
      ticketsApi.board({ teamId }),
    ]);
    setLinks(l);
    setCandidates(board.filter((t) => t.id !== ticketId));
  }

  useEffect(() => {
    let active = true;
    setLinks(null);
    (async () => {
      try {
        const [l, board] = await Promise.all([
          ticketsApi.links(ticketId),
          ticketsApi.board({ teamId }),
        ]);
        if (!active) return;
        setLinks(l);
        setCandidates(board.filter((t) => t.id !== ticketId));
      } catch (e) {
        if (active) toast.error(handleError(e));
      }
    })();
    return () => { active = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ticketId, teamId]);

  async function addLink() {
    if (!target) return;
    setBusy(true);
    try {
      if (mode === 'parent') await ticketsApi.setParent(ticketId, target);
      else if (mode === 'child') await ticketsApi.setParent(target, ticketId);
      else await ticketsApi.addRelated(ticketId, target);
      setTarget('');
      setAdding(false);
      await reload();
      toast.success('Link added.');
    } catch (e) {
      toast.error(handleError(e));
    } finally {
      setBusy(false);
    }
  }

  async function unlink(fn: () => Promise<unknown>) {
    try {
      await fn();
      await reload();
    } catch (e) {
      toast.error(handleError(e));
    }
  }

  return (
    <div className="card card-pad">
      <div className="comments-head">
        <Link2 size={18} />
        <h2 style={{ fontSize: 17 }}>Linked work items</h2>
      </div>

      {links === null ? (
        <LoadingBlock label="Loading links…" />
      ) : (
        <>
          <LinkGroup icon={<CornerLeftUp size={13} />} title="Parent">
            {links.parent ? (
              <LinkRow item={links.parent} onOpen={() => navigate(`/tickets/${links.parent!.id}`)}
                onRemove={() => unlink(() => ticketsApi.setParent(ticketId, null))} />
            ) : (
              <p className="links-empty">No parent set.</p>
            )}
          </LinkGroup>

          <LinkGroup icon={<CornerDownRight size={13} />} title={`Children (${links.children.length})`}>
            {links.children.length === 0 ? (
              <p className="links-empty">No child work items.</p>
            ) : (
              links.children.map((c) => (
                <LinkRow key={c.id} item={c} onOpen={() => navigate(`/tickets/${c.id}`)}
                  onRemove={() => unlink(() => ticketsApi.setParent(c.id, null))} />
              ))
            )}
          </LinkGroup>

          <LinkGroup icon={<Link2 size={13} />} title={`Related (${links.related.length})`}>
            {links.related.length === 0 ? (
              <p className="links-empty">No related work items.</p>
            ) : (
              links.related.map((r) => (
                <LinkRow key={r.id} item={r} onOpen={() => navigate(`/tickets/${r.id}`)}
                  onRemove={() => unlink(() => ticketsApi.removeRelated(ticketId, r.id))} />
              ))
            )}
          </LinkGroup>

          {adding ? (
            <div className="link-add">
              <div className="link-add-row">
                <select className="select" value={mode} onChange={(e) => setMode(e.target.value as typeof mode)}>
                  <option value="child">Child</option>
                  <option value="parent">Parent</option>
                  <option value="related">Related</option>
                </select>
                <select className="select" value={target} onChange={(e) => setTarget(e.target.value)}>
                  <option value="">Select a work item…</option>
                  {candidates.map((t) => (
                    <option key={t.id} value={t.id}>{typeLabel(t.type)} · {t.title}</option>
                  ))}
                </select>
              </div>
              <div className="flex-end" style={{ marginTop: 12 }}>
                <Button variant="ghost" onClick={() => { setAdding(false); setTarget(''); }}>Cancel</Button>
                <Button loading={busy} disabled={!target} icon={<Plus size={15} />} onClick={addLink}>Add link</Button>
              </div>
            </div>
          ) : (
            <Button variant="secondary" icon={<Plus size={15} />} onClick={() => setAdding(true)} block>
              Add link
            </Button>
          )}
        </>
      )}
    </div>
  );
}

function LinkGroup({ icon, title, children }: { icon: React.ReactNode; title: string; children: React.ReactNode }) {
  return (
    <div className="link-group">
      <div className="link-group-title">{icon} {title}</div>
      {children}
    </div>
  );
}

function LinkRow({ item, onOpen, onRemove }: { item: LinkedTicket; onOpen: () => void; onRemove: () => void }) {
  return (
    <div className="link-row">
      <button type="button" className="link-row-main" onClick={onOpen}>
        <TypeBadge type={item.type} />
        <span className="link-row-title">{item.title}</span>
        <span className="link-row-state">{stateLabel(item.state)}</span>
      </button>
      <button type="button" className="link-row-remove" title="Remove link" onClick={onRemove}>
        <X size={14} />
      </button>
    </div>
  );
}
