import React, { useState, useMemo } from 'react';
import { GLAccount, JournalEntry } from '../types';
import { FileText, Plus, BookOpen, Search, Filter, Check, List, Layers, ChevronRight, ChevronDown, Folder, File, Save, X } from 'lucide-react';

interface AccountingEngineProps {
  accounts: GLAccount[];
  journalEntries: JournalEntry[];
  onPostJournal: (description: string, lines: { code: string; debit: number; credit: number }[]) => void;
  onCreateAccount: (account: GLAccount) => void;
  initialView?: 'COA' | 'JV' | 'LEDGER';
}

const AccountingEngine: React.FC<AccountingEngineProps> = ({ accounts, journalEntries: jvs, onPostJournal, onCreateAccount, initialView = 'COA' }) => {
  const [view, setView] = useState<'COA' | 'JV' | 'LEDGER'>(initialView);
    const [glSearch, setGlSearch] = useState('');
  const [jvLines, setJvLines] = useState([{ accountCode: '', debit: 0, credit: 0 }, { accountCode: '', debit: 0, credit: 0 }]);
  const [jvDescription, setJvDescription] = useState('');
  const [showAddGLModal, setShowAddGLModal] = useState(false);
  const [newGL, setNewGL] = useState<Partial<GLAccount>>({ category: 'ASSET', currency: 'GHS', balance: 0, isHeader: false });
  
  // COA Tree State
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set(['10000', '20000', '30000', '40000', '50000']));

  React.useEffect(() => {
    setView(initialView);
  }, [initialView]);

  const toggleNode = (code: string) => {
      const next = new Set(expandedNodes);
      if (next.has(code)) next.delete(code);
      else next.add(code);
      setExpandedNodes(next);
  };

  const postJournal = () => {
      const totalDebit = jvLines.reduce((sum, line) => sum + Number(line.debit), 0);
      const totalCredit = jvLines.reduce((sum, line) => sum + Number(line.credit), 0);

      if (totalDebit !== totalCredit) {
          alert(`Unbalanced Journal! Difference: ${totalDebit - totalCredit}`);
          return;
      }
      
      onPostJournal(
          jvDescription,
          jvLines.map(l => ({ code: l.accountCode, debit: l.debit, credit: l.credit }))
      );

      setJvLines([{ accountCode: '', debit: 0, credit: 0 }, { accountCode: '', debit: 0, credit: 0 }]);
      setJvDescription('');
      alert("Journal Posted Successfully");
  };

  const handleCreateGL = () => {
      if (!newGL.code || !newGL.name) return;
      try {
          onCreateAccount(newGL as GLAccount);
          setShowAddGLModal(false);
          setNewGL({ category: 'ASSET', currency: 'GHS', balance: 0, isHeader: false });
      } catch (e: any) {
          alert(e.message);
      }
  };

  const addLine = () => setJvLines([...jvLines, { accountCode: '', debit: 0, credit: 0 }]);

  const updateLine = (index: number, field: string, value: any) => {
      const newLines = [...jvLines];
      // @ts-ignore
      newLines[index][field] = value;
      setJvLines(newLines);
  };

  // Group Accounts for Tree View
  const treeData = useMemo(() => {
      const query = glSearch.trim().toLowerCase();
      const roots = accounts.filter(a => a.isHeader);
      return roots
        .map(root => ({
          ...root,
          children: accounts.filter(a => !a.isHeader && a.code.startsWith(root.code.charAt(0)))
            .filter(child => !query || [child.code, child.name, child.category].some(value => String(value || '').toLowerCase().includes(query)))
        }))
        .filter(root => !query || [root.code, root.name, root.category].some(value => String(value || '').toLowerCase().includes(query)) || (root.children?.length || 0) > 0);
  }, [accounts, glSearch]);
  const draftDebit = jvLines.reduce((sum, line) => sum + Number(line.debit), 0);
  const draftCredit = jvLines.reduce((sum, line) => sum + Number(line.credit), 0);
  const draftDifference = draftDebit - draftCredit;

  return (
    <div className="simple-screen relative flex h-full flex-col overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm">
        <div className="screen-hero px-6 py-5">
            <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
                <div>
                    <p className="text-xs font-semibold text-slate-500">Finance</p>
                    <h2 className="mt-2 text-2xl font-semibold text-slate-900">Chart of accounts, journal entry, and ledger review.</h2>
                </div>
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                    <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">GL Accounts</p><p className="mt-1 text-xl font-semibold text-slate-900">{accounts.length}</p></div>
                    <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">Journal Entries</p><p className="mt-1 text-xl font-semibold text-slate-900">{jvs.length}</p></div>
                    <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">Draft balance</p><p className="mt-1 text-xl font-semibold text-slate-900">{draftDifference === 0 ? 'Balanced' : draftDifference.toLocaleString(undefined, { minimumFractionDigits: 2 })}</p></div>
                </div>
            </div>
        </div>

        {/* Navigation Tabs */}
        <div className="flex border-b border-gray-200 bg-gray-50">
            <button 
                onClick={() => setView('COA')}
                className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'COA' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}
            >
                <Layers size={16} /> Chart of Accounts
            </button>
            <button 
                onClick={() => setView('JV')}
                className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'JV' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}
            >
                <Plus size={16} /> Post Journal
            </button>
            <button 
                onClick={() => setView('LEDGER')}
                className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'LEDGER' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}
            >
                <BookOpen size={16} /> General Ledger
            </button>
        </div>

        {/* Content Area */}
        <div className="flex-1 overflow-y-auto p-6 bg-gray-50/50">
            
            {/* VIEW: CHART OF ACCOUNTS (TREE) */}
            {view === 'COA' && (
                <div className="bg-white border border-gray-200 rounded-lg shadow-sm">
                    <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
                        <h3 className="font-bold text-gray-800">General Ledger Structure</h3>
                        <div className="flex gap-2">
                            <div className="relative">
                                <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                                <input type="text" value={glSearch} onChange={e => setGlSearch(e.target.value)} placeholder="Search GL code, name, or category" className="pl-9 pr-4 py-1.5 text-sm border rounded-lg focus:outline-none focus:border-blue-500" />
                            </div>
                            <button 
                                onClick={() => setShowAddGLModal(true)}
                                className="bg-blue-600 text-white px-3 py-1.5 rounded-lg text-xs font-bold hover:bg-blue-700 flex items-center gap-1"
                            >
                                <Plus size={14} /> New Account
                            </button>
                        </div>
                    </div>
                    <div className="p-4">
                        {treeData.map(root => (
                            <div key={root.code} className="mb-4">
                                <div 
                                    className="flex items-center gap-2 p-2 hover:bg-gray-50 rounded cursor-pointer select-none"
                                    onClick={() => toggleNode(root.code)}
                                >
                                    <span className="text-gray-400">
                                        {expandedNodes.has(root.code) ? <ChevronDown size={16}/> : <ChevronRight size={16}/>}
                                    </span>
                                    <Folder size={18} className="text-blue-500 fill-blue-100" />
                                    <span className="font-bold text-gray-800">{root.code} - {root.name}</span>
                                    <span className="ml-auto text-xs px-2 py-0.5 bg-gray-100 text-gray-600 rounded uppercase">{root.category}</span>
                                </div>
                                
                                {expandedNodes.has(root.code) && (
                                    <div className="ml-8 border-l-2 border-gray-100 pl-2 mt-1 space-y-1">
                                        {root.children?.map(child => (
                                            <div key={child.code} className="flex items-center justify-between p-2 hover:bg-blue-50 rounded group">
                                                <div className="flex items-center gap-3">
                                                    <File size={16} className="text-gray-400 group-hover:text-blue-500" />
                                                    <div>
                                                        <div className="text-sm font-medium text-gray-700 group-hover:text-blue-700">{child.code} - {child.name}</div>
                                                    </div>
                                                </div>
                                                <div className="font-mono text-sm text-gray-800 font-medium">
                                                    {child.balance.toLocaleString('en-US', { minimumFractionDigits: 2 })} GHS
                                                </div>
                                            </div>
                                        ))}
                                        {root.children?.length === 0 && <div className="text-xs text-gray-400 italic p-2">No sub-accounts</div>}
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* VIEW: POST JOURNAL */}
            {view === 'JV' && (
                <div className="max-w-4xl mx-auto bg-white border border-gray-200 rounded-lg shadow-sm p-6">
                    <h3 className="text-lg font-bold text-gray-800 mb-6 flex items-center gap-2">
                        <FileText className="text-blue-600" /> New Journal Entry
                    </h3>
                    
                    <div className="mb-6 grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                            <input 
                                type="text" 
                                value={jvDescription}
                                onChange={e => setJvDescription(e.target.value)}
                                className="w-full border rounded p-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none" 
                                placeholder="e.g. Monthly accruals"
                            />
                        </div>
                         <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Date</label>
                            <input type="date" className="w-full border rounded p-2 text-sm bg-gray-50" defaultValue={new Date().toISOString().split('T')[0]} />
                        </div>
                    </div>

                    <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-3">
                        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-500">Draft debit</p><p className="mt-2 text-xl font-bold text-slate-900">{draftDebit.toLocaleString(undefined, { minimumFractionDigits: 2 })}</p></div>
                        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-500">Draft credit</p><p className="mt-2 text-xl font-bold text-slate-900">{draftCredit.toLocaleString(undefined, { minimumFractionDigits: 2 })}</p></div>
                        <div className={`rounded-2xl border ${draftDifference === 0 ? 'border-emerald-200 bg-emerald-50' : 'border-rose-200 bg-rose-50'} p-4`}><p className={`text-xs uppercase tracking-[0.18em] ${draftDifference === 0 ? 'text-emerald-600' : 'text-rose-600'}`}>Draft difference</p><p className="mt-2 text-xl font-bold text-slate-900">{draftDifference.toLocaleString(undefined, { minimumFractionDigits: 2 })}</p></div>
                    </div>

                    <div className="border rounded-lg overflow-hidden mb-6">
                        <table className="w-full text-sm">
                            <thead className="bg-gray-50 text-gray-600 text-xs uppercase">
                                <tr>
                                    <th className="p-3 text-left w-1/2">Account</th>
                                    <th className="p-3 text-right">Debit</th>
                                    <th className="p-3 text-right">Credit</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-gray-100">
                                {jvLines.map((line, idx) => (
                                    <tr key={idx}>
                                        <td className="p-2">
                                            <select 
                                                className="w-full p-2 border rounded bg-white"
                                                value={line.accountCode}
                                                onChange={e => updateLine(idx, 'accountCode', e.target.value)}
                                            >
                                                <option value="">Select Account...</option>
                                                {accounts.filter(a => !a.isHeader).map(acc => (
                                                    <option key={acc.code} value={acc.code}>{acc.code} - {acc.name}</option>
                                                ))}
                                            </select>
                                        </td>
                                        <td className="p-2">
                                            <input 
                                                type="number" 
                                                value={line.debit}
                                                onChange={e => updateLine(idx, 'debit', Number(e.target.value))}
                                                className="w-full p-2 border rounded text-right font-mono focus:ring-blue-500 outline-none"
                                            />
                                        </td>
                                        <td className="p-2">
                                            <input 
                                                type="number" 
                                                value={line.credit}
                                                onChange={e => updateLine(idx, 'credit', Number(e.target.value))}
                                                className="w-full p-2 border rounded text-right font-mono focus:ring-blue-500 outline-none"
                                            />
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                            <tfoot className="bg-gray-50 font-bold text-gray-700">
                                <tr>
                                    <td className="p-3 text-right">Totals:</td>
                                    <td className="p-3 text-right font-mono">
                                        {jvLines.reduce((s, l) => s + l.debit, 0).toLocaleString(undefined, {minimumFractionDigits: 2})}
                                    </td>
                                    <td className="p-3 text-right font-mono">
                                        {jvLines.reduce((s, l) => s + l.credit, 0).toLocaleString(undefined, {minimumFractionDigits: 2})}
                                    </td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>

                    <div className="flex justify-between">
                        <button onClick={addLine} className="text-blue-600 hover:bg-blue-50 px-4 py-2 rounded text-sm font-medium flex items-center gap-1">
                            <Plus size={16} /> Add Line
                        </button>
                        <div className="flex gap-3">
                            <button className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded text-sm">Cancel</button>
                            <button onClick={postJournal} className="px-6 py-2 bg-blue-600 text-white rounded font-medium hover:bg-blue-700 text-sm shadow-sm flex items-center gap-2">
                                <Check size={16} /> Post Entry
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* VIEW: LEDGER */}
            {view === 'LEDGER' && (
                <div className="bg-white border border-gray-200 rounded-lg shadow-sm">
                     <div className="p-4 border-b border-gray-200 flex justify-between items-center">
                        <h3 className="font-bold text-gray-800">Transaction Journal</h3>
                        <div className="flex gap-2">
                            <button className="p-2 border rounded hover:bg-gray-50"><Filter size={16} className="text-gray-500"/></button>
                            <button className="p-2 border rounded hover:bg-gray-50"><List size={16} className="text-gray-500"/></button>
                        </div>
                    </div>
                    <table className="w-full text-sm text-left">
                        <thead className="bg-gray-50 text-gray-600 font-medium">
                            <tr>
                                <th className="p-3">Date</th>
                                <th className="p-3">Ref</th>
                                <th className="p-3">Description</th>
                                <th className="p-3">Account</th>
                                <th className="p-3 text-right">Debit</th>
                                <th className="p-3 text-right">Credit</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100">
                            {jvs.flatMap(jv => jv.lines.map((line, idx) => (
                                <tr key={`${jv.id}-${idx}`} className="hover:bg-gray-50">
                                    <td className="p-3 text-gray-500">{jv.date}</td>
                                    <td className="p-3 font-mono text-blue-600 text-xs">{jv.id}</td>
                                    <td className="p-3 text-gray-800">{jv.description}</td>
                                    <td className="p-3">
                                        <span className="bg-gray-100 text-gray-600 px-2 py-0.5 rounded text-xs font-mono">{line.accountCode}</span>
                                    </td>
                                    <td className="p-3 text-right font-mono text-gray-600">{line.debit > 0 ? line.debit.toLocaleString() : '-'}</td>
                                    <td className="p-3 text-right font-mono text-gray-600">{line.credit > 0 ? line.credit.toLocaleString() : '-'}</td>
                                </tr>
                            )))}
                        </tbody>
                    </table>
                </div>
            )}

            {/* ADD GL MODAL */}
            {showAddGLModal && (
                <div className="absolute inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
                    <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6">
                        <div className="flex justify-between items-center mb-4">
                            <h3 className="font-bold text-lg text-gray-800">Add GL Account</h3>
                            <button onClick={() => setShowAddGLModal(false)}><X size={20} className="text-gray-400 hover:text-gray-600"/></button>
                        </div>
                        <div className="space-y-4">
                            <div>
                                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Account Code</label>
                                <input 
                                    type="text" 
                                    className="w-full border p-2 rounded text-sm font-mono" 
                                    value={newGL.code || ''} 
                                    onChange={e => setNewGL({...newGL, code: e.target.value})}
                                    placeholder="e.g. 60100"
                                />
                            </div>
                            <div>
                                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Account Name</label>
                                <input 
                                    type="text" 
                                    className="w-full border p-2 rounded text-sm" 
                                    value={newGL.name || ''} 
                                    onChange={e => setNewGL({...newGL, name: e.target.value})}
                                    placeholder="e.g. Office Supplies"
                                />
                            </div>
                            <div>
                                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Category</label>
                                <select 
                                    className="w-full border p-2 rounded text-sm bg-white"
                                    value={newGL.category}
                                    onChange={e => setNewGL({...newGL, category: e.target.value as any})}
                                >
                                    <option value="ASSET">ASSET</option>
                                    <option value="LIABILITY">LIABILITY</option>
                                    <option value="EQUITY">EQUITY</option>
                                    <option value="INCOME">INCOME</option>
                                    <option value="EXPENSE">EXPENSE</option>
                                </select>
                            </div>
                            <div className="flex items-center gap-2 pt-2">
                                <input 
                                    type="checkbox" 
                                    id="isHeader" 
                                    checked={newGL.isHeader} 
                                    onChange={e => setNewGL({...newGL, isHeader: e.target.checked})}
                                />
                                <label htmlFor="isHeader" className="text-sm text-gray-700">Is Header Account?</label>
                            </div>
                            <button 
                                onClick={handleCreateGL}
                                className="w-full bg-blue-600 text-white font-bold py-2 rounded mt-4 hover:bg-blue-700"
                            >
                                Create Account
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    </div>
  );
};

export default AccountingEngine;



