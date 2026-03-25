import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertCircle,
  CheckCircle2,
  CreditCard,
  FileText,
  Filter,
  Loader2,
  Search,
  StickyNote,
  Upload,
  User,
  UserPlus,
  Wallet,
  X,
} from 'lucide-react';
import { Account, ClientDocument, ClientNote, Customer, Loan, Product, Transaction } from '../types';
import { customerService, CustomerKycStatus } from '../src/services/customerService';

interface ClientManagerProps {
  customers: Customer[];
  accounts: Account[];
  loans: Loan[];
  transactions: Transaction[];
  products?: Product[];
  onCreateCustomer: (data: Partial<Customer>) => Promise<unknown> | unknown;
  onUpdateCustomer: (id: string, data: Partial<Customer>) => Promise<unknown> | unknown;
  onCreateAccount: (cif: string, product: string, type: Account['type']) => Promise<unknown> | unknown;
  initialView?: 'LIST' | 'CREATE' | 'DETAILS';
  initialDetailTab?: 'OVERVIEW' | 'ACCOUNTS' | 'LOANS' | 'TRANSACTIONS' | 'DOCS' | 'NOTES';
}

type ClientView = 'LIST' | 'CREATE' | 'DETAILS';
type DetailTab = 'OVERVIEW' | 'ACCOUNTS' | 'LOANS' | 'TRANSACTIONS' | 'DOCS' | 'NOTES';

const emptyKyc: CustomerKycStatus = {
  customerId: '',
  kycLevel: 'TIER1',
  transactionLimit: 0,
  dailyLimit: 0,
  remainingDailyLimit: 0,
  isUnlimited: false,
  ghanaCardMatchesProfile: false,
  todayPostedTotal: 0,
};

const newClientTemplate = (): Partial<Customer> => ({
  type: 'INDIVIDUAL',
  name: '',
  phone: '',
  email: '',
  ghanaCard: '',
  digitalAddress: '',
  kycLevel: 'Tier 1',
  riskRating: 'Low',
  businessRegistrationNo: '',
  tin: '',
});

const money = new Intl.NumberFormat('en-GH', { style: 'currency', currency: 'GHS', minimumFractionDigits: 2 });

const badgeTone = (value?: string) => {
  const normalized = String(value || '').toUpperCase();
  if (['ACTIVE', 'LOW', 'VERIFIED'].includes(normalized)) return 'bg-emerald-50 text-emerald-700 border-emerald-200';
  if (['PENDING', 'MEDIUM', 'DORMANT'].includes(normalized)) return 'bg-amber-50 text-amber-700 border-amber-200';
  if (['HIGH', 'FROZEN', 'REJECTED', 'WRITTEN_OFF'].includes(normalized)) return 'bg-red-50 text-red-700 border-red-200';
  return 'bg-slate-50 text-slate-700 border-slate-200';
};

export default function ClientManager({
  customers,
  accounts,
  loans,
  transactions,
  products = [],
  onCreateCustomer,
  onUpdateCustomer,
  onCreateAccount,
  initialView = 'LIST',
  initialDetailTab = 'OVERVIEW',
}: ClientManagerProps) {
  const [view, setView] = useState<ClientView>(initialView);
  const [detailTab, setDetailTab] = useState<DetailTab>(initialDetailTab);
  const [selectedClientId, setSelectedClientId] = useState<string | null>(null);
  const [selectedClient, setSelectedClient] = useState<Customer | null>(null);
  const [kycStatus, setKycStatus] = useState<CustomerKycStatus>(emptyKyc);
  const [notes, setNotes] = useState<ClientNote[]>([]);
  const [documents, setDocuments] = useState<ClientDocument[]>([]);
  const [isProfileLoading, setIsProfileLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [showAccountModal, setShowAccountModal] = useState(false);
  const [showDocumentModal, setShowDocumentModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterTier, setFilterTier] = useState('ALL');
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [newClient, setNewClient] = useState<Partial<Customer>>(newClientTemplate());
  const [editFormData, setEditFormData] = useState<Partial<Customer>>({});
  const [noteInput, setNoteInput] = useState('');
  const [newDoc, setNewDoc] = useState({ type: 'ID Card', name: '' });
  const [newAccountProduct, setNewAccountProduct] = useState('');

  useEffect(() => setView(initialView), [initialView]);
  useEffect(() => setDetailTab(initialDetailTab), [initialDetailTab]);

  const filteredClients = useMemo(() => {
    const query = searchTerm.trim().toLowerCase();
    return customers.filter((client) => {
      const matchesSearch =
        !query ||
        client.name.toLowerCase().includes(query) ||
        client.id.toLowerCase().includes(query) ||
        client.phone.toLowerCase().includes(query) ||
        client.ghanaCard.toLowerCase().includes(query);
      return matchesSearch && (filterTier === 'ALL' || client.kycLevel === filterTier);
    });
  }, [customers, filterTier, searchTerm]);

  const clientAccounts = useMemo(
    () => (selectedClient ? accounts.filter((account) => account.cif === selectedClient.id) : []),
    [accounts, selectedClient],
  );
  const clientLoans = useMemo(
    () => (selectedClient ? loans.filter((loan) => loan.cif === selectedClient.id) : []),
    [loans, selectedClient],
  );
  const clientTransactions = useMemo(() => {
    if (!selectedClient) return [];
    const accountIds = new Set(clientAccounts.map((account) => account.id));
    return transactions.filter((transaction) => accountIds.has(transaction.accountId));
  }, [clientAccounts, selectedClient, transactions]);
  const depositProducts = useMemo(
    () => products.filter((product) => ['SAVINGS', 'CURRENT', 'FIXED_DEPOSIT'].includes(product.type) && product.status === 'ACTIVE'),
    [products],
  );

  useEffect(() => {
    if (!selectedClientId) {
      setSelectedClient(null);
      setKycStatus(emptyKyc);
      setNotes([]);
      setDocuments([]);
      return;
    }

    let active = true;
    setIsProfileLoading(true);
    Promise.all([customerService.getCustomerProfile(selectedClientId), customerService.getCustomerKyc(selectedClientId)])
      .then(([profile, kyc]) => {
        if (!active) return;
        setSelectedClient(profile);
        setKycStatus(kyc);
        setNotes(profile.notes || []);
        setDocuments(profile.documents || []);
        setEditFormData({
          name: profile.name,
          phone: profile.phone,
          email: profile.email,
          digitalAddress: profile.digitalAddress,
          riskRating: profile.riskRating,
        });
      })
      .catch((err) => active && setError(err instanceof Error ? err.message : 'Unable to load the client profile.'))
      .finally(() => active && setIsProfileLoading(false));

    return () => {
      active = false;
    };
  }, [selectedClientId]);

  const openClient = (id: string) => {
    setSelectedClientId(id);
    setView('DETAILS');
    setDetailTab('OVERVIEW');
    setError(null);
    setFeedback(null);
  };

  const validateDraft = (draft: Partial<Customer>) => {
    if (!draft.name?.trim()) return 'Client name is required.';
    if (!draft.phone?.trim()) return 'Mobile number is required.';
    if (draft.type === 'INDIVIDUAL' && !draft.ghanaCard?.trim()) return 'Ghana Card number is required for individual clients.';
    if (draft.type === 'CORPORATE' && !draft.businessRegistrationNo?.trim()) return 'Business registration number is required for corporate clients.';
    return null;
  };

  const saveClient = async () => {
    const validationError = validateDraft(newClient);
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSaving(true);
    setError(null);
    try {
      await Promise.resolve(onCreateCustomer(newClient));
      setFeedback('Client profile created successfully.');
      setNewClient(newClientTemplate());
      setView('LIST');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to create the client profile.');
    } finally {
      setIsSaving(false);
    }
  };

  const updateProfile = async () => {
    if (!selectedClient || !editFormData.name?.trim()) {
      setError('Client name is required before saving changes.');
      return;
    }

    setIsSaving(true);
    setError(null);
    try {
      await Promise.resolve(onUpdateCustomer(selectedClient.id, editFormData));
      setSelectedClient((current) => (current ? { ...current, ...editFormData } : current));
      setFeedback('Client profile updated successfully.');
      setIsEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to update the client profile.');
    } finally {
      setIsSaving(false);
    }
  };

  const addNote = async () => {
    if (!selectedClient || !noteInput.trim()) return;
    setIsSaving(true);
    try {
      const created = await customerService.addCustomerNote(selectedClient.id, noteInput.trim(), 'GENERAL');
      setNotes((current) => [created, ...current]);
      setNoteInput('');
      setFeedback('Client note saved.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to save the client note.');
    } finally {
      setIsSaving(false);
    }
  };

  const addDocument = async () => {
    if (!selectedClient || !newDoc.name.trim()) return;
    setIsSaving(true);
    try {
      const created = await customerService.addCustomerDocument(selectedClient.id, newDoc.type, newDoc.name.trim());
      setDocuments((current) => [created, ...current]);
      setNewDoc({ type: 'ID Card', name: '' });
      setShowDocumentModal(false);
      setFeedback('Document record added successfully.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to save the document record.');
    } finally {
      setIsSaving(false);
    }
  };

  const submitAccountOpening = async () => {
    if (!selectedClient || !newAccountProduct) {
      setError('Select a product before opening the account.');
      return;
    }
    const selectedProduct = depositProducts.find((product) => product.id === newAccountProduct);
    if (!selectedProduct) {
      setError('The selected product is no longer available.');
      return;
    }

    setIsSaving(true);
    try {
      await Promise.resolve(onCreateAccount(selectedClient.id, selectedProduct.id, selectedProduct.type as Account['type']));
      setShowAccountModal(false);
      setNewAccountProduct('');
      setFeedback(`Account opening submitted using ${selectedProduct.name}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to open the account.');
    } finally {
      setIsSaving(false);
    }
  };

  const overviewCards = selectedClient ? [
    { label: 'Mobile Number', value: selectedClient.phone || 'Not recorded' },
    { label: 'Email Address', value: selectedClient.email || 'Not recorded' },
    { label: 'Digital Address', value: selectedClient.digitalAddress || 'Not recorded' },
    { label: 'Identity', value: selectedClient.ghanaCard || selectedClient.businessRegistrationNo || 'Not recorded' },
  ] : [];

  return (
    <div className="min-h-full space-y-6 p-4 sm:p-6">
      {error && (
        <div className="flex items-start gap-3 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          <AlertCircle className="mt-0.5 h-5 w-5 flex-shrink-0" />
          <p>{error}</p>
        </div>
      )}
      {feedback && (
        <div className="flex items-start gap-3 rounded-2xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800">
          <CheckCircle2 className="mt-0.5 h-5 w-5 flex-shrink-0" />
          <p>{feedback}</p>
        </div>
      )}

      {view === 'LIST' && (
        <>
          <div className="dashboard-sheen rounded-2xl border border-slate-200 p-6 text-slate-900 shadow-soft">
            <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
              <div>
                <p className="mb-1 text-[11px] font-accent uppercase tracking-[0.24em] text-slate-500">Client Operations</p>
                <h1 className="text-3xl font-heading font-semibold tracking-[-0.04em] text-slate-900">Client Workbench</h1>
                <p className="mt-2 max-w-3xl text-sm text-slate-600">Manage onboarding, KYC posture, document records, and product-ready client servicing from one governed workspace.</p>
              </div>
              <button type="button" onClick={() => setView('CREATE')} className="inline-flex items-center gap-2 rounded-full bg-brand-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-brand-700">
                <UserPlus size={16} />
                New client onboarding
              </button>
            </div>
          </div>

          <div className="glass-card rounded-[28px] border border-white/70 p-6">
            <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
              <div className="flex flex-1 items-center gap-3 rounded-[22px] border border-slate-200 bg-white px-4 py-3">
                <Search className="h-4 w-4 text-slate-400" />
                <input value={searchTerm} onChange={(event) => setSearchTerm(event.target.value)} placeholder="Search by client name, CIF, mobile number, or Ghana Card" className="w-full bg-transparent text-sm text-slate-700 outline-none placeholder:text-slate-400" />
              </div>
              <div className="inline-flex items-center gap-3 rounded-[22px] border border-slate-200 bg-white px-4 py-3">
                <Filter className="h-4 w-4 text-slate-400" />
                <select value={filterTier} onChange={(event) => setFilterTier(event.target.value)} className="bg-transparent text-sm text-slate-700 outline-none">
                  <option value="ALL">All KYC tiers</option>
                  <option value="Tier 1">Tier 1</option>
                  <option value="Tier 2">Tier 2</option>
                  <option value="Tier 3">Tier 3</option>
                </select>
              </div>
            </div>

            <div className="mt-6 overflow-hidden rounded-[24px] border border-slate-200">
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-slate-200 text-sm">
                  <thead className="bg-slate-50">
                    <tr>
                      <th className="px-4 py-3 text-left font-semibold text-slate-700">Client</th>
                      <th className="px-4 py-3 text-left font-semibold text-slate-700">Identity</th>
                      <th className="px-4 py-3 text-left font-semibold text-slate-700">Contact</th>
                      <th className="px-4 py-3 text-left font-semibold text-slate-700">KYC / Risk</th>
                      <th className="px-4 py-3 text-right font-semibold text-slate-700">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 bg-white">
                    {filteredClients.map((client) => (
                      <tr key={client.id}>
                        <td className="px-4 py-4">
                          <p className="font-semibold text-slate-900">{client.name}</p>
                          <p className="text-xs text-slate-500">{client.id}</p>
                        </td>
                        <td className="px-4 py-4 text-slate-600">
                          <p>{client.type || 'INDIVIDUAL'}</p>
                          <p className="text-xs text-slate-500">{client.ghanaCard || client.businessRegistrationNo || 'Identity pending'}</p>
                        </td>
                        <td className="px-4 py-4 text-slate-600">
                          <p>{client.phone || 'No mobile number'}</p>
                          <p className="text-xs text-slate-500">{client.email || 'No email recorded'}</p>
                        </td>
                        <td className="px-4 py-4">
                          <div className="flex flex-wrap gap-2">
                            <span className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${badgeTone(client.kycLevel)}`}>{client.kycLevel}</span>
                            <span className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${badgeTone(client.riskRating)}`}>{client.riskRating}</span>
                          </div>
                        </td>
                        <td className="px-4 py-4 text-right">
                          <button type="button" onClick={() => openClient(client.id)} className="rounded-full border border-slate-300 bg-white px-3.5 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50">Open profile</button>
                        </td>
                      </tr>
                    ))}
                    {filteredClients.length === 0 && (
                      <tr>
                        <td colSpan={5} className="px-4 py-8 text-center text-sm text-slate-500">No client profiles match the current filters.</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </>
      )}

      {view === 'CREATE' && (
        <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
          <div className="glass-card rounded-[28px] border border-white/70 p-6">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-[11px] font-accent uppercase tracking-[0.22em] text-slate-500">Client Onboarding</p>
                <h2 className="mt-1 text-2xl font-heading font-bold text-slate-900">Create Client Profile</h2>
              </div>
              <button type="button" onClick={() => setView('LIST')} className="rounded-full border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50">Back to list</button>
            </div>

            <div className="mt-6 grid gap-5 md:grid-cols-2">
              <select value={newClient.type} onChange={(event) => setNewClient((current) => ({ ...current, type: event.target.value as Customer['type'] }))} className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500">
                <option value="INDIVIDUAL">Individual</option>
                <option value="CORPORATE">Corporate</option>
              </select>
              <input value={newClient.name || ''} onChange={(event) => setNewClient((current) => ({ ...current, name: event.target.value }))} placeholder="Full legal name" className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
              <input value={newClient.phone || ''} onChange={(event) => setNewClient((current) => ({ ...current, phone: event.target.value }))} placeholder="Mobile number" className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
              <input value={newClient.email || ''} onChange={(event) => setNewClient((current) => ({ ...current, email: event.target.value }))} placeholder="Email address" className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
              <input value={newClient.type === 'CORPORATE' ? (newClient.businessRegistrationNo || '') : (newClient.ghanaCard || '')} onChange={(event) => setNewClient((current) => current.type === 'CORPORATE' ? { ...current, businessRegistrationNo: event.target.value } : { ...current, ghanaCard: event.target.value })} placeholder={newClient.type === 'CORPORATE' ? 'Business registration number' : 'Ghana Card number'} className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
              <input value={newClient.type === 'CORPORATE' ? (newClient.tin || '') : (newClient.digitalAddress || '')} onChange={(event) => setNewClient((current) => current.type === 'CORPORATE' ? { ...current, tin: event.target.value } : { ...current, digitalAddress: event.target.value })} placeholder={newClient.type === 'CORPORATE' ? 'TIN' : 'Digital address'} className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
              <select value={newClient.kycLevel} onChange={(event) => setNewClient((current) => ({ ...current, kycLevel: event.target.value as Customer['kycLevel'] }))} className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500">
                <option value="Tier 1">Tier 1</option>
                <option value="Tier 2">Tier 2</option>
                <option value="Tier 3">Tier 3</option>
              </select>
            </div>

            <div className="mt-6 flex justify-end gap-3">
              <button type="button" onClick={() => setNewClient(newClientTemplate())} className="rounded-full border border-slate-300 px-5 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50">Reset</button>
              <button type="button" onClick={saveClient} disabled={isSaving} className="inline-flex items-center gap-2 rounded-full bg-brand-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:bg-slate-300">
                {isSaving ? <Loader2 size={16} className="animate-spin" /> : <Save size={16} />}
                Save client profile
              </button>
            </div>
          </div>

          <div className="glass-card rounded-[28px] border border-white/70 p-6">
            <p className="text-[11px] font-accent uppercase tracking-[0.22em] text-slate-500">Onboarding Guidance</p>
            <h3 className="mt-1 text-xl font-heading font-bold text-slate-900">Production Controls</h3>
            <div className="mt-4 space-y-3 text-sm text-slate-600">
              <p>Capture a complete legal name and mobile number before saving the client profile.</p>
              <p>For individual clients, Ghana Card information should be captured before account opening and transaction servicing.</p>
              <p>Corporate profiles should include registration and TIN details before moving to product opening or loan origination.</p>
            </div>
          </div>
        </div>
      )}

      {view === 'DETAILS' && (
        <div className="grid gap-6 xl:grid-cols-[300px_1fr]">
          <div className="glass-card rounded-[28px] border border-white/70 p-5">
            <button type="button" onClick={() => setView('LIST')} className="mb-4 rounded-full border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50">Back to list</button>
            <div className="rounded-[24px] bg-slate-950 px-5 py-6 text-white">
              <p className="text-xs uppercase tracking-[0.18em] text-white/60">{selectedClient?.id || 'No client selected'}</p>
              <h2 className="mt-2 text-2xl font-heading font-bold">{selectedClient?.name || 'Client Profile'}</h2>
              <div className="mt-4 flex flex-wrap gap-2">
                <span className="rounded-full bg-white/10 px-2.5 py-1 text-xs font-semibold">{selectedClient?.kycLevel || 'Tier 1'}</span>
                <span className="rounded-full bg-white/10 px-2.5 py-1 text-xs font-semibold">{selectedClient?.riskRating || 'Low'}</span>
              </div>
            </div>
            <div className="mt-4 grid gap-3">
              <div className="rounded-[20px] border border-slate-200 bg-white px-4 py-3">
                <p className="text-[11px] uppercase tracking-[0.18em] text-slate-500">Daily Limit Remaining</p>
                <p className="mt-1 text-lg font-semibold text-slate-900">{kycStatus.isUnlimited ? 'Unlimited' : money.format(kycStatus.remainingDailyLimit)}</p>
              </div>
              <div className="rounded-[20px] border border-slate-200 bg-white px-4 py-3">
                <p className="text-[11px] uppercase tracking-[0.18em] text-slate-500">Today Posted Total</p>
                <p className="mt-1 text-lg font-semibold text-slate-900">{money.format(kycStatus.todayPostedTotal)}</p>
              </div>
            </div>
            <div className="mt-5 space-y-1">
              {[
                { id: 'OVERVIEW', label: 'Overview', icon: User },
                { id: 'ACCOUNTS', label: 'Accounts', icon: CreditCard },
                { id: 'LOANS', label: 'Loans', icon: Wallet },
                { id: 'TRANSACTIONS', label: 'Transactions', icon: Filter },
                { id: 'DOCS', label: 'Documents', icon: FileText },
                { id: 'NOTES', label: 'Notes', icon: StickyNote },
              ].map((item) => (
                <button key={item.id} type="button" onClick={() => setDetailTab(item.id as DetailTab)} className={`flex w-full items-center gap-3 rounded-2xl px-4 py-3 text-sm font-medium transition ${detailTab === item.id ? 'bg-brand-600 text-white' : 'text-slate-700 hover:bg-slate-100'}`}>
                  <item.icon size={16} />
                  {item.label}
                </button>
              ))}
            </div>
          </div>

          <div className="glass-card rounded-[28px] border border-white/70 p-6">
            {isProfileLoading ? (
              <div className="flex min-h-[300px] items-center justify-center"><Loader2 className="h-6 w-6 animate-spin text-brand-600" /></div>
            ) : !selectedClient ? (
              <div className="rounded-[24px] border border-slate-200 bg-slate-50 p-6 text-sm text-slate-500">Select a client profile from the list to continue.</div>
            ) : (
              <>
                {detailTab === 'OVERVIEW' && (
                  <div className="space-y-6">
                    <div className="flex items-center justify-between gap-4">
                      <div>
                        <h3 className="text-xl font-heading font-bold text-slate-900">Client Profile Overview</h3>
                        <p className="mt-1 text-sm text-slate-500">Identity, contact, and KYC posture for production servicing.</p>
                      </div>
                      <button type="button" onClick={() => setIsEditing((current) => !current)} className="rounded-full border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50">{isEditing ? 'Cancel edit' : 'Edit profile'}</button>
                    </div>

                    <div className="grid gap-4 md:grid-cols-2">
                      {overviewCards.map((item) => (
                        <div key={item.label} className="rounded-[22px] border border-slate-200 bg-white p-4">
                          <p className="text-xs uppercase tracking-[0.18em] text-slate-500">{item.label}</p>
                          <p className="mt-3 text-sm font-semibold text-slate-900">{item.value}</p>
                        </div>
                      ))}
                    </div>

                    {isEditing && (
                      <div className="rounded-[24px] border border-slate-200 bg-slate-50 p-5">
                        <div className="grid gap-4 md:grid-cols-2">
                          <input value={editFormData.name || ''} onChange={(event) => setEditFormData((current) => ({ ...current, name: event.target.value }))} placeholder="Client name" className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
                          <input value={editFormData.phone || ''} onChange={(event) => setEditFormData((current) => ({ ...current, phone: event.target.value }))} placeholder="Mobile number" className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
                          <input value={editFormData.email || ''} onChange={(event) => setEditFormData((current) => ({ ...current, email: event.target.value }))} placeholder="Email address" className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
                          <input value={editFormData.digitalAddress || ''} onChange={(event) => setEditFormData((current) => ({ ...current, digitalAddress: event.target.value }))} placeholder="Digital address" className="rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
                        </div>
                        <div className="mt-4 flex justify-end">
                          <button type="button" onClick={updateProfile} disabled={isSaving} className="inline-flex items-center gap-2 rounded-full bg-brand-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:bg-slate-300">
                            {isSaving ? <Loader2 size={16} className="animate-spin" /> : <User size={16} />}
                            Save changes
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                )}

                {detailTab === 'ACCOUNTS' && (
                  <div className="space-y-6">
                    <div className="flex items-center justify-between gap-4">
                      <div>
                        <h3 className="text-xl font-heading font-bold text-slate-900">Client Accounts</h3>
                        <p className="mt-1 text-sm text-slate-500">Accounts linked to this client profile and product-ready account opening.</p>
                      </div>
                      <button type="button" onClick={() => setShowAccountModal(true)} className="inline-flex items-center gap-2 rounded-full bg-brand-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-brand-700">
                        <Plus size={16} />
                        Open account
                      </button>
                    </div>
                    <div className="grid gap-4 md:grid-cols-2">
                      {clientAccounts.map((account) => (
                        <div key={account.id} className="rounded-[24px] border border-slate-200 bg-white p-5">
                          <div className="flex items-start justify-between gap-4">
                            <div>
                              <p className="text-sm font-semibold text-slate-900">{account.type}</p>
                              <p className="text-xs text-slate-500">{account.id}</p>
                            </div>
                            <span className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${badgeTone(account.status)}`}>{account.status}</span>
                          </div>
                          <p className="mt-4 text-2xl font-bold text-slate-900">{money.format(account.balance)}</p>
                        </div>
                      ))}
                      {clientAccounts.length === 0 && <div className="rounded-[24px] border border-slate-200 bg-slate-50 p-6 text-sm text-slate-500">No accounts are linked to this client yet.</div>}
                    </div>
                  </div>
                )}

                {detailTab === 'LOANS' && (
                  <div className="space-y-4">
                    <h3 className="text-xl font-heading font-bold text-slate-900">Loan Facilities</h3>
                    <div className="overflow-hidden rounded-[24px] border border-slate-200">
                      <table className="min-w-full divide-y divide-slate-200 text-sm">
                        <thead className="bg-slate-50">
                          <tr>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Loan</th>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Product</th>
                            <th className="px-4 py-3 text-right font-semibold text-slate-700">Outstanding</th>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Status</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 bg-white">
                          {clientLoans.map((loan) => (
                            <tr key={loan.id}>
                              <td className="px-4 py-3 font-medium text-slate-900">{loan.id}</td>
                              <td className="px-4 py-3 text-slate-600">{loan.productName}</td>
                              <td className="px-4 py-3 text-right font-semibold text-slate-900">{money.format(loan.outstandingBalance)}</td>
                              <td className="px-4 py-3"><span className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${badgeTone(loan.status)}`}>{loan.status}</span></td>
                            </tr>
                          ))}
                          {clientLoans.length === 0 && <tr><td colSpan={4} className="px-4 py-8 text-center text-sm text-slate-500">No loan facilities are linked to this client.</td></tr>}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {detailTab === 'TRANSACTIONS' && (
                  <div className="space-y-4">
                    <h3 className="text-xl font-heading font-bold text-slate-900">Recent Transactions</h3>
                    <div className="overflow-hidden rounded-[24px] border border-slate-200">
                      <table className="min-w-full divide-y divide-slate-200 text-sm">
                        <thead className="bg-slate-50">
                          <tr>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Type</th>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Account</th>
                            <th className="px-4 py-3 text-right font-semibold text-slate-700">Amount</th>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Status</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 bg-white">
                          {clientTransactions.map((transaction) => (
                            <tr key={transaction.id}>
                              <td className="px-4 py-3 text-slate-900">{transaction.type}</td>
                              <td className="px-4 py-3 text-slate-600">{transaction.accountId}</td>
                              <td className="px-4 py-3 text-right font-semibold text-slate-900">{money.format(transaction.amount)}</td>
                              <td className="px-4 py-3"><span className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${badgeTone(transaction.status)}`}>{transaction.status}</span></td>
                            </tr>
                          ))}
                          {clientTransactions.length === 0 && <tr><td colSpan={4} className="px-4 py-8 text-center text-sm text-slate-500">No transactions are available for this client.</td></tr>}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {detailTab === 'DOCS' && (
                  <div className="space-y-4">
                    <div className="flex items-center justify-between gap-4">
                      <div>
                        <h3 className="text-xl font-heading font-bold text-slate-900">Document Register</h3>
                        <p className="mt-1 text-sm text-slate-500">Track document records and verification status for the client profile.</p>
                      </div>
                      <button type="button" onClick={() => setShowDocumentModal(true)} className="inline-flex items-center gap-2 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50">
                        <Upload size={16} />
                        Add document
                      </button>
                    </div>
                    <div className="overflow-hidden rounded-[24px] border border-slate-200">
                      <table className="min-w-full divide-y divide-slate-200 text-sm">
                        <thead className="bg-slate-50">
                          <tr>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Type</th>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Name</th>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Uploaded</th>
                            <th className="px-4 py-3 text-left font-semibold text-slate-700">Status</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 bg-white">
                          {documents.map((document) => (
                            <tr key={document.id}>
                              <td className="px-4 py-3 text-slate-900">{document.type}</td>
                              <td className="px-4 py-3 text-slate-600">{document.name}</td>
                              <td className="px-4 py-3 text-slate-600">{document.uploadDate}</td>
                              <td className="px-4 py-3"><span className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${badgeTone(document.status)}`}>{document.status}</span></td>
                            </tr>
                          ))}
                          {documents.length === 0 && <tr><td colSpan={4} className="px-4 py-8 text-center text-sm text-slate-500">No document records have been captured yet.</td></tr>}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {detailTab === 'NOTES' && (
                  <div className="space-y-4">
                    <h3 className="text-xl font-heading font-bold text-slate-900">Client Notes</h3>
                    <div className="rounded-[24px] border border-slate-200 bg-slate-50 p-4">
                      <div className="flex flex-col gap-3 md:flex-row">
                        <input value={noteInput} onChange={(event) => setNoteInput(event.target.value)} placeholder="Add a servicing or relationship note" className="flex-1 rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" />
                        <button type="button" onClick={addNote} disabled={isSaving || !noteInput.trim()} className="inline-flex items-center justify-center gap-2 rounded-full bg-brand-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:bg-slate-300">
                          {isSaving ? <Loader2 size={16} className="animate-spin" /> : <StickyNote size={16} />}
                          Save note
                        </button>
                      </div>
                    </div>
                    <div className="space-y-3">
                      {notes.map((note) => (
                        <div key={note.id} className="rounded-[24px] border border-slate-200 bg-white p-4">
                          <div className="flex items-center justify-between gap-4">
                            <p className="font-semibold text-slate-900">{note.author}</p>
                            <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-xs font-semibold text-slate-600">{note.category}</span>
                          </div>
                          <p className="mt-3 text-sm text-slate-700">{note.text}</p>
                        </div>
                      ))}
                      {notes.length === 0 && <div className="rounded-[24px] border border-slate-200 bg-slate-50 p-6 text-sm text-slate-500">No client notes have been recorded yet.</div>}
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}

      {showAccountModal && selectedClient && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
          <div className="w-full max-w-lg rounded-[28px] bg-white p-6 shadow-2xl">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h3 className="text-xl font-heading font-bold text-slate-900">Open Account</h3>
                <p className="mt-1 text-sm text-slate-500">Use governed product definitions to open a client account.</p>
              </div>
              <button type="button" onClick={() => setShowAccountModal(false)}><X className="h-5 w-5 text-slate-400" /></button>
            </div>
            <div className="mt-5 space-y-4">
              <div className="rounded-[20px] border border-slate-200 bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-[0.18em] text-slate-500">Client</p>
                <p className="mt-1 text-sm font-semibold text-slate-900">{selectedClient.name}</p>
              </div>
              <select value={newAccountProduct} onChange={(event) => setNewAccountProduct(event.target.value)} className="w-full rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500">
                <option value="">Select a deposit product</option>
                {depositProducts.map((product) => (
                  <option key={product.id} value={product.id}>{product.name} • {product.type}</option>
                ))}
              </select>
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button type="button" onClick={() => setShowAccountModal(false)} className="rounded-full border border-slate-300 px-5 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50">Cancel</button>
              <button type="button" onClick={submitAccountOpening} disabled={isSaving} className="inline-flex items-center gap-2 rounded-full bg-brand-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:bg-slate-300">
                {isSaving ? <Loader2 size={16} className="animate-spin" /> : <Wallet size={16} />}
                Submit account opening
              </button>
            </div>
          </div>
        </div>
      )}

      {showDocumentModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
          <div className="w-full max-w-lg rounded-[28px] bg-white p-6 shadow-2xl">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h3 className="text-xl font-heading font-bold text-slate-900">Add Document Record</h3>
                <p className="mt-1 text-sm text-slate-500">Register a document record against the client profile.</p>
              </div>
              <button type="button" onClick={() => setShowDocumentModal(false)}><X className="h-5 w-5 text-slate-400" /></button>
            </div>
            <div className="mt-5 space-y-4">
              <input value={newDoc.type} onChange={(event) => setNewDoc((current) => ({ ...current, type: event.target.value }))} className="w-full rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" placeholder="Document type" />
              <input value={newDoc.name} onChange={(event) => setNewDoc((current) => ({ ...current, name: event.target.value }))} className="w-full rounded-2xl border border-slate-300 px-4 py-3 text-sm outline-none focus:border-brand-500" placeholder="Document name" />
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button type="button" onClick={() => setShowDocumentModal(false)} className="rounded-full border border-slate-300 px-5 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50">Cancel</button>
              <button type="button" onClick={addDocument} disabled={isSaving} className="inline-flex items-center gap-2 rounded-full bg-brand-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:bg-slate-300">
                {isSaving ? <Loader2 size={16} className="animate-spin" /> : <Upload size={16} />}
                Save document
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
