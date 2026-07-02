import { useEffect, useState } from 'react';
import { Info, Pencil, Plus, Trash2, Users } from 'lucide-react';
import { teamsApi } from '../api/endpoints';
import type { Team } from '../types';
import { relativeTime } from '../lib/format';
import { useErrorHandler } from '../lib/useErrorHandler';
import { Button } from '../components/ui/Button';
import { FormField } from '../components/ui/FormField';
import { Modal } from '../components/ui/Modal';
import { EmptyState, LoadingBlock } from '../components/ui/Feedback';
import { useToast } from '../components/ui/ToastProvider';
import { useConfirm } from '../components/ui/ConfirmProvider';

export function TeamsPage() {
  const handleError = useErrorHandler();
  const toast = useToast();
  const confirm = useConfirm();

  const [teams, setTeams] = useState<Team[] | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Team | null>(null);
  const [name, setName] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    try {
      setTeams(await teamsApi.list());
    } catch (e) {
      toast.error(handleError(e));
    }
  }

  useEffect(() => { void load(); }, []);

  function openCreate() { setEditing(null); setName(''); setFormOpen(true); }
  function openEdit(team: Team) { setEditing(team); setName(team.name); setFormOpen(true); }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      if (editing) {
        await teamsApi.rename(editing.id, name.trim());
        toast.success('Team renamed.');
      } else {
        await teamsApi.create(name.trim());
        toast.success('Team created.');
      }
      setFormOpen(false);
      await load();
    } catch (e) {
      toast.error(handleError(e));
    } finally {
      setBusy(false);
    }
  }

  async function remove(team: Team) {
    const ok = await confirm({
      title: 'Delete team',
      message: `Delete "${team.name}"? This cannot be undone.`,
      confirmText: 'Delete team',
      danger: true,
    });
    if (!ok) return;
    try {
      await teamsApi.remove(team.id);
      toast.success('Team deleted.');
      await load();
    } catch (e) {
      toast.error(handleError(e));
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h1 className="page-title">Teams</h1>
          <p className="page-subtitle">All verified users can view and manage all teams.</p>
        </div>
        <Button icon={<Plus size={16} />} onClick={openCreate}>Create team</Button>
      </div>

      <div className="card table-card">
        {teams === null ? (
          <LoadingBlock label="Loading teams…" />
        ) : teams.length === 0 ? (
          <EmptyState
            icon={<Users size={26} />}
            title="No teams yet"
            description="Teams group your epics and tickets. Create your first team to get started."
            action={<Button icon={<Plus size={16} />} onClick={openCreate}>Create team</Button>}
          />
        ) : (
          <>
            <table className="table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th className="num">Tickets</th>
                  <th className="num">Epics</th>
                  <th>Modified</th>
                  <th className="actions">Actions</th>
                </tr>
              </thead>
              <tbody>
                {teams.map((team) => {
                  const referenced = team.ticketCount > 0 || team.epicCount > 0;
                  return (
                    <tr key={team.id}>
                      <td className="cell-primary">{team.name}</td>
                      <td className="num">{team.ticketCount}</td>
                      <td className="num">{team.epicCount}</td>
                      <td className="cell-muted">{relativeTime(team.modifiedAt)}</td>
                      <td className="actions">
                        <span className="actions-cell">
                          <Button variant="secondary" size="sm" icon={<Pencil size={14} />} onClick={() => openEdit(team)}>
                            Edit
                          </Button>
                          <Button variant="danger" size="sm" icon={<Trash2 size={14} />} disabled={referenced}
                            title={referenced ? 'Team contains tickets or epics' : undefined}
                            onClick={() => remove(team)}>
                            Delete
                          </Button>
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            <div className="table-note"><Info size={14} /> Delete is disabled while a team contains tickets or epics.</div>
          </>
        )}
      </div>

      <Modal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        title={editing ? 'Edit team' : 'Create team'}
        subtitle={editing ? 'Rename this team.' : 'Give your new team a unique name.'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setFormOpen(false)}>Cancel</Button>
            <Button type="submit" form="team-form" loading={busy}>{editing ? 'Save changes' : 'Create team'}</Button>
          </>
        }
      >
        <form id="team-form" onSubmit={submit}>
          <FormField label="Team name" htmlFor="team-name">
            <input id="team-name" className="input" placeholder="e.g. Platform Engineering"
              value={name} onChange={(e) => setName(e.target.value)} required autoFocus />
          </FormField>
        </form>
      </Modal>
    </>
  );
}
