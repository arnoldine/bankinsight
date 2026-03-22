
import React, { useState } from 'react';
import { Workflow, WorkflowStep, Role } from '../types';
import { GitBranch, Plus, Trash2, ArrowDown, Settings, Save, AlertCircle, CheckCircle, GripVertical } from 'lucide-react';

interface ProcessDesignerProps {
    workflows: Workflow[];
    roles: Role[];
    onCreateWorkflow: (workflow: Workflow) => void;
    onUpdateWorkflow: (id: string, updates: Partial<Workflow>) => void;
}

const ProcessDesigner: React.FC<ProcessDesignerProps> = ({ workflows, roles, onCreateWorkflow, onUpdateWorkflow }) => {
    const [selectedWorkflow, setSelectedWorkflow] = useState<Workflow | null>(null);
    const [isCreating, setIsCreating] = useState(false);
    
    // Editor State
    const [editForm, setEditForm] = useState<Partial<Workflow>>({
        name: '',
        description: '',
        trigger: 'LOAN_ORIGINATION',
        isActive: true,
        steps: []
    });

    const handleSelectWorkflow = (wf: Workflow) => {
        setSelectedWorkflow(wf);
        setEditForm(JSON.parse(JSON.stringify(wf))); // Deep copy
        setIsCreating(false);
    };

    const handleCreateNew = () => {
        setSelectedWorkflow(null);
        setEditForm({
            id: `WF-${Date.now()}`,
            name: 'New Business Process',
            description: '',
            trigger: 'LOAN_ORIGINATION',
            isActive: true,
            steps: [
                { id: 'S1', name: 'Start', type: 'SYSTEM_ACTION', description: 'Process Initiation', order: 1 }
            ]
        });
        setIsCreating(true);
    };

    const handleSave = () => {
        if (!editForm.name) return;
        
        if (isCreating) {
            onCreateWorkflow(editForm as Workflow);
        } else if (selectedWorkflow) {
            onUpdateWorkflow(selectedWorkflow.id, editForm);
        }
        setIsCreating(false);
        setSelectedWorkflow(null);
    };

    const addStep = () => {
        if (!editForm.steps) return;
        const newStep: WorkflowStep = {
            id: `S${Date.now()}`,
            name: 'New Step',
            type: 'APPROVAL',
            description: '',
            order: editForm.steps.length + 1
        };
        setEditForm({ ...editForm, steps: [...editForm.steps, newStep] });
    };

    const updateStep = (id: string, field: keyof WorkflowStep, value: any) => {
        if (!editForm.steps) return;
        setEditForm({
            ...editForm,
            steps: editForm.steps.map(s => s.id === id ? { ...s, [field]: value } : s)
        });
    };

    const removeStep = (id: string) => {
        if (!editForm.steps) return;
        setEditForm({
            ...editForm,
            steps: editForm.steps.filter(s => s.id !== id)
        });
    };

    return (
        <div className="flex h-full bg-white dark:bg-slate-800 rounded-xl shadow-sm border border-gray-200 dark:border-slate-700 overflow-hidden">
            {/* Sidebar List */}
            <div className="w-1/3 bg-gray-100 dark:bg-slate-900 border-r border-gray-200 dark:border-slate-700 flex flex-col">
                <div className="p-4 border-b border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 flex justify-between items-center">
                    <h3 className="font-bold text-gray-800 dark:text-slate-100 flex items-center gap-2">
                        <GitBranch className="text-purple-600" size={20}/> Processes
                    </h3>
                    <button onClick={handleCreateNew} className="p-2 bg-purple-100 text-purple-700 rounded hover:bg-purple-200">
                        <Plus size={16} />
                    </button>
                </div>
                <div className="flex-1 overflow-y-auto p-2 space-y-2">
                    {workflows.map(wf => (
                        <div 
                            key={wf.id}
                            onClick={() => handleSelectWorkflow(wf)}
                            className={`p-3 rounded-lg border cursor-pointer transition-colors ${
                                selectedWorkflow?.id === wf.id 
                                ? 'bg-purple-50 border-purple-300' 
                                : 'bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-700 hover:border-purple-200'
                            }`}
                        >
                            <div className="flex justify-between items-start">
                                <span className="font-bold text-gray-800 dark:text-slate-100 text-sm">{wf.name}</span>
                                <span className={`text-[10px] px-1.5 py-0.5 rounded font-bold ${
                                    wf.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-50 dark:bg-slate-700 text-gray-500 dark:text-slate-400'
                                }`}>{wf.isActive ? 'ACTIVE' : 'DRAFT'}</span>
                            </div>
                            <p className="text-xs text-gray-500 dark:text-slate-400 mt-1 truncate">{wf.description}</p>
                            <div className="mt-2 flex items-center gap-2 text-[10px] text-gray-400 dark:text-slate-500 font-mono">
                                <span>{wf.trigger}</span>
                                <span>•</span>
                                <span>{wf.steps.length} Steps</span>
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            {/* Editor Canvas */}
            <div className="flex-1 bg-gray-50 dark:bg-slate-700 flex flex-col">
                {(selectedWorkflow || isCreating) ? (
                    <>
                        {/* Header */}
                        <div className="p-4 bg-white dark:bg-slate-800 border-b border-gray-200 dark:border-slate-700 flex justify-between items-center">
                            <div>
                                <input 
                                    type="text" 
                                    value={editForm.name}
                                    onChange={e => setEditForm({...editForm, name: e.target.value})}
                                    className="text-lg font-bold text-gray-800 dark:text-slate-100 border-none focus:ring-0 p-0 placeholder-gray-300 w-full"
                                    placeholder="Process Name"
                                />
                                <div className="flex items-center gap-4 mt-1">
                                    <select 
                                        value={editForm.trigger}
                                        onChange={e => setEditForm({...editForm, trigger: e.target.value as any})}
                                        className="text-xs border-none bg-gray-50 dark:bg-slate-700 rounded px-2 py-1 text-gray-500 dark:text-slate-400 focus:ring-0 cursor-pointer"
                                    >
                                        <option value="LOAN_ORIGINATION">Trigger: Loan Origination</option>
                                        <option value="TRANSACTION_LIMIT">Trigger: Transaction Limit Exceeded</option>
                                        <option value="NEW_ACCOUNT">Trigger: New Account Opening</option>
                                        <option value="GL_POSTING">Trigger: Manual GL Posting</option>
                                    </select>
                                    <label className="flex items-center gap-2 text-xs cursor-pointer">
                                        <input 
                                            type="checkbox" 
                                            checked={editForm.isActive} 
                                            onChange={e => setEditForm({...editForm, isActive: e.target.checked})}
                                            className="rounded text-purple-600 focus:ring-purple-500"
                                        />
                                        <span className="text-gray-500 dark:text-slate-400 font-medium">Active Workflow</span>
                                    </label>
                                </div>
                            </div>
                            <button 
                                onClick={handleSave}
                                className="px-4 py-2 bg-purple-600 text-white rounded font-bold hover:bg-purple-700 flex items-center gap-2 shadow-sm"
                            >
                                <Save size={16} /> Save Definition
                            </button>
                        </div>

                        {/* Flow Area */}
                        <div className="flex-1 overflow-y-auto p-8 flex flex-col items-center">
                            <div className="max-w-2xl w-full space-y-4 relative">
                                {editForm.steps?.map((step, index) => (
                                    <div key={step.id} className="relative group">
                                        {/* Connector Line */}
                                        {index > 0 && (
                                            <div className="absolute -top-4 left-1/2 -translate-x-1/2 h-4 w-0.5 bg-gray-300">
                                                <div className="absolute bottom-0 left-1/2 -translate-x-1/2">
                                                    <ArrowDown size={14} className="text-gray-300 fill-gray-300" />
                                                </div>
                                            </div>
                                        )}
                                        
                                        <div className="bg-white dark:bg-slate-800 p-4 rounded-lg border border-gray-200 dark:border-slate-700 shadow-sm hover:shadow-md transition-shadow relative">
                                            <div className="flex items-start gap-3">
                                                <div className="mt-1 cursor-move text-gray-300 hover:text-gray-500 dark:text-slate-400">
                                                    <GripVertical size={20} />
                                                </div>
                                                <div className="flex-1">
                                                    <div className="flex justify-between items-start mb-2">
                                                        <input 
                                                            type="text" 
                                                            value={step.name} 
                                                            onChange={e => updateStep(step.id, 'name', e.target.value)}
                                                            className="font-bold text-gray-800 dark:text-slate-100 text-sm border-b border-transparent hover:border-gray-300 dark:border-slate-600 focus:border-purple-500 focus:outline-none w-1/2"
                                                        />
                                                        <div className="flex gap-2">
                                                            <select 
                                                                value={step.type}
                                                                onChange={e => updateStep(step.id, 'type', e.target.value)}
                                                                className="text-xs border border-gray-200 dark:border-slate-700 rounded px-2 py-1 bg-gray-100 dark:bg-slate-900 focus:outline-none focus:border-purple-500"
                                                            >
                                                                <option value="APPROVAL">Approval Step</option>
                                                                <option value="NOTIFICATION">Notification</option>
                                                                <option value="SYSTEM_ACTION">System Action</option>
                                                                <option value="DOCUMENT_UPLOAD">Document Request</option>
                                                            </select>
                                                            <button 
                                                                onClick={() => removeStep(step.id)}
                                                                className="text-gray-400 dark:text-slate-500 hover:text-red-500 p-1"
                                                            >
                                                                <Trash2 size={14} />
                                                            </button>
                                                        </div>
                                                    </div>
                                                    
                                                    <div className="grid grid-cols-2 gap-4 mt-3">
                                                        <div>
                                                            <label className="block text-[10px] font-bold text-gray-400 dark:text-slate-500 uppercase mb-1">Assigned Role</label>
                                                            <select 
                                                                value={step.requiredRoleId || ''}
                                                                onChange={e => updateStep(step.id, 'requiredRoleId', e.target.value)}
                                                                className="w-full text-xs border border-gray-200 dark:border-slate-700 rounded p-1.5 focus:border-purple-500 outline-none"
                                                                disabled={step.type === 'NOTIFICATION' || step.type === 'SYSTEM_ACTION'}
                                                            >
                                                                <option value="">-- System / Auto --</option>
                                                                {roles.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                                                            </select>
                                                        </div>
                                                        <div>
                                                            <label className="block text-[10px] font-bold text-gray-400 dark:text-slate-500 uppercase mb-1">Description / Instructions</label>
                                                            <input 
                                                                type="text" 
                                                                value={step.description}
                                                                onChange={e => updateStep(step.id, 'description', e.target.value)}
                                                                className="w-full text-xs border border-gray-200 dark:border-slate-700 rounded p-1.5 focus:border-purple-500 outline-none"
                                                                placeholder="Instructions for this step..."
                                                            />
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}

                                {/* Add Step Button */}
                                <div className="flex justify-center pt-2">
                                    <button 
                                        onClick={addStep}
                                        className="flex items-center gap-2 px-4 py-2 bg-white dark:bg-slate-800 border border-dashed border-gray-300 dark:border-slate-600 rounded-lg text-gray-500 dark:text-slate-400 hover:text-purple-600 hover:border-purple-300 hover:bg-purple-50 transition-all text-sm font-medium"
                                    >
                                        <Plus size={16} /> Add Workflow Step
                                    </button>
                                </div>
                            </div>
                        </div>
                    </>
                ) : (
                    <div className="flex-1 flex flex-col items-center justify-center text-gray-400 dark:text-slate-500">
                        <GitBranch size={48} className="mb-4 opacity-20" />
                        <p>Select a process to design or create a new one.</p>
                    </div>
                )}
            </div>
        </div>
    );
};

export default ProcessDesigner;
