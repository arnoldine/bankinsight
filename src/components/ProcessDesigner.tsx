
import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw, GitBranch, CheckCircle2, AlertTriangle, Rocket, Layers3 } from 'lucide-react';
import { Permissions } from '../../lib/Permissions';
import { httpClient, ApiError } from '../services/httpClient';
import { API_ENDPOINTS } from '../services/apiConfig';

interface ProcessDesignerProps {
  userPermissions?: string[];
}

type ProcessDefinitionSummary = {
  id: string;
  code: string;
  name: string;
  module: string;
  entityType: string;
  triggerType: string;
  triggerEventType?: string | null;
  isSystemProcess: boolean;
  isActive: boolean;
};

type ProcessStep = {
  id: string;
  stepCode: string;
  stepName: string;
  stepType: string;
  orderNo: number;
  isStartStep: boolean;
  isEndStep: boolean;
  assignmentType?: string | null;
  assignedRoleCode?: string | null;
  assignedPermissionCode?: string | null;
  assignedUserFieldPath?: string | null;
  slaHours?: number | null;
  requireMakerCheckerSeparation?: boolean;
};

type ProcessTransition = {
  id: string;
  fromStepId: string;
  toStepId: string;
  transitionName: string;
  conditionRuleCode?: string | null;
  requiredOutcome?: string | null;
  isDefault: boolean;
};

type ProcessVersion = {
  id: string;
  versionNo: number;
  status: string;
  isPublished: boolean;
  notes?: string | null;
  createdAtUtc: string;
  publishedAtUtc?: string | null;
  steps: ProcessStep[];
  transitions: ProcessTransition[];
};

type ProcessDefinitionDetail = ProcessDefinitionSummary & {
  versions: ProcessVersion[];
};

type ValidationResult = {
  isValid: boolean;
  errors: string[];
};

type DefinitionDraft = {
  code: string;
  name: string;
  module: string;
  entityType: string;
  triggerType: string;
  triggerEventType: string;
};

type StepDraft = {
  stepCode: string;
  stepName: string;
  stepType: string;
  orderNo: number;
  isStartStep: boolean;
  isEndStep: boolean;
  assignmentType: string;
  assignedRoleCode: string;
  assignedPermissionCode: string;
  assignedUserFieldPath: string;
  slaHours: string;
  requireMakerCheckerSeparation: boolean;
};

type TransitionDraft = {
  fromStepId: string;
  toStepId: string;
  transitionName: string;
  conditionRuleCode: string;
  requiredOutcome: string;
  isDefault: boolean;
};

const emptyDefinitionDraft: DefinitionDraft = {
  code: '',
  name: '',
  module: 'Operations',
  entityType: 'Transaction',
  triggerType: 'Manual',
  triggerEventType: '',
};

const emptyStepDraft: StepDraft = {
  stepCode: '',
  stepName: '',
  stepType: 'ApprovalTask',
  orderNo: 1,
  isStartStep: false,
  isEndStep: false,
  assignmentType: 'Permission',
  assignedRoleCode: '',
  assignedPermissionCode: '',
  assignedUserFieldPath: '',
  slaHours: '',
  requireMakerCheckerSeparation: false,
};

const emptyTransitionDraft: TransitionDraft = {
  fromStepId: '',
  toStepId: '',
  transitionName: '',
  conditionRuleCode: '',
  requiredOutcome: '',
  isDefault: false,
};

function getErrorMessage(error: unknown, fallback: string) {
  if (error instanceof ApiError) {
    return error.data?.message || error.data?.error || fallback;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return fallback;
}

export default function ProcessDesigner({ userPermissions = [] }: ProcessDesignerProps) {
  const [definitions, setDefinitions] = useState<ProcessDefinitionSummary[]>([]);
  const [selectedDefinitionId, setSelectedDefinitionId] = useState<string | null>(null);
  const [detail, setDetail] = useState<ProcessDefinitionDetail | null>(null);
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null);
  const [validation, setValidation] = useState<ValidationResult | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [busyAction, setBusyAction] = useState<string | null>(null);
  const [showDefinitionForm, setShowDefinitionForm] = useState(false);
  const [definitionDraft, setDefinitionDraft] = useState<DefinitionDraft>(emptyDefinitionDraft);
  const [stepDraft, setStepDraft] = useState<StepDraft>(emptyStepDraft);
  const [transitionDraft, setTransitionDraft] = useState<TransitionDraft>(emptyTransitionDraft);

  const canView = userPermissions.includes(Permissions.Processes.View);
  const canManage = userPermissions.includes(Permissions.Processes.Manage);
  const canPublish = userPermissions.includes(Permissions.Processes.Publish);

  const selectedVersion = useMemo(
    () => detail?.versions.find((version) => version.id === selectedVersionId) || detail?.versions[0] || null,
    [detail, selectedVersionId]
  );

  const stepOptions = selectedVersion?.steps || [];

  const fetchDefinitions = async (preferredDefinitionId?: string) => {
    setLoading(true);
    setError(null);
    try {
      const result = await httpClient.get<ProcessDefinitionSummary[]>(API_ENDPOINTS.workflowDefinitions.list);
      setDefinitions(result || []);
      const nextId = preferredDefinitionId || selectedDefinitionId || result?.[0]?.id || null;
      setSelectedDefinitionId(nextId);
    } catch (fetchError) {
      setError(getErrorMessage(fetchError, 'Failed to load workflow definitions.'));
    } finally {
      setLoading(false);
    }
  };

  const fetchDefinitionDetail = async (definitionId: string, preferredVersionId?: string) => {
    setBusyAction('load-detail');
    setError(null);
    try {
      const result = await httpClient.get<ProcessDefinitionDetail>(API_ENDPOINTS.workflowDefinitions.get(definitionId));
      setDetail(result);
      const nextVersionId = preferredVersionId || result.versions?.[0]?.id || null;
      setSelectedVersionId(nextVersionId);
      setValidation(null);
      setTransitionDraft((current) => ({ ...current, fromStepId: '', toStepId: '' }));
    } catch (fetchError) {
      setError(getErrorMessage(fetchError, 'Failed to load workflow definition details.'));
    } finally {
      setBusyAction(null);
    }
  };

  useEffect(() => {
    if (canView) {
      fetchDefinitions();
    }
  }, [canView]);

  useEffect(() => {
    if (selectedDefinitionId) {
      fetchDefinitionDetail(selectedDefinitionId);
    } else {
      setDetail(null);
      setSelectedVersionId(null);
    }
  }, [selectedDefinitionId]);

  const handleCreateDefinition = async () => {
    if (!definitionDraft.code.trim() || !definitionDraft.name.trim()) {
      setError('Code and name are required before creating a definition.');
      return;
    }

    setBusyAction('create-definition');
    setError(null);
    setFeedback(null);
    try {
      const created = await httpClient.post<ProcessDefinitionSummary>(API_ENDPOINTS.workflowDefinitions.create, {
        ...definitionDraft,
        triggerEventType: definitionDraft.triggerEventType || null,
      });
      setDefinitionDraft(emptyDefinitionDraft);
      setShowDefinitionForm(false);
      setFeedback(`Definition ${created.name} created.`);
      await fetchDefinitions(created.id);
    } catch (createError) {
      setError(getErrorMessage(createError, 'Failed to create workflow definition.'));
    } finally {
      setBusyAction(null);
    }
  };

  const handleCreateDraftVersion = async () => {
    if (!detail) return;
    setBusyAction('create-version');
    setError(null);
    setFeedback(null);
    try {
      const created = await httpClient.post<{ id: string }>(API_ENDPOINTS.workflowDefinitions.createVersion(detail.id));
      setFeedback(`Draft version created for ${detail.name}.`);
      await fetchDefinitionDetail(detail.id, created.id);
      await fetchDefinitions(detail.id);
    } catch (createError) {
      setError(getErrorMessage(createError, 'Failed to create a draft version.'));
    } finally {
      setBusyAction(null);
    }
  };

  const handleAddStep = async () => {
    if (!selectedVersion) return;
    setBusyAction('add-step');
    setError(null);
    setFeedback(null);
    try {
      await httpClient.post(API_ENDPOINTS.workflowDefinitions.addStep(selectedVersion.id), {
        ...stepDraft,
        assignedRoleCode: stepDraft.assignedRoleCode || null,
        assignedPermissionCode: stepDraft.assignedPermissionCode || null,
        assignedUserFieldPath: stepDraft.assignedUserFieldPath || null,
        slaHours: stepDraft.slaHours ? Number(stepDraft.slaHours) : null,
      });
      setStepDraft({ ...emptyStepDraft, orderNo: (selectedVersion.steps?.length || 0) + 1 });
      setFeedback('Step added to the selected draft version.');
      await fetchDefinitionDetail(detail!.id, selectedVersion.id);
    } catch (stepError) {
      setError(getErrorMessage(stepError, 'Failed to add process step.'));
    } finally {
      setBusyAction(null);
    }
  };

  const handleAddTransition = async () => {
    if (!selectedVersion) return;
    if (!transitionDraft.fromStepId || !transitionDraft.toStepId || !transitionDraft.transitionName.trim()) {
      setError('Transition name, source step, and destination step are required.');
      return;
    }

    setBusyAction('add-transition');
    setError(null);
    setFeedback(null);
    try {
      await httpClient.post(API_ENDPOINTS.workflowDefinitions.addTransition(selectedVersion.id), {
        ...transitionDraft,
        conditionRuleCode: transitionDraft.conditionRuleCode || null,
        requiredOutcome: transitionDraft.requiredOutcome || null,
      });
      setTransitionDraft(emptyTransitionDraft);
      setFeedback('Transition added to the selected draft version.');
      await fetchDefinitionDetail(detail!.id, selectedVersion.id);
    } catch (transitionError) {
      setError(getErrorMessage(transitionError, 'Failed to add process transition.'));
    } finally {
      setBusyAction(null);
    }
  };

  const handleValidate = async () => {
    if (!selectedVersion) return;
    setBusyAction('validate');
    setError(null);
    try {
      const result = await httpClient.get<ValidationResult>(API_ENDPOINTS.workflowDefinitions.validate(selectedVersion.id));
      setValidation(result);
      setFeedback(result.isValid ? 'Validation passed for the selected version.' : 'Validation found issues in the selected version.');
    } catch (validateError) {
      setError(getErrorMessage(validateError, 'Failed to validate process version.'));
    } finally {
      setBusyAction(null);
    }
  };

  const handlePublish = async () => {
    if (!selectedVersion) return;
    setBusyAction('publish');
    setError(null);
    setFeedback(null);
    try {
      await httpClient.post(API_ENDPOINTS.workflowDefinitions.publish(selectedVersion.id));
      setFeedback(`Version ${selectedVersion.versionNo} published successfully.`);
      await fetchDefinitionDetail(detail!.id, selectedVersion.id);
      await fetchDefinitions(detail!.id);
    } catch (publishError) {
      setError(getErrorMessage(publishError, 'Failed to publish process version.'));
    } finally {
      setBusyAction(null);
    }
  };

  if (!canView) {
    return <div className="p-6 text-sm text-slate-500 dark:text-slate-400">You do not have permission to view workflow definitions.</div>;
  }

  return (
    <div className="flex h-full flex-col gap-6 p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-2xl font-heading font-bold text-slate-950 dark:text-white">Process Designer</h2>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Create workflow definitions, shape draft versions, validate graph integrity, and publish operational process flows.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {canManage && (
            <button onClick={() => setShowDefinitionForm((current) => !current)} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
              <Plus size={16} /> New Definition
            </button>
          )}
          <button onClick={() => fetchDefinitions()} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 px-4 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800">
            <RefreshCw size={16} className={loading ? 'animate-spin' : ''} /> Refresh
          </button>
        </div>
      </div>

      {showDefinitionForm && canManage && (
        <div className="grid gap-4 rounded-[24px] border border-slate-200 bg-white p-5 shadow-soft dark:border-slate-700 dark:bg-slate-900/70 md:grid-cols-2 xl:grid-cols-3">
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Code<input value={definitionDraft.code} onChange={(event) => setDefinitionDraft((current) => ({ ...current, code: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="MOMO_CASHIER_AUTOMATION" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Name<input value={definitionDraft.name} onChange={(event) => setDefinitionDraft((current) => ({ ...current, name: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Cashier Mobile Money Workflow" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Module<input value={definitionDraft.module} onChange={(event) => setDefinitionDraft((current) => ({ ...current, module: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Entity Type<input value={definitionDraft.entityType} onChange={(event) => setDefinitionDraft((current) => ({ ...current, entityType: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Trigger Type<select value={definitionDraft.triggerType} onChange={(event) => setDefinitionDraft((current) => ({ ...current, triggerType: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"><option>Manual</option><option>Event</option><option>Api</option></select></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Trigger Event<input value={definitionDraft.triggerEventType} onChange={(event) => setDefinitionDraft((current) => ({ ...current, triggerEventType: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Optional event type" /></label>
          <div className="xl:col-span-3 flex justify-end"><button onClick={handleCreateDefinition} className="inline-flex items-center gap-2 rounded-2xl bg-brand-600 px-4 py-3 text-sm font-medium text-white hover:bg-brand-700" disabled={busyAction === 'create-definition'}><Plus size={16} /> Create Definition</button></div>
        </div>
      )}

      {error && <div className="rounded-2xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200">{error}</div>}
      {feedback && <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200">{feedback}</div>}

      <div className="grid min-h-0 flex-1 gap-6 xl:grid-cols-[320px_minmax(0,1fr)]">
        <section className="rounded-[24px] border border-slate-200 bg-white p-4 shadow-soft dark:border-slate-700 dark:bg-slate-900/70">
          <div className="mb-4 flex items-center gap-2 text-sm font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400"><Layers3 size={16} /> Definitions</div>
          <div className="space-y-3">
            {definitions.map((definition) => (
              <button key={definition.id} onClick={() => setSelectedDefinitionId(definition.id)} className={`w-full rounded-[20px] border px-4 py-4 text-left ${selectedDefinitionId === definition.id ? 'border-brand-500 bg-brand-50 dark:border-brand-400 dark:bg-brand-500/10' : 'border-slate-200 bg-slate-50 hover:bg-white dark:border-slate-700 dark:bg-slate-800/60 dark:hover:bg-slate-800'}`}>
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-semibold text-slate-950 dark:text-white">{definition.name}</div>
                    <div className="mt-1 text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">{definition.code}</div>
                  </div>
                  <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.16em] ${definition.isActive ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300' : 'bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-200'}`}>{definition.isActive ? 'Active' : 'Inactive'}</span>
                </div>
                <div className="mt-3 text-sm text-slate-500 dark:text-slate-400">{definition.module} · {definition.entityType} · {definition.triggerType}</div>
              </button>
            ))}
            {!definitions.length && !loading && <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">No workflow definitions found.</div>}
          </div>
        </section>

        <section className="rounded-[24px] border border-slate-200 bg-white p-5 shadow-soft dark:border-slate-700 dark:bg-slate-900/70">
          {!detail ? (
            <div className="flex min-h-[420px] items-center justify-center text-sm text-slate-500 dark:text-slate-400">Select a definition to manage its versions and graph.</div>
          ) : (
            <div className="space-y-6">
              <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-200 pb-4 dark:border-slate-700">
                <div>
                  <div className="text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">Workflow definition</div>
                  <h3 className="mt-2 text-2xl font-heading font-bold text-slate-950 dark:text-white">{detail.name}</h3>
                  <div className="mt-2 text-sm text-slate-500 dark:text-slate-400">{detail.code} · {detail.module} · {detail.entityType} · {detail.triggerType}</div>
                </div>
                <div className="flex flex-wrap gap-2">
                  {canManage && <button onClick={handleCreateDraftVersion} disabled={busyAction === 'create-version'} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 px-4 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"><GitBranch size={16} /> New Draft Version</button>}
                  {canManage && <button onClick={handleValidate} disabled={!selectedVersion || busyAction === 'validate'} className="inline-flex items-center gap-2 rounded-2xl border border-amber-300 bg-amber-50 px-4 py-3 text-sm font-medium text-amber-700 hover:bg-amber-100 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-200"><CheckCircle2 size={16} /> Validate</button>}
                  {canPublish && <button onClick={handlePublish} disabled={!selectedVersion || selectedVersion.isPublished || busyAction === 'publish'} className="inline-flex items-center gap-2 rounded-2xl bg-brand-600 px-4 py-3 text-sm font-medium text-white hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-60"><Rocket size={16} /> Publish</button>}
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-4">
                <InfoTile label="Versions" value={String(detail.versions.length)} />
                <InfoTile label="Current Version" value={selectedVersion ? `v${selectedVersion.versionNo}` : 'None'} />
                <InfoTile label="Steps" value={String(selectedVersion?.steps.length || 0)} />
                <InfoTile label="Transitions" value={String(selectedVersion?.transitions.length || 0)} />
              </div>

              <div className="grid gap-4 md:grid-cols-[260px_minmax(0,1fr)]">
                <div>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Version</label>
                  <select value={selectedVersionId || ''} onChange={(event) => setSelectedVersionId(event.target.value)} className="mt-1 w-full rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80">
                    {detail.versions.map((version) => <option key={version.id} value={version.id}>{`v${version.versionNo} · ${version.status}`}</option>)}
                  </select>
                </div>
                {validation && (
                  <div className={`rounded-[20px] border px-4 py-4 ${validation.isValid ? 'border-emerald-200 bg-emerald-50 dark:border-emerald-500/30 dark:bg-emerald-500/10' : 'border-amber-200 bg-amber-50 dark:border-amber-500/30 dark:bg-amber-500/10'}`}>
                    <div className="flex items-center gap-2 font-semibold text-slate-950 dark:text-white">{validation.isValid ? <CheckCircle2 size={18} className="text-emerald-600" /> : <AlertTriangle size={18} className="text-amber-600" />}{validation.isValid ? 'Validation passed' : 'Validation issues found'}</div>
                    {!validation.isValid && <ul className="mt-2 space-y-1 text-sm text-slate-600 dark:text-slate-300">{validation.errors.map((item) => <li key={item}>• {item}</li>)}</ul>}
                  </div>
                )}
              </div>

              <div className="grid gap-6 xl:grid-cols-2">
                <div className="rounded-[22px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/50">
                  <div className="flex items-center justify-between gap-3"><h4 className="text-lg font-heading font-bold text-slate-950 dark:text-white">Steps</h4><span className="text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Draft authoring</span></div>
                  <div className="mt-4 space-y-3">{selectedVersion?.steps.map((step) => <div key={step.id} className="rounded-[18px] border border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-900/70"><div className="flex items-center justify-between gap-3"><div className="font-semibold text-slate-950 dark:text-white">{step.orderNo}. {step.stepName}</div><span className="rounded-full bg-slate-100 px-2 py-1 text-[11px] uppercase tracking-[0.16em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">{step.stepType}</span></div><div className="mt-2 text-sm text-slate-500 dark:text-slate-400">{step.stepCode} · {step.assignmentType || 'No assignment'}{step.assignedPermissionCode ? ` · ${step.assignedPermissionCode}` : ''}</div></div>)}</div>
                  {canManage && selectedVersion && !selectedVersion.isPublished && <div className="mt-4 grid gap-3 md:grid-cols-2"><input value={stepDraft.stepCode} onChange={(event) => setStepDraft((current) => ({ ...current, stepCode: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Step code" /><input value={stepDraft.stepName} onChange={(event) => setStepDraft((current) => ({ ...current, stepName: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Step name" /><select value={stepDraft.stepType} onChange={(event) => setStepDraft((current) => ({ ...current, stepType: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"><option>Start</option><option>UserTask</option><option>ApprovalTask</option><option>SystemTask</option><option>Decision</option><option>Notification</option><option>End</option></select><input type="number" value={stepDraft.orderNo} onChange={(event) => setStepDraft((current) => ({ ...current, orderNo: Number(event.target.value || 0) }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Order" /><select value={stepDraft.assignmentType} onChange={(event) => setStepDraft((current) => ({ ...current, assignmentType: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"><option>Permission</option><option>Role</option><option>User</option><option>Dynamic</option><option>None</option></select><input value={stepDraft.assignedPermissionCode} onChange={(event) => setStepDraft((current) => ({ ...current, assignedPermissionCode: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Assigned permission" /><label className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"><input type="checkbox" checked={stepDraft.isStartStep} onChange={(event) => setStepDraft((current) => ({ ...current, isStartStep: event.target.checked }))} /> Start step</label><label className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"><input type="checkbox" checked={stepDraft.isEndStep} onChange={(event) => setStepDraft((current) => ({ ...current, isEndStep: event.target.checked }))} /> End step</label><div className="md:col-span-2 flex justify-end"><button onClick={handleAddStep} disabled={busyAction === 'add-step'} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"><Plus size={16} /> Add Step</button></div></div>}
                </div>

                <div className="rounded-[22px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/50">
                  <div className="flex items-center justify-between gap-3"><h4 className="text-lg font-heading font-bold text-slate-950 dark:text-white">Transitions</h4><span className="text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Route logic</span></div>
                  <div className="mt-4 space-y-3">{selectedVersion?.transitions.map((transition) => <div key={transition.id} className="rounded-[18px] border border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-900/70"><div className="font-semibold text-slate-950 dark:text-white">{transition.transitionName}</div><div className="mt-2 text-sm text-slate-500 dark:text-slate-400">{stepOptions.find((step) => step.id === transition.fromStepId)?.stepName || 'Unknown'} ? {stepOptions.find((step) => step.id === transition.toStepId)?.stepName || 'Unknown'}{transition.requiredOutcome ? ` · ${transition.requiredOutcome}` : ''}</div></div>)}</div>
                  {canManage && selectedVersion && !selectedVersion.isPublished && <div className="mt-4 grid gap-3 md:grid-cols-2"><input value={transitionDraft.transitionName} onChange={(event) => setTransitionDraft((current) => ({ ...current, transitionName: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80 md:col-span-2" placeholder="Transition name" /><select value={transitionDraft.fromStepId} onChange={(event) => setTransitionDraft((current) => ({ ...current, fromStepId: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"><option value="">From step</option>{stepOptions.map((step) => <option key={step.id} value={step.id}>{step.stepName}</option>)}</select><select value={transitionDraft.toStepId} onChange={(event) => setTransitionDraft((current) => ({ ...current, toStepId: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"><option value="">To step</option>{stepOptions.map((step) => <option key={step.id} value={step.id}>{step.stepName}</option>)}</select><input value={transitionDraft.requiredOutcome} onChange={(event) => setTransitionDraft((current) => ({ ...current, requiredOutcome: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Required outcome" /><input value={transitionDraft.conditionRuleCode} onChange={(event) => setTransitionDraft((current) => ({ ...current, conditionRuleCode: event.target.value }))} className="rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" placeholder="Condition rule code" /><label className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80 md:col-span-2"><input type="checkbox" checked={transitionDraft.isDefault} onChange={(event) => setTransitionDraft((current) => ({ ...current, isDefault: event.target.checked }))} /> Default path</label><div className="md:col-span-2 flex justify-end"><button onClick={handleAddTransition} disabled={busyAction === 'add-transition'} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"><Plus size={16} /> Add Transition</button></div></div>}
                </div>
              </div>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

function InfoTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-[20px] border border-slate-200 bg-white px-4 py-4 dark:border-slate-700 dark:bg-slate-900/70">
      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">{label}</div>
      <div className="mt-2 text-lg font-bold text-slate-950 dark:text-white">{value}</div>
    </div>
  );
}
