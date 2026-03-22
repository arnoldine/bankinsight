
import React, { useState } from 'react';
import { UILayout, UIComponentConfig, UIComponentType } from '../types';
import { Layout, Type, Calendar, CheckSquare, AlignLeft, Grid, Save, Trash2, Plus, Move, Settings, Eye, Code, Link, GitBranch, Table, Globe } from 'lucide-react';

const TOOLBOX_ITEMS: { type: UIComponentType; icon: any; label: string }[] = [
    { type: 'TEXT_INPUT', icon: Type, label: 'Text Field' },
    { type: 'NUMBER_INPUT', icon: HashIcon, label: 'Number Field' },
    { type: 'DATE_PICKER', icon: Calendar, label: 'Date Picker' },
    { type: 'SELECT', icon: CheckSquare, label: 'Dropdown' },
    { type: 'HEADER', icon: AlignLeft, label: 'Section Header' },
    { type: 'CARD', icon: Layout, label: 'Card Container' },
    { type: 'TABLE', icon: Grid, label: 'Data Table' },
];

function HashIcon(props: any) {
    return <svg {...props} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="4" y1="9" x2="20" y2="9"/><line x1="4" y1="15" x2="20" y2="15"/><line x1="10" y1="3" x2="8" y2="21"/><line x1="16" y1="3" x2="14" y2="21"/></svg>;
}

interface NoCodeDesignerProps {
    onSaveForm?: (layout: UILayout) => void;
    // We would typically pass available tables/workflows here
}

const NoCodeDesigner: React.FC<NoCodeDesignerProps> = ({ onSaveForm }) => {
    const [layout, setLayout] = useState<UILayout>({
        id: 'new-layout',
        name: 'New Form',
        description: '',
        components: [],
        published: false
    });
    const [selectedComponentId, setSelectedComponentId] = useState<string | null>(null);
    const [previewMode, setPreviewMode] = useState(false);
    const [showConfigModal, setShowConfigModal] = useState(false);

    const addComponent = (type: UIComponentType) => {
        const newComp: UIComponentConfig = {
            id: `cmp_${Date.now()}`,
            type,
            label: `New ${type.toLowerCase().replace('_', ' ')}`,
            width: 'FULL',
            required: false,
            placeholder: ''
        };
        setLayout(prev => ({ ...prev, components: [...prev.components, newComp] }));
        setSelectedComponentId(newComp.id);
    };

    const updateComponent = (id: string, updates: Partial<UIComponentConfig>) => {
        setLayout(prev => ({
            ...prev,
            components: prev.components.map(c => c.id === id ? { ...c, ...updates } : c)
        }));
    };

    const removeComponent = (id: string) => {
        setLayout(prev => ({
            ...prev,
            components: prev.components.filter(c => c.id !== id)
        }));
        setSelectedComponentId(null);
    };

    const selectedComponent = layout.components.find(c => c.id === selectedComponentId);

    const handleSave = () => {
        if (onSaveForm) {
            onSaveForm(layout);
            alert("Form definition saved!");
        }
    };

    const handlePublish = () => {
        if (onSaveForm) {
            onSaveForm({ ...layout, published: true });
            alert("Form published to menu!");
        }
    };

    const renderPreviewComponent = (comp: UIComponentConfig) => {
        const widthClass = comp.width === 'HALF' ? 'w-1/2' : comp.width === 'THIRD' ? 'w-1/3' : 'w-full';
        
        switch (comp.type) {
            case 'HEADER':
                return <h3 className="text-lg font-bold text-gray-800 border-b pb-2 mb-4 mt-2">{comp.label}</h3>;
            case 'TEXT_INPUT':
                return (
                    <div className="mb-4">
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                            {comp.label} {comp.required && <span className="text-red-500">*</span>}
                        </label>
                        <input type="text" className="w-full border border-gray-300 rounded p-2 text-sm" placeholder={comp.placeholder} disabled={!previewMode} />
                    </div>
                );
            case 'NUMBER_INPUT':
                return (
                    <div className="mb-4">
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                            {comp.label} {comp.required && <span className="text-red-500">*</span>}
                        </label>
                        <input type="number" className="w-full border border-gray-300 rounded p-2 text-sm" placeholder={comp.placeholder} disabled={!previewMode} />
                    </div>
                );
            case 'SELECT':
                return (
                    <div className="mb-4">
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                            {comp.label} {comp.required && <span className="text-red-500">*</span>}
                        </label>
                        <select className="w-full border border-gray-300 rounded p-2 text-sm bg-white" disabled={!previewMode}>
                            <option>Select option...</option>
                            <option>Option 1</option>
                            <option>Option 2</option>
                        </select>
                    </div>
                );
            case 'DATE_PICKER':
                return (
                    <div className="mb-4">
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                            {comp.label} {comp.required && <span className="text-red-500">*</span>}
                        </label>
                        <input type="date" className="w-full border border-gray-300 rounded p-2 text-sm" disabled={!previewMode} />
                    </div>
                );
            case 'CARD':
                return (
                    <div className="border border-gray-200 rounded-lg p-4 bg-gray-50 mb-4 min-h-[100px] flex items-center justify-center text-gray-400">
                        {comp.label} Area
                    </div>
                );
            default:
                return <div className="p-2 border border-dashed mb-2">{comp.type}</div>;
        }
    };

    return (
        <div className="flex h-full bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden relative">
            {/* Left: Toolbox */}
            {!previewMode && (
                <div className="w-64 bg-gray-50 border-r border-gray-200 flex flex-col">
                    <div className="p-4 border-b border-gray-200">
                        <h3 className="font-bold text-gray-800 text-sm uppercase">Toolbox</h3>
                    </div>
                    <div className="p-4 space-y-2 overflow-y-auto flex-1">
                        {TOOLBOX_ITEMS.map((item) => (
                            <button
                                key={item.type}
                                onClick={() => addComponent(item.type)}
                                className="w-full flex items-center gap-3 p-3 bg-white border border-gray-200 rounded-lg hover:border-blue-400 hover:shadow-sm transition-all text-sm font-medium text-gray-700"
                            >
                                <item.icon size={18} className="text-blue-600" />
                                {item.label}
                            </button>
                        ))}
                    </div>
                </div>
            )}

            {/* Center: Canvas */}
            <div className="flex-1 flex flex-col bg-gray-100">
                <div className="p-4 bg-white border-b border-gray-200 flex justify-between items-center">
                    <div className="flex items-center gap-3">
                        <input 
                            type="text" 
                            value={layout.name} 
                            onChange={(e) => setLayout({...layout, name: e.target.value})}
                            className="font-bold text-lg text-gray-800 bg-transparent border-none focus:ring-0 p-0 w-64"
                        />
                        <button onClick={() => setShowConfigModal(true)} className="p-2 bg-gray-100 hover:bg-gray-200 rounded text-gray-600">
                            <Settings size={16} />
                        </button>
                    </div>
                    <div className="flex gap-2">
                        <button 
                            onClick={() => setPreviewMode(!previewMode)}
                            className={`px-3 py-1.5 rounded text-sm font-medium flex items-center gap-2 ${previewMode ? 'bg-blue-100 text-blue-700' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'}`}
                        >
                            {previewMode ? <Code size={16}/> : <Eye size={16}/>}
                            {previewMode ? 'Edit Mode' : 'Preview'}
                        </button>
                        <button onClick={handleSave} className="px-4 py-1.5 bg-blue-600 text-white rounded text-sm font-bold hover:bg-blue-700 flex items-center gap-2">
                            <Save size={16}/> Save
                        </button>
                        <button onClick={handlePublish} className="px-4 py-1.5 bg-green-600 text-white rounded text-sm font-bold hover:bg-green-700 flex items-center gap-2">
                            <Globe size={16}/> Publish
                        </button>
                    </div>
                </div>

                <div className="flex-1 p-8 overflow-y-auto">
                    <div className={`max-w-3xl mx-auto bg-white min-h-[600px] shadow-lg rounded-xl p-8 transition-all ${previewMode ? '' : 'border-2 border-dashed border-gray-300'}`}>
                        {layout.components.length === 0 && !previewMode && (
                            <div className="h-full flex flex-col items-center justify-center text-gray-400 mt-20">
                                <Plus size={48} className="mb-4 opacity-20" />
                                <p>Click items from the toolbox to add them here.</p>
                            </div>
                        )}
                        
                        <div className="grid grid-cols-12 gap-4">
                            {layout.components.map((comp) => (
                                <div 
                                    key={comp.id}
                                    onClick={() => !previewMode && setSelectedComponentId(comp.id)}
                                    className={`
                                        ${comp.width === 'FULL' ? 'col-span-12' : comp.width === 'HALF' ? 'col-span-6' : 'col-span-4'}
                                        ${selectedComponentId === comp.id && !previewMode ? 'ring-2 ring-blue-500 rounded p-1 relative group' : 'p-1'}
                                        transition-all
                                    `}
                                >
                                    {renderPreviewComponent(comp)}
                                    
                                    {selectedComponentId === comp.id && !previewMode && (
                                        <div className="absolute -top-3 -right-3 flex gap-1 bg-white shadow rounded border border-gray-200 p-1">
                                            <button onClick={(e) => { e.stopPropagation(); removeComponent(comp.id); }} className="p-1 text-red-500 hover:bg-red-50 rounded"><Trash2 size={14}/></button>
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>

            {/* Right: Properties */}
            {selectedComponentId && !previewMode && selectedComponent && (
                <div className="w-72 bg-white border-l border-gray-200 flex flex-col">
                    <div className="p-4 border-b border-gray-200 bg-gray-50 flex items-center gap-2">
                        <Settings size={16} className="text-gray-500"/>
                        <h3 className="font-bold text-gray-800 text-sm uppercase">Properties</h3>
                    </div>
                    <div className="p-4 space-y-4 overflow-y-auto">
                        <div>
                            <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Field Label</label>
                            <input 
                                type="text" 
                                className="w-full border border-gray-300 rounded p-2 text-sm"
                                value={selectedComponent.label}
                                onChange={(e) => updateComponent(selectedComponent.id, { label: e.target.value })}
                            />
                        </div>
                        
                        {['TEXT_INPUT', 'NUMBER_INPUT', 'DATE_PICKER', 'SELECT'].includes(selectedComponent.type) && (
                            <div>
                                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Database Field Binding</label>
                                <input 
                                    type="text" 
                                    className="w-full border border-gray-300 rounded p-2 text-sm font-mono text-blue-600 bg-blue-50"
                                    value={selectedComponent.variableName || ''}
                                    onChange={(e) => updateComponent(selectedComponent.id, { variableName: e.target.value })}
                                    placeholder="e.g. customer_name"
                                />
                            </div>
                        )}

                        <div>
                            <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Width</label>
                            <div className="flex gap-2">
                                {['FULL', 'HALF', 'THIRD'].map((w) => (
                                    <button
                                        key={w}
                                        onClick={() => updateComponent(selectedComponent.id, { width: w as any })}
                                        className={`flex-1 py-1 text-xs border rounded ${selectedComponent.width === w ? 'bg-blue-100 border-blue-300 text-blue-700 font-bold' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
                                    >
                                        {w}
                                    </button>
                                ))}
                            </div>
                        </div>

                        {['TEXT_INPUT', 'NUMBER_INPUT'].includes(selectedComponent.type) && (
                            <div>
                                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Placeholder</label>
                                <input 
                                    type="text" 
                                    className="w-full border border-gray-300 rounded p-2 text-sm"
                                    value={selectedComponent.placeholder || ''}
                                    onChange={(e) => updateComponent(selectedComponent.id, { placeholder: e.target.value })}
                                />
                            </div>
                        )}

                        <div className="flex items-center gap-2 pt-2">
                            <input 
                                type="checkbox" 
                                id="chkReq"
                                checked={selectedComponent.required || false}
                                onChange={(e) => updateComponent(selectedComponent.id, { required: e.target.checked })}
                                className="rounded text-blue-600 focus:ring-blue-500"
                            />
                            <label htmlFor="chkReq" className="text-sm text-gray-700">Required Field</label>
                        </div>
                    </div>
                </div>
            )}

            {/* Form Settings Modal */}
            {showConfigModal && (
                <div className="absolute inset-0 bg-black/50 z-50 flex items-center justify-center backdrop-blur-sm p-4">
                    <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
                        <div className="flex justify-between items-center mb-4">
                            <h3 className="font-bold text-lg text-gray-800">Form Configuration</h3>
                            <button onClick={() => setShowConfigModal(false)} className="text-gray-400 hover:text-gray-600"><Trash2 size={18}/></button> 
                        </div>
                        
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center gap-2"><Table size={14} className="text-blue-600"/> Linked Database Table</label>
                                <select 
                                    className="w-full border p-2 rounded text-sm bg-white"
                                    value={layout.linkedTable || ''}
                                    onChange={e => setLayout({ ...layout, linkedTable: e.target.value })}
                                >
                                    <option value="">-- No Link --</option>
                                    <option value="m_client">m_client</option>
                                    <option value="m_loan">m_loan</option>
                                    <option value="m_savings_account">m_savings_account</option>
                                </select>
                                <p className="text-xs text-gray-500 mt-1">Bind fields to columns in this table.</p>
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center gap-2"><GitBranch size={14} className="text-purple-600"/> Linked Workflow</label>
                                <select 
                                    className="w-full border p-2 rounded text-sm bg-white"
                                    value={layout.linkedWorkflow || ''}
                                    onChange={e => setLayout({ ...layout, linkedWorkflow: e.target.value })}
                                >
                                    <option value="">-- No Workflow --</option>
                                    <option value="WF-001">High Value Loan Approval</option>
                                    <option value="WF-002">Large Withdrawal Override</option>
                                </select>
                                <p className="text-xs text-gray-500 mt-1">Start this process when form is submitted.</p>
                            </div>

                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-1">Menu Label</label>
                                    <input 
                                        type="text" 
                                        className="w-full border p-2 rounded text-sm"
                                        value={layout.menuLabel || ''}
                                        onChange={e => setLayout({ ...layout, menuLabel: e.target.value })}
                                        placeholder="e.g. Survey"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-1">Menu Icon</label>
                                    <input 
                                        type="text" 
                                        className="w-full border p-2 rounded text-sm"
                                        value={layout.menuIcon || ''}
                                        onChange={e => setLayout({ ...layout, menuIcon: e.target.value })}
                                        placeholder="e.g. FileText"
                                    />
                                </div>
                            </div>
                        </div>

                        <div className="mt-6 flex justify-end">
                            <button onClick={() => setShowConfigModal(false)} className="px-4 py-2 bg-blue-600 text-white rounded font-bold hover:bg-blue-700">Done</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default NoCodeDesigner;
