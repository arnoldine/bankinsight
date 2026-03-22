
import React, { useEffect, useMemo, useState } from 'react';
import { User } from '../services/authService';
import { UILayout, UIComponentConfig, UIComponentType } from '../../types';
import { workflowRuntimeService } from '../services/workflowRuntimeService';
import { LocalFormSubmission, localRegistryService } from '../services/localRegistryService';
import {
  CheckCircle2,
  Copy,
  Eye,
  FileText,
  LayoutDashboard,
  Lock,
  Pencil,
  Play,
  Plus,
  Save,
  Send,
  Sparkles,
  Trash2,
  XCircle,
} from 'lucide-react';

type FormDesignerScreenProps = {
  user: User;
  forms: UILayout[];
  onSaveForm: (layout: UILayout) => void;
  initialFormId?: string | null;
};

type RunnerValues = Record<string, string>;

type SubmissionFeedback = {
  tone: 'success' | 'warning' | 'error';
  title: string;
  message: string;
};

const COMPONENT_TYPES: Array<{ id: UIComponentType; label: string }> = [
  { id: 'HEADER', label: 'Header' },
  { id: 'TEXT_INPUT', label: 'Text Field' },
  { id: 'NUMBER_INPUT', label: 'Number Field' },
  { id: 'DATE_PICKER', label: 'Date Picker' },
  { id: 'SELECT', label: 'Dropdown' },
  { id: 'CARD', label: 'Card Section' },
  { id: 'TABLE', label: 'Data Table' },
];

function createBlankLayout(user: User): UILayout {
  const now = new Date().toISOString();
  return {
    id: `FORM-${Date.now()}`,
    name: 'New Form',
    description: '',
    published: false,
    components: [],
    menuLabel: '',
    menuIcon: 'Layout',
    createdBy: user.id,
    createdByName: user.name,
    isTemplate: false,
    isLocked: false,
    createdAt: now,
    updatedAt: now,
  };
}

function cloneLayout(layout: UILayout): UILayout {
  return {
    ...layout,
    components: (layout.components || []).map((component) => ({ ...component })),
  };
}

function componentLabel(type: UIComponentType): string {
  const match = COMPONENT_TYPES.find((item) => item.id === type);
  return match?.label || type;
}

function createInitialValues(layout: UILayout | null): RunnerValues {
  const values: RunnerValues = {};
  for (const component of layout?.components || []) {
    if (component.variableName) {
      values[component.variableName] = '';
    }
  }
  return values;
}

function buildSelectOptions(component: UIComponentConfig): string[] {
  return (component.placeholder || '')
    .split(',')
    .map((value) => value.trim())
    .filter(Boolean);
}

function getFieldKey(component: UIComponentConfig): string {
  return component.variableName || component.id;
}

function getWidthClass(width: UIComponentConfig['width']): string {
  if (width === 'FULL') return 'md:col-span-2';
  return '';
}

function getFeedbackMessage(error: unknown, fallback: string): string {
  if (typeof error === 'object' && error && 'data' in error) {
    const apiError = error as { data?: { message?: string; error?: string } };
    if (apiError.data?.message) return apiError.data.message;
    if (apiError.data?.error) return apiError.data.error;
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallback;
}

export default function FormDesignerScreen({ user, forms, onSaveForm, initialFormId }: FormDesignerScreenProps) {
  const [selectedFormId, setSelectedFormId] = useState<string | null>(initialFormId || forms[0]?.id || null);
  const [editorLayout, setEditorLayout] = useState<UILayout | null>(null);
  const [runnerValues, setRunnerValues] = useState<RunnerValues>({});
  const [runnerFeedback, setRunnerFeedback] = useState<SubmissionFeedback | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [recentSubmissions, setRecentSubmissions] = useState<LocalFormSubmission[]>([]);

  useEffect(() => {
    if (!forms.length) {
      setSelectedFormId(null);
      return;
    }

    if (initialFormId && forms.some((form) => form.id === initialFormId)) {
      setSelectedFormId(initialFormId);
      return;
    }

    if (!selectedFormId || !forms.some((form) => form.id === selectedFormId)) {
      setSelectedFormId(forms[0].id);
    }
  }, [forms, initialFormId, selectedFormId]);

  const selectedForm = useMemo(
    () => forms.find((form) => form.id === selectedFormId) || null,
    [forms, selectedFormId]
  );

  useEffect(() => {
    setRunnerValues(createInitialValues(selectedForm));
    setRunnerFeedback(null);
    setRecentSubmissions(selectedForm ? localRegistryService.getFormSubmissions(selectedForm.id) : []);
  }, [selectedForm]);

  const isEditing = Boolean(editorLayout);

  const startNewForm = () => {
    setEditorLayout(createBlankLayout(user));
  };

  const startEditForm = (layout: UILayout) => {
    if (layout.createdBy !== user.id) {
      return;
    }
    setEditorLayout(cloneLayout(layout));
  };

  const duplicateTemplate = (layout: UILayout) => {
    const now = new Date().toISOString();
    setEditorLayout({
      ...cloneLayout(layout),
      id: `FORM-${Date.now()}`,
      name: `${layout.name} Copy`,
      published: false,
      createdBy: user.id,
      createdByName: user.name,
      isTemplate: false,
      isLocked: false,
      createdAt: now,
      updatedAt: now,
    });
  };

  const updateLayout = (updates: Partial<UILayout>) => {
    setEditorLayout((current) => (current ? { ...current, ...updates } : current));
  };

  const addComponent = () => {
    setEditorLayout((current) => {
      if (!current) return current;
      const nextComponent: UIComponentConfig = {
        id: `cmp_${Date.now()}`,
        type: 'TEXT_INPUT',
        label: 'New Field',
        width: 'FULL',
        required: false,
        placeholder: '',
        variableName: '',
      };
      return { ...current, components: [...(current.components || []), nextComponent] };
    });
  };

  const updateComponent = (componentId: string, updates: Partial<UIComponentConfig>) => {
    setEditorLayout((current) => {
      if (!current) return current;
      return {
        ...current,
        components: (current.components || []).map((component) =>
          component.id === componentId ? { ...component, ...updates } : component
        ),
      };
    });
  };

  const removeComponent = (componentId: string) => {
    setEditorLayout((current) => {
      if (!current) return current;
      return {
        ...current,
        components: (current.components || []).filter((component) => component.id !== componentId),
      };
    });
  };

  const handleSave = () => {
    if (!editorLayout) return;
    const now = new Date().toISOString();
    onSaveForm({
      ...editorLayout,
      updatedAt: now,
      createdBy: editorLayout.createdBy || user.id,
      createdByName: editorLayout.createdByName || user.name,
    });
    setEditorLayout(null);
    setSelectedFormId(editorLayout.id);
  };

  const updateRunnerValue = (component: UIComponentConfig, value: string) => {
    const key = getFieldKey(component);
    setRunnerValues((current) => ({ ...current, [key]: value }));
  };

  const submitRunner = async () => {
    if (!selectedForm) return;

    const components = (selectedForm.components || []).filter((component) => component.type !== 'HEADER');
    const missing = components.filter((component) => component.required && !String(runnerValues[getFieldKey(component)] || '').trim());
    if (missing.length > 0) {
      setRunnerFeedback({
        tone: 'error',
        title: 'Required fields missing',
        message: `${missing[0].label} is required before you can submit this form.`,
      });
      return;
    }

    const payload = Object.fromEntries(
      components.map((component) => [getFieldKey(component), runnerValues[getFieldKey(component)] || ''])
    );
    const submissionId = `SUB-${Date.now()}`;
    const correlationId = `FORM-${selectedForm.id}-${Date.now()}`;

    setIsSubmitting(true);
    setRunnerFeedback(null);

    try {
      let submission: LocalFormSubmission;

      if (selectedForm.linkedWorkflow) {
        const result = await workflowRuntimeService.startProcess(
          {
            entityType: selectedForm.linkedTable || 'CUSTOM_FORM',
            entityId: submissionId,
            correlationId,
            payloadJson: JSON.stringify({
              formId: selectedForm.id,
              formName: selectedForm.name,
              submittedAt: new Date().toISOString(),
              submittedBy: { id: user.id, name: user.name, role: user.role },
              data: payload,
            }),
          },
          selectedForm.linkedWorkflow
        );

        submission = {
          id: submissionId,
          formId: selectedForm.id,
          formName: selectedForm.name,
          submittedAt: new Date().toISOString(),
          submittedBy: user.name,
          status: 'STARTED',
          linkedWorkflow: selectedForm.linkedWorkflow,
          linkedTable: selectedForm.linkedTable,
          correlationId,
          instanceId: result.instanceId,
          message: `Workflow started with status ${result.status}.`,
          payload,
        };

        setRunnerFeedback({
          tone: 'success',
          title: 'Workflow started',
          message: `${selectedForm.name} was submitted and routed into ${selectedForm.linkedWorkflow}.`,
        });
      } else {
        submission = {
          id: submissionId,
          formId: selectedForm.id,
          formName: selectedForm.name,
          submittedAt: new Date().toISOString(),
          submittedBy: user.name,
          status: 'LOCAL_ONLY',
          linkedTable: selectedForm.linkedTable,
          message: 'Saved locally because this form is not linked to a workflow yet.',
          payload,
        };

        setRunnerFeedback({
          tone: 'warning',
          title: 'Saved locally',
          message: 'This form is not linked to a workflow yet, so the submission was stored locally for design review.',
        });
      }

      localRegistryService.saveFormSubmission(submission);
      setRecentSubmissions(localRegistryService.getFormSubmissions(selectedForm.id));
      setRunnerValues(createInitialValues(selectedForm));
    } catch (error) {
      const failedSubmission: LocalFormSubmission = {
        id: submissionId,
        formId: selectedForm.id,
        formName: selectedForm.name,
        submittedAt: new Date().toISOString(),
        submittedBy: user.name,
        status: 'FAILED',
        linkedWorkflow: selectedForm.linkedWorkflow,
        linkedTable: selectedForm.linkedTable,
        correlationId,
        message: getFeedbackMessage(error, 'The workflow could not be started.'),
        payload,
      };

      localRegistryService.saveFormSubmission(failedSubmission);
      setRecentSubmissions(localRegistryService.getFormSubmissions(selectedForm.id));
      setRunnerFeedback({
        tone: 'error',
        title: 'Submission failed',
        message: getFeedbackMessage(error, 'The workflow could not be started.'),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="simple-screen grid gap-6 xl:grid-cols-[340px_minmax(0,1fr)]">
      <section className="screen-panel p-5">
        <div className="flex items-start justify-between gap-4 border-b border-slate-200/80 pb-4 dark:border-slate-700/80">
          <div>
            <div className="text-xs font-semibold text-brand-700 dark:text-brand-300">Form catalog</div>
            <h2 className="mt-2 text-xl font-heading font-bold text-slate-950 dark:text-white">Form Designer</h2>
            <p className="mt-2 text-sm leading-6 text-slate-500 dark:text-slate-400">
              Templates remain read-only. Forms created by you can be edited and published back into the platform.
            </p>
          </div>
          <button onClick={startNewForm} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
            <Plus className="h-4 w-4" />
            New Form
          </button>
        </div>

        <div className="mt-5 space-y-3">
          {forms.map((form) => {
            const owned = form.createdBy === user.id;
            const selected = form.id === selectedFormId;
            return (
              <button
                key={form.id}
                onClick={() => {
                  setEditorLayout(null);
                  setSelectedFormId(form.id);
                }}
                className={`w-full rounded-[22px] border px-4 py-4 text-left transition ${selected ? 'border-brand-500 bg-brand-50/80 dark:border-brand-400 dark:bg-brand-900/20' : 'border-slate-200/80 bg-slate-50/85 hover:border-brand-200 hover:bg-white dark:border-slate-700/80 dark:bg-slate-800/55 dark:hover:border-brand-500/30'}`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-semibold text-slate-950 dark:text-white">{form.name}</div>
                    <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{form.menuLabel || form.linkedWorkflow || 'Unlinked form'}</div>
                  </div>
                  <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] ${owned ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300' : 'bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-200'}`}>
                    {owned ? 'Editable' : 'Read only'}
                  </span>
                </div>
                <p className="mt-3 text-sm leading-6 text-slate-500 dark:text-slate-400">{form.description || 'No description provided yet.'}</p>
                <div className="mt-3 flex items-center gap-2 text-xs text-slate-400 dark:text-slate-500">
                  {form.isTemplate ? <Sparkles className="h-3.5 w-3.5" /> : <FileText className="h-3.5 w-3.5" />}
                  <span>{form.createdByName || 'Unknown owner'}</span>
                </div>
              </button>
            );
          })}
        </div>
      </section>

      <section className="screen-panel p-6">
        {isEditing && editorLayout ? (
          <div className="space-y-6">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200/80 pb-4 dark:border-slate-700/80">
              <div>
                <div className="text-xs font-semibold text-brand-700 dark:text-brand-300">Form design</div>
                <h3 className="mt-2 text-xl font-heading font-bold text-slate-950 dark:text-white">{editorLayout.name || 'Untitled Form'}</h3>
                <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Edit structure, publication flags, and field bindings for your owned forms.</p>
              </div>
              <div className="flex flex-wrap gap-3">
                <button onClick={() => setEditorLayout(null)} className="rounded-2xl border border-slate-300 px-4 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800">Cancel</button>
                <button onClick={handleSave} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
                  <Save className="h-4 w-4" /> Save Form
                </button>
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                Form Name
                <input value={editorLayout.name} onChange={(event) => updateLayout({ name: event.target.value })} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
              </label>
              <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                Menu Label
                <input value={editorLayout.menuLabel || ''} onChange={(event) => updateLayout({ menuLabel: event.target.value })} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Form menu caption" />
              </label>
              <label className="text-sm font-medium text-slate-700 dark:text-slate-300 md:col-span-2">
                Description
                <textarea value={editorLayout.description || ''} onChange={(event) => updateLayout({ description: event.target.value })} rows={3} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
              </label>
              <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                Linked Table
                <input value={editorLayout.linkedTable || ''} onChange={(event) => updateLayout({ linkedTable: event.target.value })} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="m_mobile_money_transaction" />
              </label>
              <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                Linked Workflow
                <input value={editorLayout.linkedWorkflow || ''} onChange={(event) => updateLayout({ linkedWorkflow: event.target.value })} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="MOMO_CASHIER_AUTOMATION" />
              </label>
            </div>

            <div className="rounded-[24px] border border-slate-200/80 bg-slate-50/85 p-4 dark:border-slate-700/80 dark:bg-slate-800/55">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <h4 className="text-lg font-heading font-bold text-slate-950 dark:text-white">Field Layout</h4>
                  <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Build the form structure used by cashiers or operations teams.</p>
                </div>
                <button onClick={addComponent} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900/70 dark:text-slate-200 dark:hover:bg-slate-800">
                  <Plus className="h-4 w-4" /> Add Field
                </button>
              </div>

              <div className="mt-4 space-y-3">
                {(editorLayout.components || []).length === 0 ? (
                  <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">No fields yet. Add a field to begin shaping the form.</div>
                ) : (
                  (editorLayout.components || []).map((component) => (
                    <div key={component.id} className="grid gap-3 rounded-[20px] border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900/70 lg:grid-cols-[1.2fr_1fr_1fr_120px_80px]">
                      <input value={component.label} onChange={(event) => updateComponent(component.id, { label: event.target.value })} className="rounded-2xl border border-slate-300/90 bg-white px-3 py-2 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Field label" />
                      <select value={component.type} onChange={(event) => updateComponent(component.id, { type: event.target.value as UIComponentType })} className="rounded-2xl border border-slate-300/90 bg-white px-3 py-2 text-sm dark:border-slate-600 dark:bg-slate-950/80">
                        {COMPONENT_TYPES.map((item) => (
                          <option key={item.id} value={item.id}>{item.label}</option>
                        ))}
                      </select>
                      <input value={component.variableName || ''} onChange={(event) => updateComponent(component.id, { variableName: event.target.value })} className="rounded-2xl border border-slate-300/90 bg-white px-3 py-2 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="binding_name" />
                      <select value={component.width} onChange={(event) => updateComponent(component.id, { width: event.target.value as UIComponentConfig['width'] })} className="rounded-2xl border border-slate-300/90 bg-white px-3 py-2 text-sm dark:border-slate-600 dark:bg-slate-950/80">
                        <option value="FULL">Full</option>
                        <option value="HALF">Half</option>
                        <option value="THIRD">Third</option>
                      </select>
                      <button onClick={() => removeComponent(component.id)} className="inline-flex items-center justify-center rounded-2xl bg-danger-50 px-3 py-2 text-danger-600 hover:bg-danger-100 dark:bg-danger-500/10 dark:text-danger-300 dark:hover:bg-danger-500/20">
                        <Trash2 className="h-4 w-4" />
                      </button>
                      <input value={component.placeholder || ''} onChange={(event) => updateComponent(component.id, { placeholder: event.target.value })} className="rounded-2xl border border-slate-300/90 bg-white px-3 py-2 text-sm dark:border-slate-600 dark:bg-slate-950/80 lg:col-span-4" placeholder="Placeholder / guidance text" />
                      <label className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 dark:border-slate-600 dark:bg-slate-900/70 dark:text-slate-200">
                        <input type="checkbox" checked={component.required || false} onChange={(event) => updateComponent(component.id, { required: event.target.checked })} />
                        Required
                      </label>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        ) : selectedForm ? (
          <div className="space-y-6">
            <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-200/80 pb-4 dark:border-slate-700/80">
              <div>
                <div className="text-xs font-semibold text-brand-700 dark:text-brand-300">Form details</div>
                <h3 className="mt-2 text-xl font-heading font-bold text-slate-950 dark:text-white">{selectedForm.name}</h3>
                <p className="mt-2 text-sm leading-6 text-slate-500 dark:text-slate-400">{selectedForm.description || 'No description has been written for this form yet.'}</p>
              </div>
              <div className="flex flex-wrap gap-3">
                {selectedForm.createdBy === user.id ? (
                  <button onClick={() => startEditForm(selectedForm)} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
                    <Pencil className="h-4 w-4" /> Edit Form
                  </button>
                ) : (
                  <button onClick={() => duplicateTemplate(selectedForm)} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900/70 dark:text-slate-200 dark:hover:bg-slate-800">
                    <Copy className="h-4 w-4" /> Duplicate to My Forms
                  </button>
                )}
                <button onClick={submitRunner} disabled={isSubmitting} className="inline-flex items-center gap-2 rounded-2xl border border-brand-300 bg-brand-50 px-4 py-3 text-sm font-medium text-brand-700 hover:bg-brand-100 disabled:cursor-not-allowed disabled:opacity-60 dark:border-brand-500/40 dark:bg-brand-500/10 dark:text-brand-200 dark:hover:bg-brand-500/20">
                  {selectedForm.linkedWorkflow ? <Send className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                  {isSubmitting ? 'Submitting...' : selectedForm.linkedWorkflow ? 'Run Workflow' : 'Save Sample Submission'}
                </button>
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-4">
              <InfoTile label="Mode" value={selectedForm.createdBy === user.id ? 'Editable' : 'Read only'} icon={selectedForm.createdBy === user.id ? <Pencil className="h-4 w-4" /> : <Lock className="h-4 w-4" />} />
              <InfoTile label="Owner" value={selectedForm.createdByName || 'Unknown'} icon={<LayoutDashboard className="h-4 w-4" />} />
              <InfoTile label="Workflow" value={selectedForm.linkedWorkflow || 'Not linked'} icon={<Sparkles className="h-4 w-4" />} />
              <InfoTile label="Table" value={selectedForm.linkedTable || 'Not linked'} icon={<FileText className="h-4 w-4" />} />
            </div>

            {runnerFeedback && (
              <div className={`rounded-[22px] border px-4 py-4 ${runnerFeedback.tone === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200' : runnerFeedback.tone === 'warning' ? 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-100' : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200'}`}>
                <div className="flex items-start gap-3">
                  {runnerFeedback.tone === 'success' ? <CheckCircle2 className="mt-0.5 h-5 w-5" /> : runnerFeedback.tone === 'warning' ? <Sparkles className="mt-0.5 h-5 w-5" /> : <XCircle className="mt-0.5 h-5 w-5" />}
                  <div>
                    <div className="font-semibold">{runnerFeedback.title}</div>
                    <div className="mt-1 text-sm">{runnerFeedback.message}</div>
                  </div>
                </div>
              </div>
            )}

            <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_340px]">
              <div className="rounded-[24px] border border-slate-200/80 bg-slate-50/85 p-5 dark:border-slate-700/80 dark:bg-slate-800/55">
                <div className="flex items-center gap-2 text-lg font-heading font-bold text-slate-950 dark:text-white">
                  <Eye className="h-5 w-5 text-brand-600 dark:text-brand-300" />
                  Live form runner
                </div>
                <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
                  Templates stay read-only structurally, but operations staff can still run them with live payload values.
                </p>
                <div className="mt-4 grid gap-3 md:grid-cols-2">
                  {(selectedForm.components || []).length === 0 ? (
                    <div className="md:col-span-2 rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                      No fields are configured on this form yet.
                    </div>
                  ) : (
                    (selectedForm.components || []).map((component) => {
                      if (component.type === 'HEADER') {
                        return (
                          <div key={component.id} className="md:col-span-2 rounded-[20px] border border-slate-200 bg-white px-4 py-4 dark:border-slate-700 dark:bg-slate-900/70">
                            <div className="text-base font-semibold text-slate-950 dark:text-white">{component.label}</div>
                            {component.placeholder && <div className="mt-1 text-sm text-slate-500 dark:text-slate-400">{component.placeholder}</div>}
                          </div>
                        );
                      }

                      const fieldKey = getFieldKey(component);
                      const options = buildSelectOptions(component);
                      const sharedClassName = 'mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80';

                      return (
                        <label key={component.id} className={`rounded-[20px] border border-slate-200 bg-white p-4 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-900/70 dark:text-slate-200 ${getWidthClass(component.width)}`}>
                          <div className="flex items-center justify-between gap-3">
                            <span>{component.label}</span>
                            <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">{componentLabel(component.type)}</span>
                          </div>
                          {component.type === 'SELECT' && options.length > 0 ? (
                            <select value={runnerValues[fieldKey] || ''} onChange={(event) => updateRunnerValue(component, event.target.value)} className={sharedClassName}>
                              <option value="">Select option</option>
                              {options.map((option) => (
                                <option key={option} value={option}>{option}</option>
                              ))}
                            </select>
                          ) : (
                            <input
                              type={component.type === 'NUMBER_INPUT' ? 'number' : component.type === 'DATE_PICKER' ? 'date' : 'text'}
                              value={runnerValues[fieldKey] || ''}
                              onChange={(event) => updateRunnerValue(component, event.target.value)}
                              className={sharedClassName}
                              placeholder={component.placeholder || component.variableName || component.label}
                            />
                          )}
                          <div className="mt-2 text-xs text-slate-500 dark:text-slate-400">Binding: {fieldKey}</div>
                          {component.required && <div className="mt-2 text-xs font-semibold uppercase tracking-[0.18em] text-danger-600 dark:text-danger-300">Required field</div>}
                        </label>
                      );
                    })
                  )}
                </div>
              </div>

              <div className="space-y-4">
                <div className="rounded-[24px] border border-slate-200/80 bg-white p-5 dark:border-slate-700/80 dark:bg-slate-900/70">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">Automation target</div>
                  <h4 className="mt-2 text-lg font-heading font-bold text-slate-950 dark:text-white">Submission routing</h4>
                  <div className="mt-4 space-y-3 text-sm text-slate-500 dark:text-slate-400">
                    <div className="rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">Workflow code</div>
                      <div className="mt-1 font-semibold text-slate-950 dark:text-white">{selectedForm.linkedWorkflow || 'Not linked'}</div>
                    </div>
                    <div className="rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">Entity type</div>
                      <div className="mt-1 font-semibold text-slate-950 dark:text-white">{selectedForm.linkedTable || 'CUSTOM_FORM'}</div>
                    </div>
                    <p>
                      Use this runner as the operating template for cashier mobile money capture. If the form has a linked workflow, submission starts the backend process immediately.
                    </p>
                  </div>
                </div>

                <div className="rounded-[24px] border border-slate-200/80 bg-white p-5 dark:border-slate-700/80 dark:bg-slate-900/70">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">Recent submissions</div>
                  <h4 className="mt-2 text-lg font-heading font-bold text-slate-950 dark:text-white">Submission history</h4>
                  <div className="mt-4 space-y-3">
                    {recentSubmissions.length === 0 ? (
                      <div className="rounded-[18px] border border-dashed border-slate-300 px-4 py-6 text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                        No submissions recorded for this form yet.
                      </div>
                    ) : (
                      recentSubmissions.slice(0, 5).map((submission) => (
                        <div key={submission.id} className="rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                          <div className="flex items-center justify-between gap-3">
                            <div className="text-sm font-semibold text-slate-950 dark:text-white">{submission.status.replace('_', ' ')}</div>
                            <div className="text-xs text-slate-400 dark:text-slate-500">{new Date(submission.submittedAt).toLocaleString()}</div>
                          </div>
                          <div className="mt-2 text-sm text-slate-500 dark:text-slate-400">{submission.message || 'Submission recorded.'}</div>
                          {submission.instanceId && <div className="mt-2 text-xs text-slate-400 dark:text-slate-500">Instance: {submission.instanceId}</div>}
                        </div>
                      ))
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        ) : (
          <div className="flex h-full min-h-[400px] items-center justify-center rounded-[24px] border border-dashed border-slate-300 text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
            No forms are available yet.
          </div>
        )}
      </section>
    </div>
  );
}

function InfoTile({ label, value, icon }: { label: string; value: string; icon: React.ReactNode }) {
  return (
    <div className="rounded-[22px] border border-slate-200/80 bg-white p-4 dark:border-slate-700/80 dark:bg-slate-900/70">
      <div className="flex items-center justify-between gap-3 text-slate-500 dark:text-slate-400">
        <span className="text-xs font-semibold uppercase tracking-[0.18em]">{label}</span>
        {icon}
      </div>
      <div className="mt-3 text-sm font-semibold text-slate-950 dark:text-white">{value}</div>
    </div>
  );
}
