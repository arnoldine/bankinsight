
import React, { useState, useMemo, useEffect } from 'react';
import { Customer, Account, Loan, Transaction, ClientNote, ClientDocument, Product } from '../types';
import { 
    Search, UserPlus, Eye, Smartphone, MapPin, CreditCard, Shield, 
    MoreHorizontal, User, AlertTriangle, CheckCircle2, Briefcase, 
    History, ArrowRightLeft, FileText, StickyNote, Plus, Filter, 
    Save, X, Upload, CheckCircle, Clock, XCircle, Printer, Download, Calendar, ArrowUpRight, ArrowDownRight, Wallet, PieChart, File,
    ChevronRight, ChevronLeft, Award, Building2
} from 'lucide-react';
import ClientTransactionHistory from './ClientTransactionHistory';
import { Can } from './Can';
import { Permissions } from '../lib/Permissions';
import { customerService } from '../src/services/customerService';

interface ClientManagerProps {
  customers: Customer[];
  accounts: Account[];
  loans: Loan[];
  transactions: Transaction[];
  products?: Product[]; // Make it optional for now to avoid breaking but logic will depend on it
  onCreateCustomer: (data: any) => void;
  onUpdateCustomer: (id: string, data: Partial<Customer>) => void;
  onCreateAccount: (cif: string, product: string, type: any) => void;
  initialView?: 'LIST' | 'CREATE' | 'DETAILS';
  initialDetailTab?: 'OVERVIEW' | 'ACCOUNTS' | 'LOANS' | 'TRANSACTIONS' | 'DOCS' | 'NOTES';
}

const ClientManager: React.FC<ClientManagerProps> = ({ customers, accounts, loans, transactions, products = [], onCreateCustomer, onUpdateCustomer, onCreateAccount, initialView = 'LIST', initialDetailTab = 'OVERVIEW' }) => {
  const [view, setView] = useState<'LIST' | 'CREATE' | 'DETAILS'>(initialView);
  const [selectedClient, setSelectedClient] = useState<Customer | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterTier, setFilterTier] = useState<string>('ALL');
  const [error, setError] = useState<string | null>(null);
  
  // Details View State
  const [detailTab, setDetailTab] = useState<'OVERVIEW' | 'ACCOUNTS' | 'LOANS' | 'TRANSACTIONS' | 'DOCS' | 'NOTES'>(initialDetailTab);
  const [isEditing, setIsEditing] = useState(false);
  const [editFormData, setEditFormData] = useState<Partial<Customer>>({});
  const [showAccountModal, setShowAccountModal] = useState(false);
  const [showUploadModal, setShowUploadModal] = useState(false); // New Upload Modal
  
  // Statement Modal State
  const [showStatementModal, setShowStatementModal] = useState(false);
  const [selectedAccountForStatement, setSelectedAccountForStatement] = useState<Account | null>(null);
  const [isProfileLoading, setIsProfileLoading] = useState(false);

  // Account Wizard State
  const [wizardStep, setWizardStep] = useState(1);
  const [newAccountProduct, setNewAccountProduct] = useState('');
  const [newAccountCurrency, setNewAccountCurrency] = useState<'GHS' | 'USD'>('GHS');
  const [createdAccountId, setCreatedAccountId] = useState<string | null>(null);

  // Document Upload State
  const [newDoc, setNewDoc] = useState({ type: 'ID Card', name: '' });

  // Filter available products for account creation (exclude loans)
  const depositProducts = useMemo(() => products.filter(p => (p.type === 'SAVINGS' || p.type === 'CURRENT' || p.type === 'FIXED_DEPOSIT') && p.status === 'ACTIVE'), [products]);

  const [notes, setNotes] = useState<ClientNote[]>([]);
  const [documents, setDocuments] = useState<ClientDocument[]>([]);
  const [noteInput, setNoteInput] = useState('');

  // Form State
  const [newClient, setNewClient] = useState<Partial<Customer>>({
      type: 'INDIVIDUAL',
      name: '', phone: '', email: '', 
      ghanaCard: '', digitalAddress: '', kycLevel: 'Tier 1', riskRating: 'Low',
      dateOfBirth: '', gender: 'Male',
      nationality: 'GHANAIAN', maritalStatus: 'Single', spouseName: '', ssnitNo: '',
      businessRegistrationNo: '', tin: '', sector: 'COMMERCE'
  });

  React.useEffect(() => {
    setView(initialView);
  }, [initialView]);

  React.useEffect(() => {
    setDetailTab(initialDetailTab);
  }, [initialDetailTab]);

  // --- DERIVED DATA ---
  const filteredClients = useMemo(() => {
    return customers.filter(c => {
        const matchesSearch = c.name.toLowerCase().includes(searchTerm.toLowerCase()) || c.id.toLowerCase().includes(searchTerm.toLowerCase());
        const matchesTier = filterTier === 'ALL' || c.kycLevel === filterTier;
        return matchesSearch && matchesTier;
    });
  }, [customers, searchTerm, filterTier]);

  const clientAccounts = useMemo(() => selectedClient ? accounts.filter(a => a.cif === selectedClient.id) : [], [accounts, selectedClient]);
  const clientLoans = useMemo(() => selectedClient ? loans.filter(l => l.cif === selectedClient.id) : [], [loans, selectedClient]);
  const clientTransactions = useMemo(() => {
    if (!selectedClient) return [];
    const accIds = clientAccounts.map(a => a.id);
    return transactions.filter(t => accIds.includes(t.accountId)).sort((a,b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  }, [transactions, clientAccounts, selectedClient]);

  // --- EFFECTS ---
  useEffect(() => {
    if (!selectedClient) return;

    setEditFormData({
      name: selectedClient.name,
      phone: selectedClient.phone,
      email: selectedClient.email,
      digitalAddress: selectedClient.digitalAddress,
      ghanaCard: selectedClient.ghanaCard,
      employer: selectedClient.employer,
      maritalStatus: selectedClient.maritalStatus,
      spouseName: selectedClient.spouseName,
      riskRating: selectedClient.riskRating,
    });
    setNotes(selectedClient.notes || []);
    setDocuments(selectedClient.documents || []);
    setDetailTab('OVERVIEW');
    setIsEditing(false);

    let isActive = true;
    setIsProfileLoading(true);
    customerService.getCustomerProfile(selectedClient.id)
      .then((profile) => {
        if (!isActive) return;
        setSelectedClient(profile);
        setNotes(profile.notes || []);
        setDocuments(profile.documents || []);
        setEditFormData({
          name: profile.name,
          phone: profile.phone,
          email: profile.email,
          digitalAddress: profile.digitalAddress,
          ghanaCard: profile.ghanaCard,
          employer: profile.employer,
          maritalStatus: profile.maritalStatus,
          spouseName: profile.spouseName,
          riskRating: profile.riskRating,
        });
      })
      .catch((err) => {
        if (isActive) {
          console.error('Failed to load customer profile:', err);
        }
      })
      .finally(() => {
        if (isActive) {
          setIsProfileLoading(false);
        }
      });

    return () => {
      isActive = false;
    };
  }, [selectedClient?.id]);

  // Reset wizard when modal opens
  useEffect(() => {
      if (showAccountModal) {
          setWizardStep(1);
          setCreatedAccountId(null);
          setNewAccountProduct('');
          setNewAccountCurrency('GHS');
      }
  }, [showAccountModal]);

  // --- HANDLERS ---
  const handleCreateSubmit = async () => {
      // Basic validation logic
      if(!newClient.name || !newClient.phone) {
          setError("Please fill all mandatory fields (*)");
          return;
      }
      if (newClient.type === 'INDIVIDUAL' && !newClient.ghanaCard) {
          setError("Ghana Card is required for Individuals");
          return;
      }
      if (newClient.type === 'CORPORATE' && (!newClient.businessRegistrationNo || !newClient.tin)) {
          setError("Registration No and TIN are required for Corporates");
          return;
      }

      try {
        await onCreateCustomer(newClient);
        setView('LIST');
        setNewClient({ type: 'INDIVIDUAL', name: '', phone: '', email: '', ghanaCard: '', digitalAddress: '', kycLevel: 'Tier 1', riskRating: 'Low' });
      } catch (e: any) {
        setError(e.message);
      }
  };

  const handleUpdateProfile = async () => {
      if(!selectedClient) return;
      await onUpdateCustomer(selectedClient.id, editFormData);
      setSelectedClient(prev => prev ? ({ ...prev, ...editFormData }) : null);
      setIsEditing(false);
  };

  const handleCreateAccount = async () => {
      if(!selectedClient || !newAccountProduct) return;
      try {
          const product = depositProducts.find(p => p.id === newAccountProduct);
          if(product) {
              const accId = await onCreateAccount(selectedClient.id, product.id, newAccountCurrency);
              setCreatedAccountId(typeof accId === 'string' ? accId : 'PENDING');
              setWizardStep(4);
          }
      } catch (e) {
          console.error(e);
      }
  };

  const handleFinishWizard = () => {
      setShowAccountModal(false);
      setDetailTab('ACCOUNTS');
  };

  const handleAddNote = async () => {
      if(!selectedClient || !noteInput.trim()) return;
      const createdNote = await customerService.addCustomerNote(selectedClient.id, noteInput, 'GENERAL');
      setNotes([createdNote, ...notes]);
      setSelectedClient(prev => prev ? ({ ...prev, notes: [createdNote, ...(prev.notes || [])] }) : prev);
      setNoteInput('');
  };

  const handleUploadDocument = async () => {
      if (!selectedClient || !newDoc.name) return;
      const createdDocument = await customerService.addCustomerDocument(selectedClient.id, newDoc.type, newDoc.name);
      setDocuments([createdDocument, ...documents]);
      setSelectedClient(prev => prev ? ({ ...prev, documents: [createdDocument, ...(prev.documents || [])] }) : prev);
      setShowUploadModal(false);
      setNewDoc({ type: 'ID Card', name: '' });
  };

  const openStatement = (account: Account) => {
      setSelectedAccountForStatement(account);
      setShowStatementModal(true);
  };

  const renderStatusBadge = (status: string) => {
      const styles = {
          'ACTIVE': 'bg-green-100 text-green-700',
          'DORMANT': 'bg-orange-100 text-orange-700',
          'FROZEN': 'bg-red-100 text-red-700',
          'VERIFIED': 'bg-green-100 text-green-700',
          'PENDING': 'bg-yellow-100 text-yellow-700',
          'REJECTED': 'bg-red-100 text-red-700'
      };
      return <span className={`px-2 py-0.5 rounded text-xs font-bold ${styles[status as keyof typeof styles] || 'bg-gray-100'}`}>{status}</span>;
  };

  // Helper component for the Statement
  const AccountStatementModal = () => {
      // ... existing implementation ...
      if (!selectedAccountForStatement || !selectedClient) return null;

      // Filter transactions for this account
      const accountTxns = transactions
        .filter(t => t.accountId === selectedAccountForStatement.id)
        .sort((a,b) => new Date(a.date).getTime() - new Date(b.date).getTime()); // Ascending order for running balance

      let runningBalance = 0;
      const statementLines = accountTxns.map(tx => {
          const isCredit = tx.type === 'DEPOSIT' || tx.type === 'LOAN_REPAYMENT'; 
          const amount = tx.amount;
          if (isCredit) runningBalance += amount;
          else runningBalance -= amount;

          return { ...tx, runningBalance };
      });
      
      const displayLines = [...statementLines].reverse();

      return (
          <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4 backdrop-blur-sm">
              <div className="bg-white rounded-lg shadow-2xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden">
                  <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
                      <h3 className="font-bold text-gray-800 flex items-center gap-2">
                          <FileText className="text-blue-600" size={20} /> Account Statement
                      </h3>
                      <button onClick={() => setShowStatementModal(false)}><X size={20} className="text-gray-400 hover:text-gray-600"/></button>
                  </div>
                  
                  <div className="flex-1 overflow-y-auto p-8 bg-gray-50/50">
                      {/* Paper Layout */}
                      <div className="bg-white border border-gray-200 shadow-sm p-8 min-h-[600px] text-sm">
                          {/* Header */}
                          <div className="flex justify-between items-start mb-8 border-b pb-6">
                              <div>
                                  <h1 className="text-2xl font-bold text-blue-900 mb-1">OpenInsight Bank</h1>
                                  <p className="text-gray-500">Head Office Branch</p>
                                  <p className="text-gray-500">Accra, Ghana</p>
                              </div>
                              <div className="text-right">
                                  <h2 className="text-xl font-bold text-gray-800">Statement of Account</h2>
                                  <p className="text-gray-500">Date: {new Date().toLocaleDateString()}</p>
                              </div>
                          </div>

                          {/* Account Info */}
                          <div className="grid grid-cols-2 gap-8 mb-8">
                              <div>
                                  <h4 className="text-xs font-bold text-gray-400 uppercase mb-1">Account Holder</h4>
                                  <p className="font-bold text-gray-900 text-lg">{selectedClient.name}</p>
                                  <p className="text-gray-600">{selectedClient.digitalAddress}</p>
                                  <p className="text-gray-600">CIF: {selectedClient.id}</p>
                              </div>
                              <div className="bg-gray-50 p-4 rounded border border-gray-100">
                                  <div className="flex justify-between mb-2">
                                      <span className="text-gray-500">Account Number</span>
                                      <span className="font-mono font-bold text-gray-900">{selectedAccountForStatement.id}</span>
                                  </div>
                                  <div className="flex justify-between mb-2">
                                      <span className="text-gray-500">Product</span>
                                      <span className="font-medium text-gray-900">{selectedAccountForStatement.type} ({selectedAccountForStatement.productCode})</span>
                                  </div>
                                  <div className="flex justify-between mb-2">
                                      <span className="text-gray-500">Currency</span>
                                      <span className="font-medium text-gray-900">{selectedAccountForStatement.currency}</span>
                                  </div>
                                  <div className="flex justify-between border-t pt-2 mt-2">
                                      <span className="text-gray-500 font-bold">Available Balance</span>
                                      <span className="font-mono font-bold text-blue-700 text-lg">
                                          {selectedAccountForStatement.balance.toLocaleString(undefined, {minimumFractionDigits: 2})}
                                      </span>
                                  </div>
                              </div>
                          </div>

                          {/* Transaction Table */}
                          <table className="w-full text-left">
                              <thead className="bg-gray-100 text-gray-600 text-xs uppercase font-bold border-y border-gray-200">
                                  <tr>
                                      <th className="py-3 px-2">Date</th>
                                      <th className="py-3 px-2">Reference</th>
                                      <th className="py-3 px-2 w-1/3">Narration</th>
                                      <th className="py-3 px-2 text-right">Debit</th>
                                      <th className="py-3 px-2 text-right">Credit</th>
                                      <th className="py-3 px-2 text-right">Balance</th>
                                  </tr>
                              </thead>
                              <tbody className="divide-y divide-gray-100 font-mono text-xs">
                                  {displayLines.map((tx) => (
                                      <tr key={tx.id}>
                                          <td className="py-3 px-2 text-gray-600">{new Date(tx.date).toLocaleDateString()}</td>
                                          <td className="py-3 px-2 text-blue-600">{tx.id.split('-').pop()}</td>
                                          <td className="py-3 px-2 text-gray-800">{tx.narration}</td>
                                          <td className="py-3 px-2 text-right text-gray-600">
                                              {tx.type !== 'DEPOSIT' && tx.type !== 'LOAN_REPAYMENT' ? tx.amount.toLocaleString(undefined, {minimumFractionDigits: 2}) : '-'}
                                          </td>
                                          <td className="py-3 px-2 text-right text-gray-600">
                                              {tx.type === 'DEPOSIT' || tx.type === 'LOAN_REPAYMENT' ? tx.amount.toLocaleString(undefined, {minimumFractionDigits: 2}) : '-'}
                                          </td>
                                          <td className="py-3 px-2 text-right font-bold text-gray-800">
                                              {tx.runningBalance.toLocaleString(undefined, {minimumFractionDigits: 2})}
                                          </td>
                                      </tr>
                                  ))}
                                  {displayLines.length === 0 && (
                                      <tr><td colSpan={6} className="py-8 text-center text-gray-400">No transactions in selected period.</td></tr>
                                  )}
                              </tbody>
                          </table>
                      </div>
                  </div>

                  <div className="p-4 border-t border-gray-200 bg-gray-50 flex justify-end gap-3">
                      <button className="px-4 py-2 bg-white border border-gray-300 text-gray-700 font-medium rounded hover:bg-gray-100 flex items-center gap-2">
                          <Download size={16} /> Export PDF
                      </button>
                      <button className="px-4 py-2 bg-blue-600 text-white font-medium rounded hover:bg-blue-700 flex items-center gap-2">
                          <Printer size={16} /> Print Statement
                      </button>
                  </div>
              </div>
          </div>
      );
  };

  return (
    <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden relative">
        
        {/* ... (Account Wizard and Upload Modal remain similar, just ensuring they use selectedClient) ... */}
        {showAccountModal && selectedClient && (
            <div className="absolute inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
               {/* ... (Account Wizard Content - reused) ... */}
               <div className="bg-white rounded-xl shadow-2xl w-full max-w-2xl flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-200">
                    <div className="bg-gray-50 p-6 border-b border-gray-200 flex justify-between items-center">
                        <div>
                            <h3 className="text-xl font-bold text-gray-900">Open New Account</h3>
                            <p className="text-sm text-gray-500">Account Opening Wizard for <span className="font-semibold text-blue-700">{selectedClient.name}</span></p>
                        </div>
                        <button onClick={() => setShowAccountModal(false)}><X size={24} className="text-gray-400 hover:text-gray-600"/></button>
                    </div>
                    {/* ... Wizard Steps reused (simplified here) ... */}
                    <div className="p-8 flex-1 overflow-y-auto">
                        {wizardStep === 1 && (
                            <div className="space-y-6">
                                <div className="bg-blue-50 border border-blue-100 rounded-lg p-4 flex items-start gap-4">
                                    <div className="bg-blue-100 p-2 rounded-full text-blue-600">
                                        <User size={24} />
                                    </div>
                                    <div>
                                        <h4 className="font-bold text-blue-900 text-lg">{selectedClient.name}</h4>
                                        <p className="text-sm text-blue-700">CIF: {selectedClient.id}</p>
                                        <div className="flex gap-2 mt-2">
                                            <span className="bg-white border border-blue-200 text-blue-700 px-2 py-1 rounded text-xs font-bold">
                                                {selectedClient.kycLevel}
                                            </span>
                                        </div>
                                    </div>
                                </div>
                                <button onClick={() => setWizardStep(2)} className="w-full bg-blue-600 text-white py-2 rounded font-bold">Proceed to Product Selection</button>
                            </div>
                        )}
                        {/* ... Other steps same logic as before ... */}
                        {wizardStep === 2 && (
                             <div className="space-y-4">
                                <h4 className="font-bold text-gray-800">Select Banking Product</h4>
                                <div className="grid grid-cols-2 gap-4">
                                    {depositProducts.map(product => (
                                        <div 
                                            key={product.id}
                                            onClick={() => { setNewAccountProduct(product.id); setWizardStep(3); }}
                                            className="cursor-pointer border rounded-xl p-4 hover:border-blue-500 hover:bg-blue-50"
                                        >
                                            <h5 className="font-bold text-gray-900 mb-1">{product.name}</h5>
                                            <span className="text-xs bg-gray-100 px-2 py-1 rounded">{product.currency}</span>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                        {wizardStep === 3 && (
                             <div className="space-y-6">
                                <p>Confirm account creation for {newAccountProduct}?</p>
                                <button onClick={handleCreateAccount} className="w-full bg-green-600 text-white py-2 rounded font-bold">Confirm & Create</button>
                            </div>
                        )}
                        {wizardStep === 4 && (
                            <div className="text-center p-8">
                                <h3 className="text-2xl font-bold text-green-600 mb-2">Success!</h3>
                                <p>Account {createdAccountId} created.</p>
                                <button onClick={handleFinishWizard} className="mt-4 bg-gray-200 px-4 py-2 rounded">Close</button>
                            </div>
                        )}
                    </div>
               </div>
            </div>
        )}

        {/* --- UPLOAD DOCUMENT MODAL --- */}
        {showUploadModal && selectedClient && (
            <div className="absolute inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
                <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6 animate-in fade-in zoom-in-95 duration-200">
                    <div className="flex justify-between items-center mb-4">
                        <h3 className="text-lg font-bold text-gray-800">Upload Document</h3>
                        <button onClick={() => setShowUploadModal(false)}><X size={20} className="text-gray-400 hover:text-gray-600"/></button>
                    </div>
                    <div className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Document Type</label>
                            <select 
                                className="w-full border border-gray-300 rounded p-2 text-sm bg-white"
                                value={newDoc.type}
                                onChange={(e) => setNewDoc({...newDoc, type: e.target.value})}
                            >
                                <option value="ID Card">ID Card (Ghana Card)</option>
                                <option value="Proof of Address">Proof of Address (Utility Bill)</option>
                                <option value="Business Registration">Business Registration</option>
                                <option value="Tax Clearance">Tax Clearance Cert</option>
                                <option value="Form">Application Form</option>
                                <option value="Other">Other</option>
                            </select>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">File Name</label>
                            <input 
                                type="text" 
                                className="w-full border border-gray-300 rounded p-2 text-sm"
                                placeholder="e.g. Scanned_Doc.jpg"
                                value={newDoc.name}
                                onChange={(e) => setNewDoc({...newDoc, name: e.target.value})}
                            />
                        </div>
                        <button 
                            onClick={handleUploadDocument}
                            disabled={!newDoc.name}
                            className="w-full bg-blue-600 text-white py-2 rounded font-bold hover:bg-blue-700 flex items-center justify-center gap-2 disabled:opacity-50"
                        >
                            <Upload size={16} /> Upload Now
                        </button>
                    </div>
                </div>
            </div>
        )}

        {/* --- STATEMENT MODAL --- */}
        {showStatementModal && <AccountStatementModal />}

        {/* --- HEADER --- */}
        <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
            <h2 className="text-lg font-bold text-gray-800 flex items-center gap-2">
                <User className="text-blue-600" size={20} /> Client Management
            </h2>
            <div className="flex gap-2">
                {view === 'LIST' && (
                    <Can permission={Permissions.Customers.Create}>
                        <button onClick={() => setView('CREATE')} className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 flex items-center gap-2">
                            <UserPlus size={16} /> Onboard New Client
                        </button>
                    </Can>
                )}
                {view !== 'LIST' && (
                    <button onClick={() => setView('LIST')} className="text-gray-600 hover:text-blue-600 text-sm font-medium px-3 py-2 border rounded bg-white">
                        &larr; Back to List
                    </button>
                )}
            </div>
        </div>

        <div className="flex-1 overflow-hidden flex flex-col">
            
            {/* --- VIEW: LIST --- */}
            {view === 'LIST' && (
                <div className="flex-1 flex flex-col">
                    {/* Toolbar */}
                    <div className="p-4 border-b border-gray-100 flex gap-4 items-center">
                        <div className="relative flex-1 max-w-md">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
                            <input 
                                type="text" 
                                placeholder="Search by Name or CIF..." 
                                value={searchTerm}
                                onChange={e => setSearchTerm(e.target.value)}
                                className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 outline-none"
                            />
                        </div>
                        <div className="flex items-center gap-2">
                            <Filter size={16} className="text-gray-400" />
                            <select 
                                className="border border-gray-300 rounded-lg p-2 text-sm bg-white"
                                value={filterTier}
                                onChange={e => setFilterTier(e.target.value)}
                            >
                                <option value="ALL">All Tiers</option>
                                <option value="Tier 1">Tier 1</option>
                                <option value="Tier 2">Tier 2</option>
                                <option value="Tier 3">Tier 3</option>
                            </select>
                        </div>
                    </div>

                    {/* Table */}
                    <div className="flex-1 overflow-y-auto">
                        <table className="w-full text-sm text-left">
                            <thead className="bg-gray-50 text-gray-600 font-semibold sticky top-0 z-10 border-b border-gray-200">
                                <tr>
                                    <th className="p-4">CIF Number</th>
                                    <th className="p-4">Entity Name</th>
                                    <th className="p-4">Type</th>
                                    <th className="p-4">Phone</th>
                                    <th className="p-4">KYC Tier</th>
                                    <th className="p-4">Risk Rating</th>
                                    <th className="p-4 text-center">Action</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-gray-100">
                                {filteredClients.map(client => (
                                    <tr key={client.id} className="hover:bg-blue-50 group transition-colors">
                                        <td className="p-4 font-mono text-blue-600 font-medium">{client.id}</td>
                                        <td className="p-4 font-medium text-gray-800">
                                            <div className="flex items-center gap-2">
                                                <div className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${client.type === 'CORPORATE' ? 'bg-purple-100 text-purple-600' : 'bg-blue-100 text-blue-600'}`}>
                                                    {client.type === 'CORPORATE' ? <Building2 size={12}/> : <User size={12}/>}
                                                </div>
                                                {client.name}
                                            </div>
                                        </td>
                                        <td className="p-4 text-xs font-bold text-gray-500">{client.type}</td>
                                        <td className="p-4 text-gray-600">{client.phone}</td>
                                        <td className="p-4">
                                            <span className={`px-2 py-0.5 rounded text-xs font-bold border ${client.kycLevel === 'Tier 3' ? 'bg-green-100 text-green-700 border-green-200' : 'bg-gray-100 text-gray-600 border-gray-200'}`}>
                                                {client.kycLevel}
                                            </span>
                                        </td>
                                        <td className="p-4">
                                            <span className={`px-2 py-0.5 rounded text-xs font-bold ${client.riskRating === 'High' ? 'bg-red-50 text-red-600' : 'bg-green-50 text-green-600'}`}>
                                                {client.riskRating}
                                            </span>
                                        </td>
                                        <td className="p-4 text-center">
                                            <button 
                                                onClick={() => { setSelectedClient(client); setView('DETAILS'); }}
                                                className="text-gray-400 hover:text-blue-600 p-2 hover:bg-blue-100 rounded-full transition-colors"
                                            >
                                                <Eye size={18} />
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {/* --- VIEW: CREATE --- */}
            {view === 'CREATE' && (
                <div className="flex-1 overflow-y-auto p-8">
                    <div className="max-w-3xl mx-auto bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
                        <div className="bg-blue-50 p-6 border-b border-blue-100">
                            <h3 className="text-xl font-bold text-blue-900 flex items-center gap-2">
                                <UserPlus size={24}/> Client Onboarding
                            </h3>
                            <p className="text-blue-600 text-sm mt-1">Bank of Ghana Mandated KYC Process</p>
                        </div>
                        
                        <div className="p-8 space-y-6">
                            {error && (
                                <div className="p-4 bg-red-50 text-red-700 border border-red-200 rounded flex items-center gap-2 text-sm">
                                    <AlertTriangle size={18} /> {error}
                                </div>
                            )}

                            {/* Client Type Selector */}
                            <div className="flex gap-4 mb-4">
                                <button 
                                    className={`flex-1 py-3 border rounded-lg font-bold ${newClient.type === 'INDIVIDUAL' ? 'bg-blue-600 text-white border-blue-600' : 'text-gray-600 hover:bg-gray-50'}`}
                                    onClick={() => setNewClient({...newClient, type: 'INDIVIDUAL'})}
                                >
                                    Individual
                                </button>
                                <button 
                                    className={`flex-1 py-3 border rounded-lg font-bold ${newClient.type === 'CORPORATE' ? 'bg-blue-600 text-white border-blue-600' : 'text-gray-600 hover:bg-gray-50'}`}
                                    onClick={() => setNewClient({...newClient, type: 'CORPORATE'})}
                                >
                                    Corporate / SME
                                </button>
                            </div>

                            <div className="grid grid-cols-2 gap-6">
                                <div className="col-span-2">
                                    <label className="block text-sm font-bold text-gray-700 mb-1">{newClient.type === 'INDIVIDUAL' ? 'Full Name' : 'Company Name'} <span className="text-red-500">*</span></label>
                                    <input type="text" className="w-full border border-gray-300 rounded p-2.5 focus:ring-2 focus:ring-blue-500 outline-none"
                                        value={newClient.name} onChange={e => setNewClient({...newClient, name: e.target.value})} />
                                </div>
                                
                                {newClient.type === 'INDIVIDUAL' && (
                                    <>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Ghana Card (NIA) <span className="text-red-500">*</span></label>
                                            <input type="text" className="w-full border border-gray-300 rounded p-2.5 uppercase font-mono focus:ring-2 focus:ring-blue-500 outline-none"
                                                placeholder="GHA-000000000-0"
                                                value={newClient.ghanaCard} onChange={e => setNewClient({...newClient, ghanaCard: e.target.value.toUpperCase()})} />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Date of Birth</label>
                                            <input type="date" className="w-full border border-gray-300 rounded p-2.5 focus:ring-2 focus:ring-blue-500 outline-none"
                                                value={newClient.dateOfBirth} onChange={e => setNewClient({...newClient, dateOfBirth: e.target.value})} />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Gender</label>
                                            <select className="w-full border border-gray-300 rounded p-2.5 bg-white" 
                                                value={newClient.gender} onChange={e => setNewClient({...newClient, gender: e.target.value as any})}>
                                                <option value="M">Male</option>
                                                <option value="F">Female</option>
                                            </select>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Marital Status</label>
                                            <select className="w-full border border-gray-300 rounded p-2.5 bg-white" 
                                                value={newClient.maritalStatus} onChange={e => setNewClient({...newClient, maritalStatus: e.target.value as any})}>
                                                <option value="SINGLE">Single</option>
                                                <option value="MARRIED">Married</option>
                                                <option value="DIVORCED">Divorced</option>
                                                <option value="WIDOWED">Widowed</option>
                                            </select>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Spouse Name</label>
                                            <input type="text" className="w-full border border-gray-300 rounded p-2.5 focus:ring-2 focus:ring-blue-500 outline-none"
                                                value={newClient.spouseName} onChange={e => setNewClient({...newClient, spouseName: e.target.value})} />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">SSNIT No.</label>
                                            <input type="text" className="w-full border border-gray-300 rounded p-2.5 focus:ring-2 focus:ring-blue-500 outline-none"
                                                value={newClient.ssnitNo} onChange={e => setNewClient({...newClient, ssnitNo: e.target.value})} />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Nationality</label>
                                            <input type="text" className="w-full border border-gray-300 rounded p-2.5 focus:ring-2 focus:ring-blue-500 outline-none"
                                                value={newClient.nationality} onChange={e => setNewClient({...newClient, nationality: e.target.value})} />
                                        </div>
                                    </>
                                )}

                                {newClient.type === 'CORPORATE' && (
                                    <>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Registration No. <span className="text-red-500">*</span></label>
                                            <input type="text" className="w-full border border-gray-300 rounded p-2.5 uppercase focus:ring-2 focus:ring-blue-500 outline-none"
                                                value={newClient.businessRegistrationNo} onChange={e => setNewClient({...newClient, businessRegistrationNo: e.target.value})} />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Tax ID (TIN) <span className="text-red-500">*</span></label>
                                            <input type="text" className="w-full border border-gray-300 rounded p-2.5 uppercase focus:ring-2 focus:ring-blue-500 outline-none"
                                                value={newClient.tin} onChange={e => setNewClient({...newClient, tin: e.target.value})} />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-bold text-gray-700 mb-1">Sector</label>
                                            <select className="w-full border border-gray-300 rounded p-2.5 bg-white" 
                                                value={newClient.sector} onChange={e => setNewClient({...newClient, sector: e.target.value})}>
                                                <option value="COMMERCE">Commerce/Trading</option>
                                                <option value="AGRICULTURE">Agriculture</option>
                                                <option value="SERVICES">Services</option>
                                                <option value="MANUFACTURING">Manufacturing</option>
                                            </select>
                                        </div>
                                    </>
                                )}
                                
                                <div>
                                    <label className="block text-sm font-bold text-gray-700 mb-1">Phone Number <span className="text-red-500">*</span></label>
                                    <input type="text" className="w-full border border-gray-300 rounded p-2.5 focus:ring-2 focus:ring-blue-500 outline-none"
                                        placeholder="020xxxxxxx"
                                        value={newClient.phone} onChange={e => setNewClient({...newClient, phone: e.target.value})} />
                                </div>

                                <div>
                                    <label className="block text-sm font-bold text-gray-700 mb-1">Email Address</label>
                                    <input type="email" className="w-full border border-gray-300 rounded p-2.5 focus:ring-2 focus:ring-blue-500 outline-none"
                                        value={newClient.email} onChange={e => setNewClient({...newClient, email: e.target.value})} />
                                </div>

                                <div>
                                    <label className="block text-sm font-bold text-gray-700 mb-1">Digital Address (GPS)</label>
                                    <input type="text" className="w-full border border-gray-300 rounded p-2.5 uppercase focus:ring-2 focus:ring-blue-500 outline-none"
                                        placeholder="XX-000-0000"
                                        value={newClient.digitalAddress} onChange={e => setNewClient({...newClient, digitalAddress: e.target.value.toUpperCase()})} />
                                </div>

                                <div>
                                    <label className="block text-sm font-bold text-gray-700 mb-1">KYC Tier</label>
                                    <select className="w-full border border-gray-300 rounded p-2.5 bg-white" 
                                        value={newClient.kycLevel} onChange={e => setNewClient({...newClient, kycLevel: e.target.value as any})}>
                                        <option value="Tier 1">Tier 1 (Low Limits)</option>
                                        <option value="Tier 2">Tier 2 (Medium Limits)</option>
                                        <option value="Tier 3">Tier 3 (Full Access)</option>
                                    </select>
                                </div>
                            </div>
                        </div>

                        <div className="bg-gray-50 p-6 border-t border-gray-200 flex justify-end gap-3">
                            <button onClick={() => setView('LIST')} className="px-6 py-2 text-gray-600 font-medium hover:bg-gray-200 rounded">Cancel</button>
                            <button onClick={handleCreateSubmit} className="px-6 py-2 bg-blue-600 text-white font-bold rounded shadow-sm hover:bg-blue-700 flex items-center gap-2">
                                <CheckCircle2 size={18} /> Complete Registration
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* --- VIEW: DETAILS (360) --- */}
            {view === 'DETAILS' && selectedClient && (
                <div className="flex h-full">
                    {/* SIDEBAR / HEADER COMBINED FOR 360 VIEW */}
                    <div className="w-64 bg-gray-50 border-r border-gray-200 flex flex-col overflow-y-auto">
                        <div className="p-6 text-center border-b border-gray-200 bg-white">
                            <div className={`w-20 h-20 rounded-full flex items-center justify-center text-3xl font-bold mx-auto mb-4 ${selectedClient.type === 'CORPORATE' ? 'bg-purple-100 text-purple-600' : 'bg-blue-100 text-blue-600'}`}>
                                {selectedClient.type === 'CORPORATE' ? <Building2 size={32}/> : selectedClient.name.charAt(0)}
                            </div>
                            <h2 className="font-bold text-gray-900 leading-tight">{selectedClient.name}</h2>
                            <p className="text-xs font-mono text-gray-500 mt-1">{selectedClient.id}</p>
                            <span className="text-[10px] text-gray-400 block mt-1">{selectedClient.type}</span>
                            <div className="flex justify-center gap-2 mt-3">
                                {renderStatusBadge('ACTIVE')}
                                {renderStatusBadge(selectedClient.riskRating)}
                            </div>
                        </div>
                        
                        <nav className="p-4 space-y-1">
                            {[
                                { id: 'OVERVIEW', icon: User, label: 'Overview' },
                                { id: 'ACCOUNTS', icon: CreditCard, label: 'Accounts', count: clientAccounts.length },
                                { id: 'LOANS', icon: Briefcase, label: 'Loans', count: clientLoans.length },
                                { id: 'TRANSACTIONS', icon: ArrowRightLeft, label: 'Transactions', count: clientTransactions.length },
                                { id: 'DOCS', icon: FileText, label: 'Documents', count: documents.length },
                                { id: 'NOTES', icon: StickyNote, label: 'CRM Notes', count: notes.length },
                            ].map(item => (
                                <button 
                                    key={item.id}
                                    onClick={() => setDetailTab(item.id as any)}
                                    className={`w-full flex items-center justify-between px-3 py-2 text-sm font-medium rounded-lg transition-colors ${
                                        detailTab === item.id ? 'bg-blue-100 text-blue-700' : 'text-gray-600 hover:bg-gray-100'
                                    }`}
                                >
                                    <div className="flex items-center gap-3">
                                        <item.icon size={16} /> {item.label}
                                    </div>
                                    {item.count !== undefined && item.id !== 'TRANSACTIONS' && (
                                        <span className="bg-gray-200 text-gray-600 text-[10px] px-1.5 py-0.5 rounded-full">{item.count}</span>
                                    )}
                                </button>
                            ))}
                        </nav>

                        <div className="mt-auto p-4 border-t border-gray-200">
                             <button className="w-full py-2 border border-gray-300 bg-white rounded text-xs font-bold text-gray-600 hover:bg-gray-50 mb-2">
                                 Reset Password / PIN
                             </button>
                             <button className="w-full py-2 border border-red-200 bg-red-50 rounded text-xs font-bold text-red-600 hover:bg-red-100">
                                 Block Client
                             </button>
                        </div>
                    </div>

                    {/* MAIN CONTENT AREA */}
                    <div className="flex-1 bg-gray-50/50 p-8 overflow-y-auto">
                        {/* TAB: OVERVIEW */}
                        {detailTab === 'OVERVIEW' && (
                            <div className="max-w-4xl space-y-6 animate-in fade-in slide-in-from-right-2 duration-300">
                                {/* ... (Stats Widgets) ... */}
                                <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                                    <div className="grid grid-cols-2 gap-y-6 gap-x-12">
                                        <div>
                                            <label className="text-xs font-bold text-gray-400 uppercase">Mobile Number</label>
                                            <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                <Smartphone size={16} className="text-gray-400"/> {selectedClient.phone}
                                            </div>
                                        </div>
                                        <div>
                                            <label className="text-xs font-bold text-gray-400 uppercase">Email Address</label>
                                            <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                <User size={16} className="text-gray-400"/> {selectedClient.email || 'N/A'}
                                            </div>
                                        </div>
                                        <div>
                                            <label className="text-xs font-bold text-gray-400 uppercase">Address</label>
                                            <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                <MapPin size={16} className="text-gray-400"/> {selectedClient.digitalAddress}
                                            </div>
                                        </div>
                                        {selectedClient.type === 'INDIVIDUAL' ? (
                                            <>
                                                <div>
                                                    <label className="text-xs font-bold text-gray-400 uppercase">National ID</label>
                                                    <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                        <Shield size={16} className="text-gray-400"/> {selectedClient.ghanaCard}
                                                    </div>
                                                </div>
                                                <div>
                                                    <label className="text-xs font-bold text-gray-400 uppercase">Date of Birth</label>
                                                    <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                        <Calendar size={16} className="text-gray-400"/> {selectedClient.dateOfBirth}
                                                    </div>
                                                </div>
                                                <div>
                                                    <label className="text-xs font-bold text-gray-400 uppercase">Marital Status</label>
                                                    <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                        <User size={16} className="text-gray-400"/> {selectedClient.maritalStatus || 'N/A'}
                                                    </div>
                                                </div>
                                                <div>
                                                    <label className="text-xs font-bold text-gray-400 uppercase">Nationality</label>
                                                    <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                        <Award size={16} className="text-gray-400"/> {selectedClient.nationality || 'N/A'}
                                                    </div>
                                                </div>
                                            </>
                                        ) : (
                                            <>
                                                <div>
                                                    <label className="text-xs font-bold text-gray-400 uppercase">Reg Number</label>
                                                    <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                        <Shield size={16} className="text-gray-400"/> {selectedClient.businessRegistrationNo}
                                                    </div>
                                                </div>
                                                <div>
                                                    <label className="text-xs font-bold text-gray-400 uppercase">TIN</label>
                                                    <div className="flex items-center gap-2 mt-1 text-gray-800 font-medium">
                                                        <FileText size={16} className="text-gray-400"/> {selectedClient.tin}
                                                    </div>
                                                </div>
                                            </>
                                        )}
                                    </div>
                                </div>
                                {/* ... (Transactions Preview) ... */}
                            </div>
                        )}
                        {/* ... (Other Tabs remain the same but handle data contextually) ... */}
                        {detailTab === 'ACCOUNTS' && (
                            /* Reusing existing logic */
                            <div className="space-y-6">
                                <div className="flex justify-between items-center">
                                    <h3 className="text-lg font-bold text-gray-800">Financial Accounts</h3>
                                    <button onClick={() => setShowAccountModal(true)} className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-bold flex items-center gap-2">
                                        <Plus size={16}/> Open New Account
                                    </button>
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    {clientAccounts.map(acc => (
                                        <div key={acc.id} onClick={() => openStatement(acc)} className="bg-white p-5 rounded-lg border border-gray-200 shadow-sm cursor-pointer hover:border-blue-300">
                                            <div className="flex justify-between items-start mb-4">
                                                <div>
                                                    <h4 className="font-bold text-gray-800">{acc.type}</h4>
                                                    <p className="text-xs text-gray-500 font-mono">{acc.id}</p>
                                                </div>
                                                {renderStatusBadge(acc.status)}
                                            </div>
                                            <div className="text-2xl font-bold text-gray-900 mb-1">
                                                {acc.balance.toLocaleString('en-US', { style: 'currency', currency: 'GHS' })}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                        {detailTab === 'LOANS' && (
                            <div className="space-y-6">
                                <h3 className="text-lg font-bold text-gray-800">Credit Facilities</h3>
                                <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
                                     <table className="w-full text-sm text-left">
                                        <thead className="bg-gray-50 text-gray-500 border-b border-gray-200">
                                            <tr>
                                                <th className="p-4">Loan ID</th>
                                                <th className="p-4">Product</th>
                                                <th className="p-4 text-right">Outstanding</th>
                                                <th className="p-4 text-center">Arrears</th>
                                                <th className="p-4 text-center">Status</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y divide-gray-100">
                                            {clientLoans.map(loan => (
                                                <tr key={loan.id}>
                                                    <td className="p-4 font-mono text-blue-600">{loan.id}</td>
                                                    <td className="p-4">{loan.productName}</td>
                                                    <td className="p-4 text-right font-bold">{loan.outstandingBalance.toLocaleString()}</td>
                                                    <td className="p-4 text-center">{loan.parBucket}</td>
                                                    <td className="p-4 text-center">{renderStatusBadge(loan.status)}</td>
                                                </tr>
                                            ))}
                                        </tbody>
                                     </table>
                                </div>
                            </div>
                        )}
                        {detailTab === 'TRANSACTIONS' && (
                            <div className="space-y-6">
                                <h3 className="text-lg font-bold text-gray-800">Recent Transactions</h3>
                                <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
                                    {clientTransactions.length === 0 ? (
                                        <div className="p-8 text-sm text-gray-500">No transaction history is available for this customer yet.</div>
                                    ) : (
                                        <ClientTransactionHistory transactions={clientTransactions} />
                                    )}
                                </div>
                            </div>
                        )}
                        {detailTab === 'DOCS' && (
                            <div className="space-y-6">
                                <div className="flex justify-between items-center">
                                    <h3 className="text-lg font-bold text-gray-800">Customer Documents</h3>
                                    <button onClick={() => setShowUploadModal(true)} className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-bold flex items-center gap-2">
                                        <Upload size={16}/> Add Document Record
                                    </button>
                                </div>
                                <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
                                    {documents.length === 0 ? (
                                        <div className="p-8 text-sm text-gray-500">No document records have been saved for this customer yet.</div>
                                    ) : (
                                        <table className="w-full text-sm text-left">
                                            <thead className="bg-gray-50 text-gray-500 border-b border-gray-200">
                                                <tr>
                                                    <th className="p-4">Type</th>
                                                    <th className="p-4">Name</th>
                                                    <th className="p-4">Uploaded</th>
                                                    <th className="p-4 text-center">Status</th>
                                                </tr>
                                            </thead>
                                            <tbody className="divide-y divide-gray-100">
                                                {documents.map((document) => (
                                                    <tr key={document.id}>
                                                        <td className="p-4 font-medium text-gray-800">{document.type}</td>
                                                        <td className="p-4 text-gray-600">{document.name}</td>
                                                        <td className="p-4 text-gray-600">{document.uploadDate}</td>
                                                        <td className="p-4 text-center">{renderStatusBadge(document.status)}</td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    )}
                                </div>
                            </div>
                        )}
                        {detailTab === 'NOTES' && (
                            <div className="space-y-6">
                                <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-5">
                                    <h3 className="text-lg font-bold text-gray-800 mb-4">CRM Notes</h3>
                                    <div className="flex gap-3">
                                        <input
                                            type="text"
                                            value={noteInput}
                                            onChange={(e) => setNoteInput(e.target.value)}
                                            placeholder="Add a client note"
                                            className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none"
                                        />
                                        <button onClick={handleAddNote} className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-bold flex items-center gap-2">
                                            <Save size={16}/> Save Note
                                        </button>
                                    </div>
                                </div>
                                <div className="space-y-3">
                                    {notes.length === 0 ? (
                                        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-8 text-sm text-gray-500">No CRM notes have been recorded for this customer yet.</div>
                                    ) : notes.map((note) => (
                                        <div key={note.id} className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
                                            <div className="flex items-center justify-between gap-4 mb-2">
                                                <div>
                                                    <p className="font-semibold text-gray-800">{note.author}</p>
                                                    <p className="text-xs text-gray-500">{new Date(note.date).toLocaleString()}</p>
                                                </div>
                                                <span className="rounded-full bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-600">{note.category}</span>
                                            </div>
                                            <p className="text-sm text-gray-700">{note.text}</p>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            )}
        </div>
    </div>
  );
};

export default ClientManager;











