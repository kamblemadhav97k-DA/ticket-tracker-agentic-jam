import { useEffect, useState } from 'react';
import { Info, Layers, Pencil, Plus, Trash2 } from 'lucide-react';
import { epicsApi, teamsApi } from '../api/endpoints';
import type { Epic, Team } from '../types';
import { relativeTime } from '../lib/format';
import { useErrorHandler } from '../lib/useErrorHandler';
import { Button } from '../components/ui/Button';
import { FormField } from '../components/ui/FormField';
import { Modal } from '../components/ui/Modal';
import { EmptyState, LoadingBlock } from '../components/ui/Feedback';
import { useToast } from '../components/ui/ToastProvider';
import { useConfirm } from '../components/ui/ConfirmProvider';

interface FormState { mode: 'create' | 'edit'; id?: string; title: string; description: string; }

export function EpicsPage() {
  const handleError = useErrorHandler();
  const toast = useToast();
  const confirm = useConfirm();

  const [teams, setTeams] = useState<Team[]>([]);
  const [teamId, setTeamId] = useState('');
  const [epics, setEpics] = useState<Epic[] | null>(null);
  const [form, setForm] = useState<FormState | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const list = await teamsApi.list();
        setTeams(list);
        if (list.length) setTeamId(list[0].id);
        else setEpics([]);
      } catch (e) { toast.error(handleError(e)); }
    })();
  }, []);

  async function loadEpics(id: string) {
    if (!id) { setEpics([]); return; }
    try {
      setEpics(await epicsApi.list(id));
    } catch (e) { toast.error(handleError(e)); }
  }

  useEffect(() => { if (teamId) { setEpics(null); void loadEpics(teamId); } }, [teamId]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form) return;
    setBusy(true);
    try {
      const description = form.description.trim() ? form.description.trim() : null;
      if (form.mode === 'edit' && form.id) {
        await epicsApi.update(form.id, form.title.trim(), description);
        toast.success('Epic updated.');
      } else {
        await epicsApi.create(teamId, form.title.trim(), description);
        toast.success('Epic created.');
      }
      setForm(null);
      await loadEpics(teamId);
    } catch (e) {
      toast.error(handleError(e));
    } finally {
      setBusy(false);
    }
  }

  async function remove(epic: Epic) {
    const ok = await confirm({
      title: 'Delete epic',
      message: `Delete "${epic.title}"? This cannot be undone.`,
      confirmText: 'Delete epic',
      danger: true,
    });
    if (!ok) return;
    try {
      await epicsApi.remove(epic.id);
      toast.success('Epic deleted.');
      await loadEpics(teamId);
    } catch (e) {
      toast.error(handleError(e));
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h1 className="page-title">Epics</h1>
          <p className="page-subtitle">Group related tickets under an epic within a team.</p>
          <div className="field team-select" style={{ marginTop: 14, marginBottom: 0 }}>
            <label className="field-label" htmlFor="team">Team</label>
            <select id="team" className="select" value={teamId} onChange={(e) => setTeamId(e.target.value)}>
              {teams.length === 0 && <option value="">No teams yet</option>}
              {teams.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
            </select>
          </div>
        </div>
        <Button icon={<Plus size={16} />} disabled={!teamId}
          onClick={() => setForm({ mode: 'create', title: '', description: '' })}>
          Create epic
        </Button>
      </div>

      <div className="card table-card">
        {!teamId ? (
          <EmptyState icon={<Layers size={26} />} title="No teams yet"
            description="Create a team first, then you can add epics to it." />
        ) : epics === null ? (
          <LoadingBlock label="Loading epics…" />
        ) : epics.length === 0 ? (
          <EmptyState
            icon={<Layers size={26} />}
            title="No epics for this team"
            description="Epics help you organise tickets around larger pieces of work."
            action={<Button icon={<Plus size={16} />} onClick={() => setForm({ mode: 'create', title: '', description: '' })}>Create epic</Button>}
          />
        ) : (
          <>
            <table className="table">
              <thead>
                <tr>
                  <th>Title</th>
                  <th className="num">Tickets</th>
                  <th>Modified</th>
                  <th className="actions">Actions</th>
                </tr>
              </thead>
              <tbody>
                {epics.map((epic) => (
                  <tr key={epic.id}>
                    <td>
                      <div className="cell-primary">{epic.title}</div>
                      {epic.description && <div className="cell-sub">{epic.description}</div>}
                    </td>
                    <td className="num">{epic.ticketCount}</td>
                    <td className="cell-muted">{relativeTime(epic.modifiedAt)}</td>
                    <td className="actions">
                      <span className="actions-cell">
                        <Button variant="secondary" size="sm" icon={<Pencil size={14} />}
                          onClick={() => setForm({ mode: 'edit', id: epic.id, title: epic.title, description: epic.description ?? '' })}>
                          Edit
                        </Button>
                        <Button variant="danger" size="icon" disabled={epic.ticketCount > 0}
                          title={epic.ticketCount > 0 ? 'Tickets reference this epic' : 'Delete epic'}
                          aria-label="Delete epic" onClick={() => remove(epic)}>
                          <Trash2 size={15} />
                        </Button>
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="table-note"><Info size={14} /> Delete is disabled while tickets reference the epic.</div>
          </>
        )}
      </div>

      <Modal
        open={form !== null}
        onClose={() => setForm(null)}
        title={form?.mode === 'edit' ? 'Edit epic' : 'Create epic'}
        subtitle={form?.mode === 'edit' ? 'Update the epic details.' : 'Add a new epic to this team.'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setForm(null)}>Cancel</Button>
            <Button type="submit" form="epic-form" loading={busy}>{form?.mode === 'edit' ? 'Save changes' : 'Create epic'}</Button>
          </>
        }
      >
        {form && (
          <form id="epic-form" onSubmit={submit}>
            <FormField label="Title" htmlFor="epic-title">
              <input id="epic-title" className="input" value={form.title}
                onChange={(e) => setForm({ ...form, title: e.target.value })} required autoFocus
                placeholder="e.g. Checkout reliability" />
            </FormField>
            <FormField label="Description" htmlFor="epic-desc" hint="Optional.">
              <textarea id="epic-desc" className="textarea" value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                placeholder="What is this epic about?" />
            </FormField>
          </form>
        )}
      </Modal>
    </>
  );
}
