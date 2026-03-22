
import React, { useState } from 'react';
import { Play, Save, Terminal, Database, Loader2, ChevronDown, Sparkles, X, Globe } from 'lucide-react';
import { runSQLCode, generateSQLCode } from '../services/geminiService';
import { StoredProcedureResult } from '../types';

interface SQLEditorProps {
    data: any;
    onExecute?: (name: string, args: any[]) => StoredProcedureResult;
}

const SCRIPTS: Record<string, string> = {
    'SP_CALC_LOAN_INTEREST': `CREATE PROCEDURE SP_CALC_LOAN_INTEREST(
    IN p_principal DECIMAL(15,2),
    IN p_rate DECIMAL(5,2),
    IN p_term INT
)
BEGIN
    -- Description: Calculate Monthly Installment (MySQL Syntax)
    DECLARE v_monthly_rate DECIMAL(10,6);
    DECLARE v_monthly_payment DECIMAL(15,2);
    
    SET v_monthly_rate = (p_rate / 100) / 12;
    
    -- Amortization Formula: P * r * (1+r)^n / ((1+r)^n - 1)
    SET v_monthly_payment = p_principal * v_monthly_rate * POW(1 + v_monthly_rate, p_term) / 
                           (POW(1 + v_monthly_rate, p_term) - 1);
                           
    SELECT 
        p_principal AS Principal,
        ROUND(v_monthly_payment, 2) AS MonthlyPayment,
        ROUND((v_monthly_payment * p_term) - p_principal, 2) AS TotalInterest;
END;`,

    'SP_POST_TRANSACTION': `CREATE PROCEDURE SP_POST_TRANSACTION(
    IN p_account_id VARCHAR(20),
    IN p_type VARCHAR(20),
    IN p_amount DECIMAL(15,2),
    IN p_teller_id VARCHAR(20)
)
BEGIN
    DECLARE v_current_balance DECIMAL(15,2);
    
    START TRANSACTION;
    
    -- Lock Row for Update
    SELECT balance INTO v_current_balance 
    FROM accounts 
    WHERE id = p_account_id 
    FOR UPDATE;
    
    IF v_current_balance IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Account not found';
    END IF;
    
    -- Update Balance
    IF p_type = 'DEPOSIT' THEN
        UPDATE accounts SET balance = balance + p_amount WHERE id = p_account_id;
    ELSEIF p_type = 'WITHDRAWAL' THEN
        IF v_current_balance < p_amount THEN
             SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Insufficient Funds';
        END IF;
        UPDATE accounts SET balance = balance - p_amount WHERE id = p_account_id;
    END IF;
    
    -- Insert Log
    INSERT INTO transactions (account_id, type, amount, teller_id, created_at)
    VALUES (p_account_id, p_type, p_amount, p_teller_id, NOW());
    
    COMMIT;
    
    SELECT 'SUCCESS' as status, LAST_INSERT_ID() as txn_ref;
END;`,

    'SP_EOD_BATCH': `CREATE PROCEDURE SP_EOD_BATCH(IN p_date DATE)
BEGIN
    -- 1. Accrue Interest
    UPDATE accounts 
    SET accrued_interest = accrued_interest + (balance * (interest_rate / 36500))
    WHERE type = 'SAVINGS' AND status = 'ACTIVE';
    
    -- 2. Update Loan Buckets
    UPDATE loans
    SET par_bucket = CASE 
        WHEN DATEDIFF(NOW(), last_payment_date) > 90 THEN '91+'
        WHEN DATEDIFF(NOW(), last_payment_date) > 30 THEN '31-90'
        ELSE '0'
    END
    WHERE status = 'ACTIVE';
    
    SELECT 'BATCH_COMPLETE' as msg;
END;`
};

const TEMPLATES = [
    'Stored Procedure (CRUD)',
    'Trigger (Audit)',
    'Complex Join Report',
    'Schema Migration'
];

const SQLEditor: React.FC<SQLEditorProps> = ({ data, onExecute }) => {
    const [selectedScript, setSelectedScript] = useState('SP_POST_TRANSACTION');
    const [code, setCode] = useState(SCRIPTS['SP_POST_TRANSACTION']);
    const [output, setOutput] = useState('');
    const [isRunning, setIsRunning] = useState(false);
    
    // AI Generation State
    const [showGenModal, setShowGenModal] = useState(false);
    const [genPrompt, setGenPrompt] = useState('');
    const [genTemplate, setGenTemplate] = useState('Stored Procedure (CRUD)');
    const [isGenerating, setIsGenerating] = useState(false);

    const handleScriptChange = (scriptName: string) => {
        setSelectedScript(scriptName);
        if (SCRIPTS[scriptName]) {
            setCode(SCRIPTS[scriptName]);
        }
        setOutput('');
    };

    const handleRun = async () => {
        setIsRunning(true);
        setOutput(`> Executing on MySQL 8.0 instance...\n> Script: ${selectedScript}`);

        // Mapping SQL names to internal simulation functions
        const procMap: Record<string, string> = {
            'SP_POST_TRANSACTION': 'SP_POST_TRANSACTION',
            'SP_CALC_LOAN_INTEREST': 'SP_CALC_LOAN_INTEREST',
            'SP_EOD_BATCH': 'SP_EOD_BATCH'
        };

        const internalName = procMap[selectedScript] || selectedScript;

        if (onExecute && ['SP_POST_TRANSACTION', 'SP_CREATE_ACCOUNT', 'SP_EOD_BATCH'].includes(internalName)) {
            setTimeout(() => {
                let args: any[] = [];
                let userCancelled = false;

                if (internalName === 'SP_POST_TRANSACTION') {
                     const acct = prompt("SQL Parameter @p_account_id:", "2010000001");
                     const amt = prompt("SQL Parameter @p_amount:", "500");
                     if (acct && amt) args = [acct, 'DEPOSIT', amt, 'SQL Console', 'DBA'];
                     else userCancelled = true;
                } else if (internalName === 'SP_EOD_BATCH') {
                     args = [new Date().toISOString().split('T')[0]];
                }

                if (!userCancelled) {
                    const result = onExecute(internalName, args);
                    const sqlRes = JSON.stringify({
                        affectedRows: result.success ? 1 : 0,
                        serverStatus: 2,
                        warningCount: 0,
                        message: result.output,
                        resultSet: result.data ? [result.data] : []
                    }, null, 2);
                    
                    setOutput(prev => prev + `\n\n${sqlRes}`);
                } else {
                    setOutput(prev => prev + "\n> Query Cancelled.");
                }
                setIsRunning(false);
            }, 800);
            return;
        }

        // Fallback to AI execution for custom scripts
        setTimeout(async () => {
             const result = await runSQLCode(code, data);
             setOutput(prev => prev + "\n\n" + result);
             setIsRunning(false);
        }, 1200);
    };

    const handleAiGenerate = async () => {
        if (!genPrompt) return;
        setIsGenerating(true);
        const generatedCode = await generateSQLCode(genPrompt, genTemplate);
        setCode(generatedCode);
        setSelectedScript(`GEN_SQL_${Date.now().toString().slice(-4)}`);
        setIsGenerating(false);
        setShowGenModal(false);
        setGenPrompt('');
    };

    return (
        <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden relative">
            {/* Toolbar */}
            <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 bg-gray-50">
                <div className="flex items-center gap-3">
                    <div className="bg-blue-100 p-1.5 rounded text-blue-700">
                        <Database size={18} />
                    </div>
                    <div>
                        <div className="relative group">
                            <button className="font-semibold text-gray-700 text-sm flex items-center gap-1 hover:text-blue-600 focus:outline-none">
                                {selectedScript} <ChevronDown size={14} />
                            </button>
                            {/* Dropdown Menu */}
                            <div className="absolute top-full left-0 mt-1 w-64 bg-white border border-gray-200 rounded shadow-lg hidden group-hover:block z-50">
                                {Object.keys(SCRIPTS).map(script => (
                                    <button 
                                        key={script}
                                        onClick={() => handleScriptChange(script)}
                                        className={`w-full text-left px-4 py-2 text-xs hover:bg-gray-50 ${selectedScript === script ? 'text-blue-600 font-bold' : 'text-gray-700'}`}
                                    >
                                        {script}
                                    </button>
                                ))}
                            </div>
                        </div>
                        <span className="text-xs text-gray-500 block">MySQL Workbench • 8.0</span>
                    </div>
                </div>
                <div className="flex gap-2">
                     <button 
                        onClick={() => setShowGenModal(true)}
                        className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-purple-700 bg-purple-50 border border-purple-200 rounded hover:bg-purple-100 transition-colors mr-2"
                     >
                        <Sparkles size={14} /> AI Assist
                     </button>
                     <button className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-gray-600 bg-white border border-gray-300 rounded hover:bg-gray-50 hover:text-blue-700 transition-colors">
                        <Save size={14} /> Save
                     </button>
                     <button 
                        onClick={handleRun}
                        disabled={isRunning}
                        className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-white bg-green-600 rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm transition-colors"
                     >
                        {isRunning ? <Loader2 size={14} className="animate-spin"/> : <Play size={14} />} 
                        {isRunning ? 'Running...' : 'Execute'}
                     </button>
                </div>
            </div>

            {/* Editor Area */}
            <div className="flex-1 flex flex-col lg:flex-row min-h-0">
                <div className="flex-1 bg-[#1e1e1e] relative overflow-hidden flex flex-col">
                    <div className="absolute top-0 left-0 bottom-0 w-10 bg-[#252526] text-gray-500 text-xs font-mono pt-4 text-right pr-2 select-none border-r border-[#333]">
                        {code.split('\n').map((_, i) => (
                            <div key={i} className="leading-6">{i + 1}</div>
                        ))}
                    </div>
                    <textarea 
                        value={code}
                        onChange={(e) => setCode(e.target.value)}
                        className="w-full h-full bg-transparent text-gray-300 font-mono text-sm resize-none focus:outline-none p-4 pl-12 leading-6"
                        spellCheck={false}
                        autoCapitalize="off"
                    />
                </div>
                
                {/* Output Console */}
                <div className="h-48 lg:h-auto lg:w-96 bg-[#0f0f0f] border-t lg:border-t-0 lg:border-l border-gray-700 flex flex-col">
                    <div className="px-4 py-2 bg-[#252526] text-gray-400 text-xs font-mono border-b border-gray-700 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                             <Terminal size={12} /> QUERY RESULT
                        </div>
                        <button onClick={() => setOutput('')} className="hover:text-gray-900 dark:hover:text-gray-900 dark:hover:text-white">Clear</button>
                    </div>
                    <div className="p-4 font-mono text-sm text-green-400 whitespace-pre-wrap overflow-auto flex-1 font-light leading-relaxed">
                        {output || <span className="text-gray-600 opacity-50">{"> Waiting for query..."}</span>}
                    </div>
                </div>
            </div>

            {/* AI Generator Modal */}
            {showGenModal && (
                <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center backdrop-blur-sm">
                    <div className="bg-white rounded-xl shadow-2xl p-6 w-[500px] border border-gray-200">
                        <div className="flex justify-between items-center mb-4">
                            <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2">
                                <Sparkles className="text-purple-600" size={20} />
                                Generate SQL Script
                            </h3>
                            <button onClick={() => setShowGenModal(false)} className="text-gray-400 hover:text-gray-600">
                                <X size={20} />
                            </button>
                        </div>
                        
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">SQL Template</label>
                                <select 
                                    value={genTemplate}
                                    onChange={(e) => setGenTemplate(e.target.value)}
                                    className="w-full p-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-purple-500 outline-none"
                                >
                                    {TEMPLATES.map(t => <option key={t} value={t}>{t}</option>)}
                                </select>
                            </div>
                            
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Requirements</label>
                                <textarea 
                                    value={genPrompt}
                                    onChange={(e) => setGenPrompt(e.target.value)}
                                    placeholder="e.g., 'Create a procedure to transfer funds between two accounts securely using transactions'..."
                                    className="w-full p-3 border border-gray-300 rounded-lg text-sm h-32 resize-none focus:ring-2 focus:ring-purple-500 outline-none"
                                />
                            </div>
                            
                            <button 
                                onClick={handleAiGenerate}
                                disabled={isGenerating || !genPrompt}
                                className="w-full py-2.5 bg-purple-600 text-white rounded-lg font-bold hover:bg-purple-700 disabled:opacity-50 flex items-center justify-center gap-2"
                            >
                                {isGenerating ? <Loader2 className="animate-spin" size={18} /> : <Sparkles size={18} />}
                                {isGenerating ? 'Generating SQL...' : 'Generate Script'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default SQLEditor;
