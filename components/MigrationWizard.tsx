import React, { useState } from 'react';
import { Upload, ArrowRight, ChevronRight, Play, FileSpreadsheet, RefreshCw, Server } from 'lucide-react';
import { MigrationFieldMapping, MigrationLog } from '../types';
import { autoMapMigrationFields, generateMigrationBasicPlusCode } from '../services/geminiService';

// --- EXISTING MIGRATION CONSTANTS ---
const TARGET_DICTIONARIES = [
    'CIF_NO', 'FULL_NAME', 'GHANA_CARD_ID', 'DIGITAL_ADDRESS_GPS', 
    'PHONE_MOBILE', 'EMAIL_ADDR', 'KYC_TIER', 'DATE_OF_BIRTH', 
    'NEXT_OF_KIN', 'RELATIONSHIP_MGR'
];

const MOCK_CSV_DATA = [
    { "Client_ID": "OLD_001", "Client_Name": "Kwame Mensah", "Nat_ID": "GHA-728192823-1", "Phone": "0244123456", "GPS": "GA-100-2020" },
    { "Client_ID": "OLD_002", "Client_Name": "Adwoa Boateng", "Nat_ID": "GHA-111222333-X", "Phone": "0509988776", "GPS": "AK-039-2911" },
    { "Client_ID": "OLD_003", "Client_Name": "Kofi Annan", "Nat_ID": "INVALID-ID", "Phone": "0201122334", "GPS": "WR-222-1111" },
    { "Client_ID": "OLD_004", "Client_Name": "Ama Serwaa", "Nat_ID": "GHA-999888777-2", "Phone": "0245556666", "GPS": "ER-333-4444" },
    { "Client_ID": "OLD_005", "Client_Name": "Yaw Osei", "Nat_ID": "GHA-123456789-0", "Phone": "0277778888", "GPS": "AS-555-6666" },
];

const MigrationWizard: React.FC = () => {
    const [step, setStep] = useState<1 | 2 | 3 | 4>(1);
    const [rawData, setRawData] = useState<any[]>([]);
    const [headers, setHeaders] = useState<string[]>([]);
    const [mappings, setMappings] = useState<MigrationFieldMapping[]>([]);
    const [logs, setLogs] = useState<MigrationLog[]>([]);
    const [progress, setProgress] = useState(0);
    const [analyzing, setAnalyzing] = useState(false);
    const [generatedScript, setGeneratedScript] = useState('');
    const [generatingScript, setGeneratingScript] = useState(false);

    // --- MIGRATION HANDLERS ---
    const handleFileUpload = () => {
        setAnalyzing(true);
        setTimeout(async () => {
            const data = MOCK_CSV_DATA;
            const heads = Object.keys(data[0]);
            setRawData(data);
            setHeaders(heads);
            const suggestions = await autoMapMigrationFields(heads, TARGET_DICTIONARIES);
            setMappings(suggestions);
            setAnalyzing(false);
            setStep(2);
        }, 1500);
    };

    const handleMappingChange = (legacyHeader: string, newDict: string) => {
        setMappings(prev => prev.map(m => m.legacyHeader === legacyHeader ? { ...m, targetDict: newDict, confidence: 1 } : m));
    };

    const handleGenerateScript = async () => {
        setGeneratingScript(true);
        const script = await generateMigrationBasicPlusCode(mappings, "CUST_ACCOUNTS");
        setGeneratedScript(script);
        setGeneratingScript(false);
    };

    const executeMigration = () => {
        setStep(4);
        setLogs([]);
        setProgress(0);
        let currentIndex = 0;
        const total = rawData.length;
        const interval = setInterval(() => {
            if (currentIndex >= total) {
                clearInterval(interval);
                return;
            }
            const row = rawData[currentIndex];
            const ghanaCardHeader = mappings.find(m => m.targetDict === 'GHANA_CARD_ID')?.legacyHeader;
            const ghanaCardValue = ghanaCardHeader ? row[ghanaCardHeader] : '';
            const isValidID = /^GHA-\d{9}-\d{1}$/.test(ghanaCardValue);
            const newLog: MigrationLog = {
                row: currentIndex + 1,
                status: isValidID ? 'SUCCESS' : 'ERROR',
                message: isValidID ? 'Record written to DATA_VOL' : 'Validation Failed: Invalid Ghana Card Format',
                data: JSON.stringify(row)
            };
            setLogs(prev => [newLog, ...prev]);
            setProgress(Math.round(((currentIndex + 1) / total) * 100));
            currentIndex++;
        }, 800);
    };

    return (
        <div className="h-full flex flex-col bg-gray-50 rounded-xl overflow-hidden">
            {/* Header */}
            <div className="bg-white px-6 py-4 border-b border-gray-200">
                <div className="flex justify-between items-center">
                     <div>
                         <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
                            <Server className="text-blue-700" size={24} />
                            Data Migration Utility
                        </h1>
                        <p className="text-gray-500 text-sm mt-1">
                            Import legacy data into PostgreSQL linear hash files.
                        </p>
                    </div>
                </div>
            </div>

            {/* Content */}
            <div className="flex-1 p-8 overflow-y-auto">
                <div className="flex items-center gap-2 text-sm font-medium text-gray-500 mb-6">
                    <span className={`px-3 py-1 rounded-full ${step >= 1 ? 'bg-blue-100 text-blue-800' : 'bg-gray-100'}`}>1. Source</span>
                    <ChevronRight size={16} />
                    <span className={`px-3 py-1 rounded-full ${step >= 2 ? 'bg-blue-100 text-blue-800' : 'bg-gray-100'}`}>2. Map</span>
                    <ChevronRight size={16} />
                    <span className={`px-3 py-1 rounded-full ${step >= 3 ? 'bg-blue-100 text-blue-800' : 'bg-gray-100'}`}>3. Review</span>
                    <ChevronRight size={16} />
                    <span className={`px-3 py-1 rounded-full ${step >= 4 ? 'bg-blue-100 text-blue-800' : 'bg-gray-100'}`}>4. Load</span>
                </div>

                {step === 1 && (
                    <div className="h-full flex flex-col items-center justify-center border-2 border-dashed border-gray-300 rounded-xl bg-white p-12 text-center">
                        <div className="w-16 h-16 bg-blue-50 text-blue-600 rounded-full flex items-center justify-center mb-4">
                            <Upload size={32} />
                        </div>
                        <h3 className="text-xl font-semibold text-gray-900 mb-2">Upload Legacy Data File</h3>
                        <p className="text-gray-500 mb-6 max-w-md">Supported formats: CSV, Excel, XML.</p>
                        <button onClick={handleFileUpload} disabled={analyzing} className="px-6 py-3 bg-blue-700 text-white font-medium rounded-lg hover:bg-blue-800 flex items-center gap-2 disabled:opacity-50">
                            {analyzing ? 'Analyzing...' : <><FileSpreadsheet size={18} /> Select File</>}
                        </button>
                    </div>
                )}
                
                {step === 2 && (
                    <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
                            <div className="p-4 bg-gray-50 border-b border-gray-200">
                            <h3 className="font-semibold text-gray-700">Dictionary Mapping</h3>
                            </div>
                            <table className="w-full text-sm text-left">
                            <thead className="bg-gray-50 text-gray-500 uppercase font-medium"><tr><th className="p-4">Legacy Header</th><th className="p-4 text-center">Mapping</th><th className="p-4">Target Dictionary</th></tr></thead>
                            <tbody className="divide-y divide-gray-100">
                                {mappings.map((map, idx) => (
                                    <tr key={idx}>
                                        <td className="p-4 font-mono text-gray-800">{map.legacyHeader}</td>
                                        <td className="p-4 text-center text-gray-400"><ArrowRight size={16} className="mx-auto"/></td>
                                        <td className="p-4">
                                            <select value={map.targetDict} onChange={(e) => handleMappingChange(map.legacyHeader, e.target.value)} className={`w-full p-2 border rounded ${map.confidence > 0.8 ? 'bg-green-50 border-green-300' : ''}`}>
                                                <option value="">-- Ignore --</option>
                                                {TARGET_DICTIONARIES.map(d => <option key={d} value={d}>{d}</option>)}
                                            </select>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                            </table>
                            <div className="p-4 border-t border-gray-200 flex justify-end">
                            <button onClick={() => setStep(3)} className="px-6 py-2 bg-blue-700 text-white rounded-lg">Next</button>
                            </div>
                    </div>
                )}
                
                {step === 3 && (
                    <div className="max-w-3xl mx-auto space-y-6">
                            <div className="bg-blue-50 border border-blue-200 rounded-xl p-6">
                            <h3 className="text-lg font-bold text-blue-900 mb-2">Summary</h3>
                            <div className="text-sm">Mapping {mappings.filter(m => m.targetDict).length} fields for {rawData.length} records.</div>
                            </div>
                            <div className="bg-gray-900 rounded-xl overflow-hidden border border-gray-700">
                            <div className="px-4 py-3 bg-gray-800 border-b border-gray-700 flex justify-between items-center text-white">
                                <span className="font-mono text-sm">Import Script</span>
                                <button onClick={handleGenerateScript} disabled={generatingScript} className="text-xs bg-blue-700 px-3 py-1 rounded">Generate</button>
                            </div>
                            {generatedScript && <div className="p-4"><textarea readOnly value={generatedScript} className="w-full h-48 bg-transparent text-green-400 font-mono text-xs resize-none focus:outline-none"/></div>}
                            </div>
                            <div className="flex justify-end gap-3">
                                <button onClick={() => setStep(2)} className="px-6 py-2 border rounded-lg">Back</button>
                                <button onClick={executeMigration} className="px-6 py-2 bg-green-600 text-white rounded-lg flex items-center gap-2"><Play size={16}/> Start</button>
                            </div>
                    </div>
                )}
                
                {step === 4 && (
                    <div className="space-y-6">
                            <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
                            <div className="flex justify-between mb-2"><span className="font-semibold">Importing...</span><span className="font-mono text-blue-600">{progress}%</span></div>
                            <div className="w-full bg-gray-100 rounded-full h-3 overflow-hidden"><div className="bg-blue-600 h-3 rounded-full transition-all duration-300" style={{ width: `${progress}%` }}></div></div>
                            </div>
                            <div className="bg-gray-900 rounded-xl overflow-hidden border border-gray-800 h-96 flex flex-col">
                            <div className="px-4 py-2 bg-gray-800 text-gray-400 font-mono text-xs">MIGRATION_LOG.TXT</div>
                            <div className="flex-1 p-4 overflow-y-auto font-mono text-xs space-y-1">
                                {logs.map((log, i) => (
                                    <div key={i} className="flex gap-3"><span className={log.status === 'SUCCESS' ? 'text-green-500' : 'text-red-500'}>{log.status}</span><span className="text-gray-300">{log.message}</span></div>
                                ))}
                            </div>
                            </div>
                            {progress === 100 && <div className="flex justify-end"><button onClick={() => { setStep(1); setGeneratedScript(''); }} className="px-6 py-2 bg-blue-700 text-white rounded-lg">New Job</button></div>}
                    </div>
                )}
            </div>
        </div>
    );
};

export default MigrationWizard;