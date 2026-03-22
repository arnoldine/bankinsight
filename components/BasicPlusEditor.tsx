import React, { useState, useEffect } from 'react';
import { Play, Save, Terminal, FileCode, Loader2, ChevronDown, Sparkles, X, Database, Globe } from 'lucide-react';
import { runBasicPlusCode, generateBasicPlusCode } from '../services/geminiService';
import { StoredProcedureResult } from '../types';

interface BasicPlusEditorProps {
    data: any;
    onExecute?: (name: string, args: any[]) => StoredProcedureResult;
}

const SCRIPTS: Record<string, string> = {
    'O4W_CALC_LOAN_INTEREST': `Compile Function O4W_CALC_LOAN_INTEREST(Request)
*-------------------------------------------------------------------------
* Description: O4W Endpoint for Loan Simulation
* Input: JSON String via 'Request'
* Output: JSON String
*-------------------------------------------------------------------------

   $Insert O4W_Equates
   
   * Parse Incoming JSON
   JsonReq = Request<1>
   Principal = JsonReq{"principal"}
   AnnualRate = JsonReq{"rate"}
   TermMonths = JsonReq{"term"}

   If Unassigned(Principal) Then Principal = 10000
   If Unassigned(AnnualRate) Then AnnualRate = 28.5
   If Unassigned(TermMonths) Then TermMonths = 12

   MonthlyRate = (AnnualRate / 100) / 12
   
   * Amortization Formula
   Top = Principal * MonthlyRate * Power((1 + MonthlyRate), TermMonths)
   Bot = Power((1 + MonthlyRate), TermMonths) - 1
   
   MonthlyPayment = Top / Bot
   TotalPayment   = MonthlyPayment * TermMonths
   TotalInterest  = TotalPayment - Principal

   * Construct JSON Response
   Response  = '{'
   Response := '  "status": "OK",'
   Response := '  "data": {'
   Response := '    "principal": ' : Principal : ','
   Response := '    "monthly_payment": "' : Oconv(MonthlyPayment, "MD2,") : '",'
   Response := '    "total_interest": "' : Oconv(TotalInterest, "MD2,") : '",'
   Response := '    "apr": "' : AnnualRate : '%"'
   Response := '  }'
   Response := '}'

Return Response`,

    'O4W_POST_TRANSACTION': `Compile Function O4W_POST_TRANSACTION(Request)
*-------------------------------------------------------------------------
* Description: O4W Endpoint for Teller Posting
* Validates Token, Checks Limits, Posts to GL
*-------------------------------------------------------------------------

   * Simulate Parsing
   AccountId = Request{"accountId"}
   TxType    = Request{"type"}
   Amount    = Request{"amount"}
   TellerId  = Request{"tellerId"}

   Open "ACCOUNTS" To ACCOUNTS_FILE Else 
      Return '{"status": "ERROR", "message": "DB_OFFLINE"}'
   End

   Lock ACCOUNTS_FILE, AccountId Else 
      Return '{"status": "ERROR", "message": "RECORD_LOCKED"}'
   End
   
   Read Rec From ACCOUNTS_FILE, AccountId Else 
      Unlock ACCOUNTS_FILE, AccountId
      Return '{"status": "ERROR", "message": "INVALID_ACCOUNT"}'
   End
   
   * ... [Core Logic Removed for Brevity] ...
   
   * Construct Success Response
   Json = '{'
   Json := ' "status": "SUCCESS",'
   Json := ' "txn_ref": "' : TxId : '",'
   Json := ' "new_balance": "' : Oconv(NewBal, "MD2,") : '"'
   Json := '}'
   
   Return Json`,

    'SP_EOD_BATCH': `Compile Subroutine SP_EOD_BATCH(ProcessDate)
*-------------------------------------------------------------------------
* Description: End of Day Batch Processing (Internal Subroutine)
* Not directly exposed via O4W (called by O4W_RUN_EOD)
*-------------------------------------------------------------------------

   If Unassigned(ProcessDate) Then ProcessDate = Date()
   
   * --- 1. Interest Accrual (Savings) ---
   Select "ACCOUNTS" With TYPE = "SAVINGS" And STATUS = "ACTIVE"
   Done = 0
   AccrualTotal = 0
   
   Loop
      ReadNext ID Else Done = 1
   Until Done
      Read Rec From ACCOUNTS_FILE, ID Then
         Balance = Rec<4>
         Rate = 0.05 ;* 5% p.a.
         DailyInterest = Balance * (Rate / 365)
         
         * Accumulate in internal accrual bucket (Field 8)
         Rec<8> = Rec<8> + DailyInterest
         AccrualTotal += DailyInterest
         
         Write Rec On ACCOUNTS_FILE, ID
      End
   Repeat
   
   * Post GL for Total Accrual
   Call SP_POST_GL_JOURNAL("EOD Accrual", "50100", AccrualTotal, "20500", AccrualTotal)

   Return "BATCH_COMPLETED_SUCCESS"`,

   'O4W_AUTH_USER': `Compile Function O4W_AUTH_USER(Request)
*-------------------------------------------------------------------------
* Description: O4W Auth Endpoint
* Returns JWT Token if credentials match
*-------------------------------------------------------------------------

   User = Request{"username"}
   Pass = Request{"password"}
   
   Open "SECURITY_USERS" To USERS_FILE Else Return '{"error": "DB"}'
   
   * ... Verify Hash ...
   
   If Valid Then
       Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
       Return '{"status": "OK", "token": "' : Token : '"}'
   End Else
       Return '{"status": "DENIED", "message": "Invalid Credentials"}'
   End`
};

const TEMPLATES = [
    'O4W JSON Endpoint',
    'Internal Subroutine',
    'Dict.MFS Hook',
    'BoG Report Logic'
];

const BasicPlusEditor: React.FC<BasicPlusEditorProps> = ({ data, onExecute }) => {
    const [selectedScript, setSelectedScript] = useState('O4W_POST_TRANSACTION');
    const [code, setCode] = useState(SCRIPTS['O4W_POST_TRANSACTION']);
    const [output, setOutput] = useState('');
    const [isRunning, setIsRunning] = useState(false);
    
    // AI Generation State
    const [showGenModal, setShowGenModal] = useState(false);
    const [genPrompt, setGenPrompt] = useState('');
    const [genTemplate, setGenTemplate] = useState('O4W JSON Endpoint');
    const [isGenerating, setIsGenerating] = useState(false);

    // Update code when script selection changes
    const handleScriptChange = (scriptName: string) => {
        setSelectedScript(scriptName);
        if (SCRIPTS[scriptName]) {
            setCode(SCRIPTS[scriptName]);
        }
        setOutput('');
    };

    const handleRun = async () => {
        setIsRunning(true);
        setOutput(`> POST /api/o4w/${selectedScript}\n> Content-Type: application/json\n> Waiting for OEngine...`);

        // WIRING: Check if this is a known system procedure and we have a dispatcher
        // Mapping O4W names to internal simulation functions
        const procMap: Record<string, string> = {
            'O4W_POST_TRANSACTION': 'SP_POST_TRANSACTION',
            'O4W_CALC_LOAN_INTEREST': 'SP_CALC_LOAN_INTEREST',
            'SP_EOD_BATCH': 'SP_EOD_BATCH'
        };

        const internalName = procMap[selectedScript] || selectedScript;

        if (onExecute && ['SP_POST_TRANSACTION', 'SP_CREATE_ACCOUNT', 'SP_EOD_BATCH'].includes(internalName)) {
            setTimeout(() => {
                let args: any[] = [];
                let userCancelled = false;

                // Simulate O4W Request Body construction via Prompt
                if (internalName === 'SP_POST_TRANSACTION') {
                     const acct = prompt("Simulate JSON Payload - Account ID:", "2010000001");
                     const amt = prompt("Simulate JSON Payload - Amount:", "500");
                     if (acct && amt) args = [acct, 'DEPOSIT', amt, 'O4W Web Deposit', 'API_USER'];
                     else userCancelled = true;
                } else if (internalName === 'SP_EOD_BATCH') {
                     args = [new Date().toISOString().split('T')[0]];
                }

                if (!userCancelled) {
                    const result = onExecute(internalName, args);
                    // Simulate O4W Wrapping the result in JSON
                    const o4wJson = JSON.stringify({
                        status: result.success ? "200 OK" : "500 ERROR",
                        server: "PostgreSQL 10.2 OEngine",
                        payload: result.data || result.output
                    }, null, 2);
                    
                    setOutput(prev => prev + `\n\n< HTTP/1.1 200 OK\n< Content-Type: application/json\n\n${o4wJson}`);
                } else {
                    setOutput(prev => prev + "\n> Request Aborted.");
                }
                setIsRunning(false);
            }, 800);
            return;
        }

        // Fallback to AI execution for custom scripts
        setTimeout(async () => {
             // Pass data context (accounts) to the AI runner so it can simulate "Read Rec"
             const result = await runBasicPlusCode(code, data);
             setOutput(prev => prev + "\n\n" + result);
             setIsRunning(false);
        }, 1200);
    };

    const handleAiGenerate = async () => {
        if (!genPrompt) return;
        setIsGenerating(true);
        const generatedCode = await generateBasicPlusCode(genPrompt, genTemplate);
        setCode(generatedCode);
        setSelectedScript(`O4W_GEN_${Date.now().toString().slice(-4)}`);
        setIsGenerating(false);
        setShowGenModal(false);
        setGenPrompt('');
    };

    return (
        <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden relative">
            {/* Toolbar */}
            <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 bg-gray-50">
                <div className="flex items-center gap-3">
                    <div className="bg-orange-100 p-1.5 rounded text-orange-700">
                        <Globe size={18} />
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
                        <span className="text-xs text-gray-500 block">O4W Endpoint • PL/pgSQL</span>
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
                        {isRunning ? 'Sending...' : 'Test API'}
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
                             <Terminal size={12} /> HTTP RESPONSE
                        </div>
                        <button onClick={() => setOutput('')} className="hover:text-gray-900 dark:hover:text-gray-900 dark:hover:text-white">Clear</button>
                    </div>
                    <div className="p-4 font-mono text-sm text-green-400 whitespace-pre-wrap overflow-auto flex-1 font-light leading-relaxed">
                        {output || <span className="text-gray-600 opacity-50">{"> Waiting for request..."}</span>}
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
                                Generate PL/pgSQL O4W Module
                            </h3>
                            <button onClick={() => setShowGenModal(false)} className="text-gray-400 hover:text-gray-600">
                                <X size={20} />
                            </button>
                        </div>
                        
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Module Template</label>
                                <select 
                                    value={genTemplate}
                                    onChange={(e) => setGenTemplate(e.target.value)}
                                    className="w-full p-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-purple-500 outline-none"
                                >
                                    {TEMPLATES.map(t => <option key={t} value={t}>{t}</option>)}
                                </select>
                            </div>
                            
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Description / Requirements</label>
                                <textarea 
                                    value={genPrompt}
                                    onChange={(e) => setGenPrompt(e.target.value)}
                                    placeholder="Describe the API functionality (e.g., 'API to validate account balance and return user details as JSON')..."
                                    className="w-full p-3 border border-gray-300 rounded-lg text-sm h-32 resize-none focus:ring-2 focus:ring-purple-500 outline-none"
                                />
                            </div>
                            
                            <button 
                                onClick={handleAiGenerate}
                                disabled={isGenerating || !genPrompt}
                                className="w-full py-2.5 bg-purple-600 text-white rounded-lg font-bold hover:bg-purple-700 disabled:opacity-50 flex items-center justify-center gap-2"
                            >
                                {isGenerating ? <Loader2 className="animate-spin" size={18} /> : <Sparkles size={18} />}
                                {isGenerating ? 'Generating Code...' : 'Generate Module'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default BasicPlusEditor;