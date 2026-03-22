import React, { useMemo, useState } from 'react';
import { MenuItem, Workflow, UILayout, Role } from '../types';
import { Check, GitBranch, Layout, Link as LinkIcon, Menu, Plus, Save, Trash2 } from 'lucide-react';

type MenuTarget = {
  id: string;
  label: string;
  helper?: string;
};

interface MenuEditorProps {
  menuItems: MenuItem[];
  workflows: Workflow[];
  forms: UILayout[];
  roles: Role[];
  onSaveMenu: (item: MenuItem) => void;
  onDeleteMenu: (id: string) => void;
  pageTargets?: MenuTarget[];
  currentUserId?: string;
}

const ICONS = ['FileText', 'GitBranch', 'Users', 'DollarSign', 'Briefcase', 'Shield', 'Settings', 'Database', 'Globe', 'Bell', 'Layout', 'Smartphone'];

const emptyMenuItem = (): MenuItem => ({
  id: `MNU-${Date.now()}`,
  label: '',
  icon: 'FileText',
  type: 'PAGE',
  targetId: 'form-designer',
  requiredRoleIds: [],
  description: '',
});

const MenuEditor: React.FC<MenuEditorProps> = ({ menuItems, workflows, forms, roles, onSaveMenu, onDeleteMenu, pageTargets = [], currentUserId }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editItem, setEditItem] = useState<MenuItem>(emptyMenuItem());

  const formOptions = useMemo(
    () => forms.map((form) => ({
      id: form.id,
      label: form.name,
      helper: form.createdBy === currentUserId ? 'Editable form' : 'Read-only template',
    })),
    [forms, currentUserId]
  );

  const handleEdit = (item: MenuItem) => {
    setEditItem({ ...item, requiredRoleIds: item.requiredRoleIds || [], description: item.description || '' });
    setIsEditing(true);
  };

  const handleCreate = () => {
    setEditItem(emptyMenuItem());
    setIsEditing(true);
  };

  const handleSave = () => {
    if (!editItem.label.trim() || !editItem.targetId) return;
    onSaveMenu({
      ...editItem,
      label: editItem.label.trim(),
      requiredRoleIds: editItem.requiredRoleIds || [],
      description: editItem.description?.trim(),
    });
    setIsEditing(false);
  };

  const toggleRole = (roleId: string) => {
    const current = new Set(editItem.requiredRoleIds || []);
    if (current.has(roleId)) current.delete(roleId);
    else current.add(roleId);
    setEditItem({ ...editItem, requiredRoleIds: Array.from(current) });
  };

  const availableTargets = editItem.type === 'WORKFLOW'
    ? workflows.map((workflow) => ({ id: workflow.id, label: workflow.name, helper: workflow.trigger }))
    : editItem.type === 'FORM'
      ? formOptions
      : pageTargets;

  return (
    <div className="grid h-full gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
      <div className="rounded-[24px] border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
        <div className="flex items-center justify-between rounded-t-[24px] border-b border-slate-200 bg-slate-50 px-4 py-4 dark:border-slate-700 dark:bg-slate-900">
          <h3 className="flex items-center gap-2 font-heading text-lg font-bold text-slate-950 dark:text-white">
            <Menu size={18} className="text-brand-600" />
            Menu Configuration
          </h3>
          <button onClick={handleCreate} className="rounded-2xl bg-slate-950 p-2 text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
            <Plus size={16} />
          </button>
        </div>
        <div className="space-y-3 p-3">
          {menuItems.map((item) => (
            <div key={item.id} className="flex items-start justify-between gap-3 rounded-[20px] border border-slate-200 bg-slate-50/85 p-4 dark:border-slate-700 dark:bg-slate-800/55">
              <button className="flex-1 text-left" onClick={() => handleEdit(item)}>
                <div className="font-semibold text-slate-950 dark:text-white">{item.label}</div>
                <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">{item.type}</div>
                {item.description && <div className="mt-2 text-sm text-slate-500 dark:text-slate-400">{item.description}</div>}
              </button>
              <button onClick={() => onDeleteMenu(item.id)} className="rounded-xl p-2 text-slate-400 hover:bg-danger-50 hover:text-danger-600 dark:hover:bg-danger-500/10 dark:hover:text-danger-300">
                <Trash2 size={16} />
              </button>
            </div>
          ))}
          {menuItems.length === 0 && (
            <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
              No custom menu items defined yet.
            </div>
          )}
        </div>
      </div>

      <div className="rounded-[24px] border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
        {isEditing ? (
          <>
            <div className="flex items-center justify-between rounded-t-[24px] border-b border-slate-200 bg-slate-50 px-5 py-4 dark:border-slate-700 dark:bg-slate-900">
              <div>
                <h3 className="font-heading text-lg font-bold text-slate-950 dark:text-white">Menu Item Editor</h3>
                <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Link a sidebar item to a page, workflow, or form.</p>
              </div>
              <button onClick={handleSave} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
                <Save size={16} /> Save Item
              </button>
            </div>
            <div className="space-y-6 p-6">
              <div className="grid gap-4 md:grid-cols-2">
                <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                  Menu Label
                  <input type="text" className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-900" value={editItem.label} onChange={(e) => setEditItem({ ...editItem, label: e.target.value })} placeholder="e.g. Mobile Money Cashier" />
                </label>
                <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                  Icon
                  <select className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-900" value={editItem.icon} onChange={(e) => setEditItem({ ...editItem, icon: e.target.value })}>
                    {ICONS.map((icon) => <option key={icon} value={icon}>{icon}</option>)}
                  </select>
                </label>
                <label className="text-sm font-medium text-slate-700 dark:text-slate-300 md:col-span-2">
                  Description
                  <textarea className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-900" rows={3} value={editItem.description || ''} onChange={(e) => setEditItem({ ...editItem, description: e.target.value })} placeholder="Short context for operations staff" />
                </label>
              </div>

              <div>
                <label className="block text-xs font-bold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Action Type</label>
                <div className="mt-3 grid gap-3 md:grid-cols-3">
                  {[
                    { id: 'PAGE', label: 'Page Link', icon: <LinkIcon size={18} /> },
                    { id: 'FORM', label: 'Custom Form', icon: <Layout size={18} /> },
                    { id: 'WORKFLOW', label: 'Business Process', icon: <GitBranch size={18} /> },
                  ].map((type) => (
                    <button
                      key={type.id}
                      onClick={() => setEditItem({ ...editItem, type: type.id as MenuItem['type'], targetId: '' })}
                      className={`flex items-center justify-center gap-2 rounded-2xl border px-4 py-4 text-sm font-medium ${editItem.type === type.id ? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-400 dark:bg-brand-900/20 dark:text-brand-300' : 'border-slate-200 text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-700/50'}`}
                    >
                      {type.icon}
                      {type.label}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Target Object</label>
                <select className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-900" value={editItem.targetId} onChange={(e) => setEditItem({ ...editItem, targetId: e.target.value })}>
                  <option value="">-- Select Target --</option>
                  {availableTargets.map((target) => (
                    <option key={target.id} value={target.id}>{target.helper ? `${target.label} - ${target.helper}` : target.label}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-bold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Visible To Roles</label>
                <div className="mt-3 grid gap-2 md:grid-cols-2">
                  {roles.map((role) => (
                    <button key={role.id} onClick={() => toggleRole(role.id)} className={`flex items-center gap-3 rounded-2xl border px-3 py-3 text-left ${editItem.requiredRoleIds?.includes(role.id) ? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-400 dark:bg-brand-900/20 dark:text-brand-300' : 'border-slate-200 text-slate-600 dark:border-slate-700 dark:text-slate-300'}`}>
                      <span className={`flex h-5 w-5 items-center justify-center rounded-md border ${editItem.requiredRoleIds?.includes(role.id) ? 'border-brand-500 bg-brand-500 text-white' : 'border-slate-300 dark:border-slate-600'}`}>
                        {editItem.requiredRoleIds?.includes(role.id) && <Check size={12} />}
                      </span>
                      <span className="text-sm font-medium">{role.name}</span>
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </>
        ) : (
          <div className="flex min-h-[420px] flex-col items-center justify-center text-center text-slate-500 dark:text-slate-400">
            <LinkIcon size={44} className="mb-4 opacity-20" />
            <p>Select an item to edit or create a new sidebar link.</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default MenuEditor;
