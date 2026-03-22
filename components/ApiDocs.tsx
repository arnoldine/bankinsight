
import React, { useState } from 'react';
import { ApiEndpoint } from '../types';
import { Server, Copy, Play, ChevronDown, ChevronRight, Globe, Database, Briefcase, Landmark, ShieldAlert, Terminal } from 'lucide-react';
import { calculateCreditScore } from '../services/creditScoringEngine';

const API_ENDPOINTS: ApiEndpoint[] = [
    // --- CUSTOMER INFORMATION FILE (CIF) ---
    {
        method: 'GET',
        path: '/api/v1/cif/{id}',
        summary: 'Get Full Customer Profile',
        parameters: [
            { name: 'id', in: 'path', type: 'string', required: true },
            { name: 'X-Staff-ID', in: 'header', type: 'string', required: true }
        ],
        response: '{"id": "CIF10001", "kycLevel": "Tier 3", "riskRating": "Low", "accounts": [...]}'
    },
    {
        method: 'POST',
        path: '/api/v1/cif/onboard',
        summary: 'Create New Customer (Staff Only)',
        parameters: [
            { name: 'firstName', in: 'body', type: 'string', required: true },
            { name: 'lastName', in: 'body', type: 'string', required: true },
            { name: 'ghanaCard', in: 'body', type: 'string', required: true },
            { name: 'digitalAddress', in: 'body', type: 'string', required: true }
        ],
        response: '{"cifId": "CIF102938", "status": "PENDING_VERIFICATION"}'
    },

    // --- ACCOUNT OPERATIONS ---
    {
        method: 'POST',
        path: '/api/v1/accounts/open',
        summary: 'Open CASA/Term Deposit',
        parameters: [
            { name: 'cif', in: 'body', type: 'string', required: true },
            { name: 'productCode', in: 'body', type: 'string', required: true }, // e.g. SA-100
            { name: 'currency', in: 'body', type: 'string', required: false }
        ],
        response: '{"accountId": "20100055501", "status": "ACTIVE"}'
    },
    {
        method: 'POST',
        path: '/api/v1/accounts/{id}/freeze',
        summary: 'Place Post No Debit (PND)',
        parameters: [
            { name: 'id', in: 'path', type: 'string', required: true },
            { name: 'reason', in: 'body', type: 'string', required: true },
            { name: 'approvedBy', in: 'body', type: 'string', required: true }
        ],
        response: '{"status": "FROZEN", "timestamp": "2023-10-27T10:00:00Z"}'
    },

    // --- TELLER & CASH (BRANCH OPS) ---
    {
        method: 'POST',
        path: '/api/v1/teller/transaction',
        summary: 'Post Cash Deposit/Withdrawal',
        parameters: [
            { name: 'X-Teller-ID', in: 'header', type: 'string', required: true },
            { name: 'accountId', in: 'body', type: 'string', required: true },
            { name: 'type', in: 'body', type: 'enum<DEPOSIT|WITHDRAWAL>', required: true },
            { name: 'amount', in: 'body', type: 'number', required: true },
            { name: 'narration', in: 'body', type: 'string', required: true }
        ],
        response: '{"txnRef": "TXN-998877", "newBalance": 1500.00, "status": "POSTED"}'
    },

    // --- LENDING (BACK OFFICE) ---
    {
        method: 'POST',
        path: '/api/v1/lending/score',
        summary: 'Internal Credit Scoring Engine',
        parameters: [
            { name: 'monthlyIncome', in: 'body', type: 'number', required: true },
            { name: 'monthlyExpense', in: 'body', type: 'number', required: true },
            { name: 'loanAmount', in: 'body', type: 'number', required: true },
            { name: 'loanTerm', in: 'body', type: 'number', required: true },
        ],
        response: '{"score": 85, "grade": "A", "recommendation": "APPROVE"}'
    },
    {
        method: 'POST',
        path: '/api/v1/lending/disburse',
        summary: 'Disburse Loan Principal',
        parameters: [
            { name: 'loanId', in: 'body', type: 'string', required: true },
            { name: 'disbursementAccount', in: 'body', type: 'string', required: true },
            { name: 'authCode', in: 'body', type: 'string', required: true }
        ],
        response: '{"status": "ACTIVE", "maturityDate": "2024-10-27"}'
    },

    // --- GENERAL LEDGER (FINANCE) ---
    {
        method: 'POST',
        path: '/api/v1/gl/journal',
        summary: 'Post Manual Journal Entry',
        parameters: [
            { name: 'description', in: 'body', type: 'string', required: true },
            { name: 'valueDate', in: 'body', type: 'date', required: true },
            { name: 'lines', in: 'body', type: 'array<{glCode, debit, credit}>', required: true }
        ],
        response: '{"jvRef": "JV-2023-001", "status": "POSTED"}'
    },

    // --- SYSTEM ADMINISTRATION ---
    {
        method: 'POST',
        path: '/api/v1/admin/eod/execute',
        summary: 'Trigger End of Day Batch',
        parameters: [
            { name: 'X-Admin-Token', in: 'header', type: 'string', required: true },
            { name: 'businessDate', in: 'body', type: 'date', required: true }
        ],
        response: '{"batchId": "BATCH-20231027", "status": "PROCESSING"}'
    }
];

const ApiDocs: React.FC = () => {
    const [openEndpoints, setOpenEndpoints] = useState<Set<string>>(new Set());
    const [testResult, setTestResult] = useState<string | null>(null);

    // Test Console State
    const [testIncome, setTestIncome] = useState(5000);
    const [testExpense, setTestExpense] = useState(2000);
    const [testAmount, setTestAmount] = useState(10000);

    const toggleEndpoint = (path: string) => {
        const next = new Set(openEndpoints);
        if (next.has(path)) next.delete(path);
        else next.add(path);
        setOpenEndpoints(next);
    };

    const handleTestScoring = () => {
        const result = calculateCreditScore(
            { 
                monthlyIncome: testIncome, 
                monthlyExpense: testExpense, 
                existingDebtService: 0, 
                totalAssets: 0, 
                totalLiabilities: 0 
            },
            testAmount,
            12,
            25,
            'Tier 3' // Default for test
        );
        setTestResult(JSON.stringify(result, null, 2));
    };

    const getMethodColor = (method: string) => {
        switch(method) {
            case 'GET': return 'bg-blue-100 text-blue-700 border-blue-200';
            case 'POST': return 'bg-green-100 text-green-700 border-green-200';
            case 'PUT': return 'bg-orange-100 text-orange-700 border-orange-200';
            case 'DELETE': return 'bg-red-100 text-red-700 border-red-200';
            default: return 'bg-gray-100 text-gray-700';
        }
    };

    const getCategoryIcon = (path: string) => {
        if (path.includes('/cif')) return <Globe size={14} className="text-purple-600"/>;
        if (path.includes('/accounts')) return <Database size={14} className="text-blue-600"/>;
        if (path.includes('/teller')) return <Landmark size={14} className="text-green-600"/>;
        if (path.includes('/lending')) return <Briefcase size={14} className="text-orange-600"/>;
        if (path.includes('/gl')) return <Briefcase size={14} className="text-gray-600"/>;
        if (path.includes('/admin')) return <ShieldAlert size={14} className="text-red-600"/>;
        return <Server size={14} className="text-gray-500"/>;
    };

    return (
        <div className="flex flex-col h-full bg-gray-50 rounded-xl overflow-hidden">
            {/* ... (Existing JSX for Header and List) ... */}
            <div className="bg-white px-6 py-4 border-b border-gray-200">
                <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
                    <Server className="text-blue-700" /> Core Banking API (Internal)
                </h1>
                <p className="text-gray-500 text-sm mt-1">
                    Internal Microservices Gateway for Staff Terminals & Back Office. Base URL: <span className="font-mono bg-gray-100 px-1 rounded">https://core.bankinsight.internal/v1</span>
                </p>
                <div className="flex gap-2 mt-2">
                    <span className="text-[10px] bg-red-50 text-red-700 px-2 py-1 rounded border border-red-100 font-bold flex items-center gap-1"><ShieldAlert size={10}/> Restricted Access</span>
                    <span className="text-[10px] bg-gray-100 text-gray-700 px-2 py-1 rounded border border-gray-200 font-bold flex items-center gap-1"><Terminal size={10}/> Intranet Only</span>
                </div>
            </div>

            <div className="flex-1 overflow-y-auto p-6">
                <div className="space-y-4">
                    {API_ENDPOINTS.map((ep, idx) => (
                        <div key={idx} className="bg-white border border-gray-200 rounded-lg overflow-hidden shadow-sm">
                            <div 
                                className="flex items-center justify-between p-4 cursor-pointer hover:bg-gray-50"
                                onClick={() => toggleEndpoint(ep.path)}
                            >
                                <div className="flex items-center gap-4">
                                    <span className={`px-3 py-1 rounded font-bold text-xs border w-16 text-center ${getMethodColor(ep.method)}`}>
                                        {ep.method}
                                    </span>
                                    <div className="flex items-center gap-2">
                                        {getCategoryIcon(ep.path)}
                                        <span className="font-mono text-sm text-gray-700">{ep.path}</span>
                                    </div>
                                    <span className="text-sm text-gray-500 hidden sm:block">- {ep.summary}</span>
                                </div>
                                {openEndpoints.has(ep.path) ? <ChevronDown size={16} className="text-gray-400"/> : <ChevronRight size={16} className="text-gray-400"/>}
                            </div>

                            {openEndpoints.has(ep.path) && (
                                <div className="p-4 border-t border-gray-100 bg-gray-50">
                                    <h4 className="text-xs font-bold text-gray-500 uppercase mb-2">Parameters</h4>
                                    <div className="bg-white border border-gray-200 rounded mb-4 overflow-hidden">
                                        <table className="w-full text-sm text-left">
                                            <thead className="bg-gray-100 text-gray-600 font-semibold text-xs uppercase">
                                                <tr>
                                                    <th className="p-2">Name</th>
                                                    <th className="p-2">In</th>
                                                    <th className="p-2">Type</th>
                                                    <th className="p-2">Required</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {ep.parameters.map((param, pIdx) => (
                                                    <tr key={pIdx} className="border-t border-gray-100">
                                                        <td className="p-2 font-mono text-blue-600">{param.name}</td>
                                                        <td className="p-2">
                                                            <span className={`text-[10px] uppercase font-bold px-1.5 py-0.5 rounded ${
                                                                param.in === 'header' ? 'bg-purple-100 text-purple-700' :
                                                                param.in === 'path' ? 'bg-orange-100 text-orange-700' : 
                                                                'bg-gray-100 text-gray-600'
                                                            }`}>{param.in}</span>
                                                        </td>
                                                        <td className="p-2 text-purple-600">{param.type}</td>
                                                        <td className="p-2 text-red-500">{param.required ? 'Yes' : 'No'}</td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>

                                    {/* Interactive Test Console for Credit Score */}
                                    {ep.path.includes('lending/score') && (
                                        <div className="bg-gray-900 rounded-lg p-4 text-gray-300 font-mono text-sm">
                                            <div className="flex justify-between items-center mb-2 border-b border-gray-700 pb-2">
                                                <span className="font-bold text-gray-900 dark:text-white flex items-center gap-2">
                                                    <Globe size={14}/> Risk Engine Console
                                                </span>
                                                <button onClick={handleTestScoring} className="bg-green-600 hover:bg-green-700 text-white px-3 py-1 rounded text-xs flex items-center gap-1">
                                                    <Play size={12}/> Execute
                                                </button>
                                            </div>
                                            <div className="grid grid-cols-3 gap-4 mb-4">
                                                <div>
                                                    <label className="block text-xs text-gray-500 mb-1">Income</label>
                                                    <input type="number" value={testIncome} onChange={e => setTestIncome(Number(e.target.value))} className="bg-gray-800 border border-gray-700 rounded p-1 w-full text-white" />
                                                </div>
                                                <div>
                                                    <label className="block text-xs text-gray-500 mb-1">Expense</label>
                                                    <input type="number" value={testExpense} onChange={e => setTestExpense(Number(e.target.value))} className="bg-gray-800 border border-gray-700 rounded p-1 w-full text-white" />
                                                </div>
                                                <div>
                                                    <label className="block text-xs text-gray-500 mb-1">Loan Amt</label>
                                                    <input type="number" value={testAmount} onChange={e => setTestAmount(Number(e.target.value))} className="bg-gray-800 border border-gray-700 rounded p-1 w-full text-white" />
                                                </div>
                                            </div>
                                            {testResult && (
                                                <div className="bg-black p-3 rounded border border-gray-700 text-green-400 whitespace-pre-wrap">
                                                    {testResult}
                                                </div>
                                            )}
                                        </div>
                                    )}

                                    <h4 className="text-xs font-bold text-gray-500 uppercase mb-2 mt-4">Example Response</h4>
                                    <div className="bg-gray-900 text-green-400 p-3 rounded font-mono text-sm flex justify-between items-start overflow-x-auto">
                                        <pre>{ep.response}</pre>
                                        <Copy size={14} className="text-gray-500 cursor-pointer hover:text-gray-900 dark:hover:text-gray-900 dark:hover:text-white" />
                                    </div>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
};

export default ApiDocs;
