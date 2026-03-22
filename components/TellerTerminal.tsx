import React, { useEffect, useMemo, useState } from 'react';
import { Account, Customer } from '../types';
import { ledgerService } from '../src/services/ledgerService';
import { customerService } from '../src/services/customerService';
import { TellerTillSummaryDto, vaultService } from '../src/services/vaultService';
import {
  AlertCircle,
  ArrowRightLeft,
  BadgeCheck,
  CheckCircle,
  FileText,
  CreditCard,
  Landmark,
  Search,
  ShieldCheck,
  User,
  UserCheck,
  Wallet,
} from 'lucide-react';
import { tellerNotesService, TellerNoteRecord } from '../src/services/tellerNotesService';

interface TellerTerminalProps {
  accounts: Account[];
  customers: Customer[];
  tellerId?: string;
  onTransaction: (
    accountId: string,
    type: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'LOAN_REPAYMENT',
    amount: number,
    narration: string,
    tellerId: string
  ) => Promise<{ success: boolean; id: string; message: string; status: 'POSTED' | 'PENDING_APPROVAL' }>;
  initialTransactionType?: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER';
  onOpenNotes?: () => void;
}

type VerificationMode = 'ACCOUNT_HOLDER' | 'THIRD_PARTY';

type VerificationDraft = {
  mode: VerificationMode;
  presenterName: string;
  presenterGhanaCard: string;
  presenterPhone: string;
  relationship: string;
  noteId: string;
};

type VerificationApiContext = {
  availableMargin: number;
  kycLevel: string;
  transactionLimit: number;
  dailyLimit: number;
  remainingDailyLimit: number;
  ghanaCardMatchesProfile: boolean;
};

const defaultVerificationDraft: VerificationDraft = {
  mode: 'ACCOUNT_HOLDER',
  presenterName: '',
  presenterGhanaCard: '',
  presenterPhone: '',
  relationship: '',
  noteId: '',
};

const GHANA_CARD_PATTERN = /^GHA-\d{9}-\d$/i;
const GRA_RATE = 0.01;
const BOG_GHANA_CARD_EFFECTIVE_DATE = 'December 1, 2025';

function formatAmount(value: number) {
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatTillStatus(status?: string) {
  return (status || 'CLOSED').replace(/_/g, ' ');
}

function normalizeCard(value: string) {
  return value.trim().toUpperCase();
}

function getTransactionLabel(type: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER') {
  return type === 'DEPOSIT' ? 'Cash Deposit' : type === 'WITHDRAWAL' ? 'Cash Withdrawal' : 'Transfer';
}

const TellerTerminal: React.FC<TellerTerminalProps> = ({ accounts, customers, tellerId = 'TLR001', onTransaction, initialTransactionType = 'DEPOSIT', onOpenNotes }) => {
  const [accountNum, setAccountNum] = useState('');
  const [selectedAccount, setSelectedAccount] = useState<Account | null>(null);
  const [amount, setAmount] = useState('');
  const [narration, setNarration] = useState('');
  const [txType, setTxType] = useState<'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER'>(initialTransactionType);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [pendingMsg, setPendingMsg] = useState<string | null>(null);
  const [verificationDraft, setVerificationDraft] = useState<VerificationDraft>(defaultVerificationDraft);
  const [verificationConfirmed, setVerificationConfirmed] = useState(false);
  const [verificationMessage, setVerificationMessage] = useState<string | null>(null);
  const [verificationContext, setVerificationContext] = useState<VerificationApiContext | null>(null);
  const [verificationLoading, setVerificationLoading] = useState(false);
  const [tillSummary, setTillSummary] = useState<TellerTillSummaryDto | null>(null);
  const [tillLoading, setTillLoading] = useState(false);
  const [availableNotes, setAvailableNotes] = useState<TellerNoteRecord[]>([]);

  useEffect(() => {
    setTxType(initialTransactionType);
  }, [initialTransactionType]);

  useEffect(() => {
    let cancelled = false;

    const loadTillSummary = async () => {
      if (!tellerId) {
        setTillSummary(null);
        return;
      }

      setTillLoading(true);
      try {
        const tillSummaries = await vaultService.getTillSummaries(undefined, 'GHS');
        if (!cancelled) {
          const matchedTill = tillSummaries.find((item) => item.tellerId === tellerId) || null;
          setTillSummary(matchedTill);
        }
      } catch (loadError) {
        if (!cancelled) {
          console.error('Failed to load teller till summary', loadError);
          setTillSummary(null);
        }
      } finally {
        if (!cancelled) {
          setTillLoading(false);
        }
      }
    };

    void loadTillSummary();

    return () => {
      cancelled = true;
    };
  }, [tellerId]);

  const customer = useMemo(
    () => (selectedAccount ? customers.find((item) => item.id === selectedAccount.cif) || null : null),
    [customers, selectedAccount]
  );

  useEffect(() => {
    setAvailableNotes(tellerNotesService.getNotesForContext(selectedAccount?.id, customer?.id));
  }, [selectedAccount, customer]);

  const tax = useMemo(() => {
    if (txType !== 'WITHDRAWAL' || !amount) return 0;
    const parsed = parseFloat(amount);
    if (Number.isNaN(parsed) || parsed <= 100) return 0;
    return parsed * GRA_RATE;
  }, [amount, txType]);

  const totalDebit = (parseFloat(amount || '0') || 0) + tax;
  const parsedAmount = parseFloat(amount || '0') || 0;
  const amountWithinTransactionLimit = !verificationContext || parsedAmount <= verificationContext.transactionLimit;
  const amountWithinDailyLimit = !verificationContext || parsedAmount <= verificationContext.remainingDailyLimit;
  const canCommit = Boolean(
    selectedAccount &&
    customer &&
    verificationConfirmed &&
    parsedAmount > 0 &&
    amountWithinTransactionLimit &&
    amountWithinDailyLimit,
  );
  const transactionLabel = getTransactionLabel(txType);
  const tillStatusTone = !tillSummary
    ? 'text-slate-600 dark:text-slate-300'
    : (tillSummary.variance || 0) === 0
      ? 'text-emerald-700 dark:text-emerald-300'
      : 'text-amber-700 dark:text-amber-200';
  const commitHint = !selectedAccount
    ? 'Lookup an account to continue.'
    : !verificationConfirmed
      ? 'Complete Ghana Card verification before posting.'
      : parsedAmount <= 0
        ? 'Enter a valid transaction amount.'
        : !amountWithinTransactionLimit
          ? 'Amount exceeds the verified KYC transaction limit.'
          : !amountWithinDailyLimit
            ? 'Amount exceeds the remaining verified daily limit.'
        : 'Ready to post to the ledger.';
  const complianceChecklist = [
    {
      label: 'Customer account loaded',
      done: Boolean(selectedAccount && customer),
      detail: selectedAccount ? `${selectedAccount.id} · ${customer?.name || 'Customer loaded'}` : 'Lookup an account to load the customer profile.',
    },
    {
      label: 'Ghana Card verification completed',
      done: verificationConfirmed,
      detail: verificationConfirmed
        ? verificationDraft.mode === 'ACCOUNT_HOLDER'
          ? 'Account holder identity matched against stored Ghana Card.'
          : 'Third-party presenter captured with linked teller note.'
        : 'Complete live Ghana Card verification before posting.',
    },
    {
      label: 'Amount is within verified KYC limits',
      done: parsedAmount > 0 && amountWithinTransactionLimit && amountWithinDailyLimit,
      detail: verificationContext
        ? `Txn limit GHS ${formatAmount(verificationContext.transactionLimit)} · Remaining daily limit GHS ${formatAmount(verificationContext.remainingDailyLimit)}`
        : 'Verify identity to load KYC limits.',
    },
  ];

  const clearMessages = () => {
    setError(null);
    setSuccessMsg(null);
    setPendingMsg(null);
  };

  const resetVerification = () => {
    setVerificationDraft(defaultVerificationDraft);
    setVerificationConfirmed(false);
    setVerificationMessage(null);
    setVerificationContext(null);
    setVerificationLoading(false);
  };
  const findAccount = () => {
    clearMessages();
    const account = accounts.find((item) => item.id === accountNum.trim());
    if (!account) {
      setSelectedAccount(null);
      resetVerification();
      setError('Account not found.');
      return;
    }

    setSelectedAccount(account);
    resetVerification();
  };

  const validateVerificationDraft = () => {
    if (!customer) {
      return { ok: false, message: 'Customer identity profile is not available for this account.' };
    }

    const presenterCard = normalizeCard(verificationDraft.presenterGhanaCard);
    if (!GHANA_CARD_PATTERN.test(presenterCard)) {
      return { ok: false, message: 'Enter a valid Ghana Card number in the format GHA-123456789-0.' };
    }

    if (!verificationDraft.presenterName.trim()) {
      return { ok: false, message: 'Presenter name is required for ID verification.' };
    }

    if (verificationDraft.mode === 'ACCOUNT_HOLDER' && !customer.ghanaCard) {
      return { ok: false, message: 'Customer does not have a Ghana Card stored on profile.' };
    }

    if (verificationDraft.mode === 'THIRD_PARTY') {
      if (!verificationDraft.relationship.trim()) {
        return { ok: false, message: 'Relationship to account holder is required for third-party cash handling.' };
      }

      if (!verificationDraft.noteId.trim()) {
        return { ok: false, message: 'Select a linked teller note for third-party cash handling.' };
      }
    }

    return { ok: true, message: '' };
  };

  const handleVerifyIdentity = async () => {
    clearMessages();
    const draftValidation = validateVerificationDraft();
    if (!draftValidation.ok || !customer) {
      setVerificationConfirmed(false);
      setVerificationContext(null);
      setVerificationMessage(draftValidation.message);
      setError(draftValidation.message);
      return;
    }

    const presenterCard = normalizeCard(verificationDraft.presenterGhanaCard);
    setVerificationLoading(true);
    setVerificationConfirmed(false);
    setVerificationContext(null);

    try {
      const [isValid, availableMargin, kycLimits] = await Promise.all([
        ledgerService.validateCustomerGhanaCard(customer.id, presenterCard),
        ledgerService.getAvailableMargin(customer.id),
        customerService.getCustomerKyc(customer.id),
      ]);

      const apiContext: VerificationApiContext = {
        availableMargin,
        kycLevel: kycLimits.kycLevel,
        transactionLimit: kycLimits.transactionLimit,
        dailyLimit: kycLimits.dailyLimit,
        remainingDailyLimit: kycLimits.remainingDailyLimit,
        ghanaCardMatchesProfile: kycLimits.ghanaCardMatchesProfile,
      };
      setVerificationContext(apiContext);

      if (verificationDraft.mode === 'ACCOUNT_HOLDER') {
        if (!isValid) {
          const message = 'Margins/KYC API could not verify the Ghana Card against the customer profile.';
          setVerificationMessage(message);
          setVerificationConfirmed(false);
          setError(message);
          return;
        }

        const message = `Ghana Card verified for ${customer.name}. KYC ${apiContext.kycLevel}; transaction limit GHS ${formatAmount(apiContext.transactionLimit)}; remaining daily limit GHS ${formatAmount(apiContext.remainingDailyLimit)}.`;
        setVerificationMessage(message);
        setVerificationConfirmed(true);
        return;
      }

      const message = 'Third-party presenter captured. Customer profile was checked live through the Margins/KYC API; a linked teller note is required for cash handling.';
      setVerificationMessage(message);
      setVerificationConfirmed(true);
    } catch (verificationError: any) {
      const message = verificationError?.message || 'Live Ghana Card verification failed.';
      setVerificationMessage(message);
      setVerificationConfirmed(false);
      setVerificationContext(null);
      setError(message);
    } finally {
      setVerificationLoading(false);
    }
  };

  const handleSubmit = async () => {
    clearMessages();

    if (!selectedAccount || !customer) {
      setError('Select an account before posting a transaction.');
      return;
    }

    if (Number.isNaN(parsedAmount) || parsedAmount <= 0) {
      setError('Enter a valid cash amount before committing the transaction.');
      return;
    }

    if (!verificationConfirmed) {
      setError('Complete Ghana Card verification before posting this transaction.');
      return;
    }

    try {
      const verificationSummary = verificationDraft.mode === 'ACCOUNT_HOLDER'
        ? `IDV:ACCOUNT_HOLDER:${normalizeCard(verificationDraft.presenterGhanaCard)}`
        : `IDV:THIRD_PARTY:${normalizeCard(verificationDraft.presenterGhanaCard)}:${verificationDraft.relationship.trim()}:NOTE:${verificationDraft.noteId}`;

      const composedNarration = [
        narration.trim() || `${txType} via Teller`,
        verificationSummary,
        customer.digitalAddress ? `ADDR:${customer.digitalAddress}` : '',
      ].filter(Boolean).join(' | ');

      const result = await onTransaction(selectedAccount.id, txType, parsedAmount, composedNarration, tellerId);

      if (result.status === 'POSTED') {
        setSuccessMsg(`Success: Posted (Ref: ${result.id})`);
      } else {
        setPendingMsg(`Held: ${result.message} (Req: ${result.id})`);
      }

      setAmount('');
      setNarration('');
      resetVerification();
    } catch (submissionError: any) {
      setError(submissionError?.message || 'Transaction failed.');
    }
  };

  const clearForm = () => {
    setAccountNum('');
    setSelectedAccount(null);
    setAmount('');
    setNarration('');
    clearMessages();
    resetVerification();
  };

  return (
    <div className="h-full bg-[linear-gradient(180deg,_#f8fafc_0%,_#eef2f7_100%)] p-4">
      <div className="grid h-full gap-4 xl:grid-cols-[minmax(0,1.45fr)_340px]">
        <section className="flex flex-col overflow-hidden rounded-[28px] border border-slate-200/80 bg-white/95 shadow-soft dark:border-slate-700 dark:bg-slate-900/85">
          <div className="border-b border-slate-200 bg-[linear-gradient(135deg,#0f172a,#1e293b)] px-6 py-5 text-white dark:border-slate-700">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <div className="text-[11px] font-semibold uppercase tracking-[0.24em] text-slate-300">Teller operations</div>
                <h2 className="mt-2 flex items-center gap-2 text-2xl font-heading font-bold">
                  <Landmark className="h-6 w-6 text-brand-300" />
                  Teller transactions
                </h2>
                <p className="mt-2 max-w-2xl text-sm text-slate-300">Process cash transactions, verify presenter identity, and review till position before posting.</p>
              </div>
              <div className="flex flex-wrap gap-2 text-xs font-semibold uppercase tracking-[0.16em]">
                <span className="rounded-full bg-brand-500/15 px-3 py-1.5 text-brand-100">{transactionLabel}</span>
                <span className="rounded-full bg-emerald-500/15 px-3 py-1.5 text-emerald-200">ID verification required</span>
                <span className="rounded-full bg-slate-800 px-3 py-1.5 text-slate-200">Teller ID {tellerId}</span>
                <span className="rounded-full bg-slate-800 px-3 py-1.5 text-slate-200">{tillLoading ? 'Loading till...' : `Till ${tillSummary ? formatTillStatus(tillSummary.status) : 'Not Open'}`}</span>
              </div>
            </div>
            <div className="mt-5 grid gap-3 md:grid-cols-4">
              <HeroMetric label="Transaction type" value={transactionLabel} helper="Current transaction" />
              <HeroMetric label="Presenter check" value={verificationConfirmed ? 'Verified' : 'Pending'} helper="Identity verification" tone={verificationConfirmed ? 'good' : 'warn'} />
              <HeroMetric label="Till balance" value={`GHS ${formatAmount(tillSummary?.currentBalance || 0)}`} helper={tillSummary?.branchName || 'No active till'} />
              <HeroMetric label="Variance" value={`GHS ${formatAmount(tillSummary?.variance || 0)}`} helper={tillSummary?.currency || 'GHS'} tone={(tillSummary?.variance || 0) === 0 ? 'good' : 'warn'} />
            </div>
          </div>

          <div className="grid flex-1 gap-6 overflow-y-auto p-6 xl:grid-cols-[minmax(0,1fr)_320px]">
            <div className="space-y-6">
              <div className="rounded-[24px] border border-indigo-200 bg-indigo-50/90 px-5 py-4 text-indigo-900 dark:border-indigo-500/30 dark:bg-indigo-500/10 dark:text-indigo-100">
                <div className="flex items-start gap-3">
                  <ShieldCheck className="mt-0.5 h-5 w-5 flex-shrink-0" />
                  <div>
                    <div className="text-[11px] font-semibold uppercase tracking-[0.2em]">Bank of Ghana identity control</div>
                    <div className="mt-1 text-sm">
                      Cashier posting enforces Ghana Card verification before cash handling. For third-party transactions, presenter identification and verification must be completed and linked to the transaction record.
                    </div>
                    <div className="mt-2 text-xs text-indigo-700 dark:text-indigo-200">
                      Control basis: BOG supervisory guidance on the use of the Ghana Card and the December 1, 2025 biometric verification implementation notice.
                    </div>
                  </div>
                </div>
              </div>

              <div className="rounded-[24px] border border-slate-200 bg-slate-50/90 p-5 dark:border-slate-700 dark:bg-slate-800/60">
                <div className="mb-4 flex items-center justify-between gap-3">
                  <div>
                    <div className="text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">Cashier step 1</div>
                    <h3 className="mt-2 text-lg font-heading font-bold text-slate-950 dark:text-white">Lookup account and customer</h3>
                  </div>
                  <div className="rounded-full bg-white px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-slate-600 shadow-sm dark:bg-slate-900 dark:text-slate-300">
                    {selectedAccount ? 'Customer loaded' : 'Awaiting account lookup'}
                  </div>
                </div>
                <div className="flex flex-wrap items-end gap-3">
                  <div className="min-w-[220px] flex-1">
                    <label className="block text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Account Number</label>
                    <div className="mt-2 flex">
                      <input
                        type="text"
                        value={accountNum}
                        onChange={(event) => setAccountNum(event.target.value)}
                        onKeyDown={(event) => event.key === 'Enter' && findAccount()}
                        className="flex-1 rounded-l-2xl border border-slate-300 bg-white px-4 py-3 font-mono text-lg text-slate-950 outline-none ring-0 transition focus:border-brand-500 dark:border-slate-600 dark:bg-slate-950 dark:text-white"
                        placeholder="001100000148"
                      />
                      <button onClick={findAccount} className="inline-flex items-center gap-2 rounded-r-2xl border border-l-0 border-slate-300 bg-slate-950 px-4 py-3 text-sm font-semibold text-white hover:bg-slate-800 dark:border-slate-600 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
                        <Search size={16} /> Lookup
                      </button>
                    </div>
                  </div>
                  <div className="min-w-[220px]">
                    <label className="block text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Transaction Type</label>
                    <select value={txType} onChange={(event) => setTxType(event.target.value as 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER')} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-semibold text-slate-900 dark:border-slate-600 dark:bg-slate-950 dark:text-white">
                      <option value="DEPOSIT">Cash Deposit</option>
                      <option value="WITHDRAWAL">Cash Withdrawal</option>
                    </select>
                  </div>
                </div>
              </div>

              <div className="rounded-[24px] border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-900/70">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <div className="text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">Account profile</div>
                    <h3 className="mt-2 text-lg font-heading font-bold text-slate-950 dark:text-white">Customer and account details</h3>
                  </div>
                  <div className={`rounded-full px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] ${selectedAccount ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300' : 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300'}`}>
                    {selectedAccount ? 'Loaded' : 'Awaiting lookup'}
                  </div>
                </div>

                {selectedAccount && customer ? (
                  <div className="mt-4 grid gap-4 md:grid-cols-2">
                    <InfoCard label="Customer" value={customer.name} helper={`CIF ${customer.id}`} icon={<User className="h-4 w-4" />} />
                    <InfoCard label="Account" value={selectedAccount.id} helper={`${selectedAccount.type} - ${selectedAccount.productCode}`} icon={<Wallet className="h-4 w-4" />} />
                    <InfoCard label="Ghana Card on File" value={customer.ghanaCard || 'Not captured'} helper={customer.digitalAddress || 'No digital address'} icon={<CreditCard className="h-4 w-4" />} />
                    <InfoCard label="Clear Balance" value={`GHS ${formatAmount(selectedAccount.balance)}`} helper={`Status ${selectedAccount.status}`} icon={<BadgeCheck className="h-4 w-4" />} />
                  </div>
                ) : (
                  <div className="mt-4 rounded-[20px] border border-dashed border-slate-300 px-4 py-12 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                    Search an account to load the customer profile before verification.
                  </div>
                )}
              </div>

              <div className="rounded-[24px] border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-900/70">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <div className="text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">Cashier step 2</div>
                    <h3 className="mt-2 text-lg font-heading font-bold text-slate-950 dark:text-white">Capture transaction details</h3>
                    <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Enter the amount and business reason exactly as it should appear in the audit trail.</p>
                  </div>
                  <div className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">Verification gate enforced</div>
                </div>

                <div className="mt-4 grid gap-4 md:grid-cols-2">
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-300 md:col-span-2">
                    Amount (GHS)
                    <input type="number" value={amount} onChange={(event) => setAmount(event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-4 text-right font-mono text-2xl text-slate-950 dark:border-slate-600 dark:bg-slate-950 dark:text-white" placeholder="0.00" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-300 md:col-span-2">
                    Narration
                    <input type="text" value={narration} onChange={(event) => setNarration(event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm dark:border-slate-600 dark:bg-slate-950 dark:text-white" placeholder="By self / representative / reason for withdrawal" />
                  </label>
                </div>

                {txType === 'WITHDRAWAL' && amount && (
                  <div className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-[20px] border border-amber-200 bg-amber-50 px-4 py-4 dark:border-amber-500/30 dark:bg-amber-500/10">
                    <div>
                      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-amber-700 dark:text-amber-200">Withdrawal levy estimate</div>
                      <div className="mt-1 text-sm text-amber-800 dark:text-amber-100">Illustrative statutory charge is shown for teller awareness.</div>
                    </div>
                    <div className="text-right">
                      <div className="text-xs text-amber-700 dark:text-amber-200">Levy</div>
                      <div className="font-mono text-lg font-bold text-amber-900 dark:text-white">GHS {formatAmount(tax)}</div>
                    </div>
                    <div className="text-right">
                      <div className="text-xs text-amber-700 dark:text-amber-200">Total Debit</div>
                      <div className="font-mono text-lg font-bold text-slate-950 dark:text-white">GHS {formatAmount(totalDebit)}</div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            <aside className="space-y-6">
              <div className="rounded-[24px] border border-slate-200 bg-white p-5 shadow-soft dark:border-slate-700 dark:bg-slate-900/70">
                <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">
                  <ShieldCheck className="h-4 w-4" /> Identity Verification
                </div>
                <h3 className="mt-2 text-lg font-heading font-bold text-slate-950 dark:text-white">Cashier step 3: Ghana Card verification</h3>
                <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Cash cannot be posted until the presenter identity is validated for this transaction and recorded in the transaction trail.</p>
                <div className="mt-4 grid gap-3">
                  <div className="grid gap-3 sm:grid-cols-2">
                    <button onClick={() => { setVerificationDraft((current) => ({ ...current, mode: 'ACCOUNT_HOLDER', relationship: '' })); setVerificationConfirmed(false); setVerificationMessage(null); }} className={`rounded-2xl border px-4 py-3 text-sm font-semibold ${verificationDraft.mode === 'ACCOUNT_HOLDER' ? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-400 dark:bg-brand-500/10 dark:text-brand-200' : 'border-slate-300 text-slate-600 dark:border-slate-600 dark:text-slate-300'}`}>
                      Account Holder
                    </button>
                    <button onClick={() => { setVerificationDraft((current) => ({ ...current, mode: 'THIRD_PARTY' })); setVerificationConfirmed(false); setVerificationMessage(null); }} className={`rounded-2xl border px-4 py-3 text-sm font-semibold ${verificationDraft.mode === 'THIRD_PARTY' ? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-400 dark:bg-brand-500/10 dark:text-brand-200' : 'border-slate-300 text-slate-600 dark:border-slate-600 dark:text-slate-300'}`}>
                      Third-Party Presenter
                    </button>
                  </div>

                  <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                    Presenter Name
                    <input value={verificationDraft.presenterName} onChange={(event) => setVerificationDraft((current) => ({ ...current, presenterName: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm dark:border-slate-600 dark:bg-slate-950 dark:text-white" placeholder="Name as shown on Ghana Card" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                    Ghana Card Number
                    <input value={verificationDraft.presenterGhanaCard} onChange={(event) => setVerificationDraft((current) => ({ ...current, presenterGhanaCard: event.target.value.toUpperCase() }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 font-mono text-sm dark:border-slate-600 dark:bg-slate-950 dark:text-white" placeholder="GHA-123456789-0" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                    Presenter Phone
                    <input value={verificationDraft.presenterPhone} onChange={(event) => setVerificationDraft((current) => ({ ...current, presenterPhone: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm dark:border-slate-600 dark:bg-slate-950 dark:text-white" placeholder="Optional contact number" />
                  </label>

                  {verificationDraft.mode === 'THIRD_PARTY' && (
                    <>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                        Relationship to Account Holder
                        <input value={verificationDraft.relationship} onChange={(event) => setVerificationDraft((current) => ({ ...current, relationship: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm dark:border-slate-600 dark:bg-slate-950 dark:text-white" placeholder="Spouse, sibling, business cashier, attorney..." />
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
                        Linked Teller Note
                        <select value={verificationDraft.noteId} onChange={(event) => setVerificationDraft((current) => ({ ...current, noteId: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm dark:border-slate-600 dark:bg-slate-950 dark:text-white">
                          <option value="">Select note</option>
                          {availableNotes.map((note) => (
                            <option key={note.id} value={note.id}>
                              {note.id} - {note.subject}
                            </option>
                          ))}
                        </select>
                      </label>
                      <button type="button" onClick={onOpenNotes} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-950 dark:text-white">
                        <FileText className="h-4 w-4" />
                        Open teller notes
                      </button>
                    </>
                  )}

                  <button onClick={() => { void handleVerifyIdentity(); }} disabled={!selectedAccount || verificationLoading} className="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
                    <UserCheck className="h-4 w-4" /> {verificationLoading ? 'Verifying...' : 'Verify Ghana Card'}
                  </button>
                </div>

                {verificationMessage && (
                  <div className={`mt-4 rounded-[20px] border px-4 py-4 text-sm ${verificationConfirmed ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200' : 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-100'}`}>
                    {verificationMessage}
                  </div>
                )}

                {verificationContext && (
                  <div className="mt-4 grid gap-3 sm:grid-cols-3">
                    <ActionStat label="KYC level" value={verificationContext.kycLevel} subtle />
                    <ActionStat label="Transaction limit" value={`GHS ${formatAmount(verificationContext.transactionLimit)}`} subtle />
                    <ActionStat label="Remaining daily limit" value={`GHS ${formatAmount(verificationContext.remainingDailyLimit)}`} subtle />
                  </div>
                )}

                <div className="mt-4 rounded-[20px] border border-slate-200 bg-slate-50 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Compliance checklist</div>
                  <div className="mt-3 space-y-3">
                    {complianceChecklist.map((item) => (
                      <div key={item.label} className="flex items-start gap-3">
                        <div className={`mt-0.5 flex h-5 w-5 items-center justify-center rounded-full ${item.done ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300' : 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-200'}`}>
                          {item.done ? <CheckCircle className="h-3.5 w-3.5" /> : <AlertCircle className="h-3.5 w-3.5" />}
                        </div>
                        <div>
                          <div className="text-sm font-semibold text-slate-900 dark:text-white">{item.label}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">{item.detail}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>

              <div className="rounded-[24px] border border-slate-200 bg-white p-5 shadow-soft dark:border-slate-700 dark:bg-slate-900/70">
                <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">
                  <Wallet className="h-4 w-4" /> Teller Till
                </div>
                <h3 className="mt-2 text-lg font-heading font-bold text-slate-950 dark:text-white">Live till position</h3>
                <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Current till status and expected cash position for the signed-in teller.</p>

                <div className="mt-4 grid gap-3 text-sm text-slate-500 dark:text-slate-400">
                  <div className="rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                    <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">Till status</div>
                    <div className={`mt-1 font-semibold ${tillStatusTone}`}>{tillLoading ? 'Loading...' : formatTillStatus(tillSummary?.status)}</div>
                    <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{tillSummary ? `${tillSummary.branchName} - ${tillSummary.currency}` : 'No open till session found for this teller.'}</div>
                  </div>
                  <div className="grid gap-3 sm:grid-cols-2">
                    <div className="rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">Expected balance</div>
                      <div className="mt-1 font-mono text-lg font-bold text-slate-950 dark:text-white">GHS {formatAmount(tillSummary?.currentBalance || 0)}</div>
                    </div>
                    <div className="rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">Variance</div>
                      <div className={`mt-1 font-mono text-lg font-bold ${(tillSummary?.variance || 0) === 0 ? 'text-emerald-700 dark:text-emerald-300' : 'text-rose-700 dark:text-rose-300'}`}>GHS {formatAmount(tillSummary?.variance || 0)}</div>
                    </div>
                  </div>
                  <div className="grid gap-3 sm:grid-cols-2">
                    <div className="rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">Vault allocated</div>
                      <div className="mt-1 font-mono font-semibold text-slate-950 dark:text-white">GHS {formatAmount(tillSummary?.allocatedFromVault || 0)}</div>
                    </div>
                    <div className="rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">Opened at</div>
                      <div className="mt-1 font-semibold text-slate-950 dark:text-white">{tillSummary?.openedAt ? new Date(tillSummary.openedAt).toLocaleString() : 'Not opened'}</div>
                    </div>
                  </div>
                </div>
              </div>
              <div className="rounded-[24px] border border-slate-200 bg-white p-5 shadow-soft dark:border-slate-700 dark:bg-slate-900/70">
                <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.22em] text-brand-700 dark:text-brand-300">
                  <ArrowRightLeft className="h-4 w-4" /> Posting control
                </div>
                <h3 className="mt-2 text-lg font-heading font-bold text-slate-950 dark:text-white">Cashier step 4: Review and post</h3>
                <div className="mt-4 rounded-[20px] border border-slate-200 bg-slate-50 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="grid gap-3 sm:grid-cols-2">
                    <ActionStat label="Verification status" value={verificationConfirmed ? 'Ready to post' : 'Verification pending'} tone={verificationConfirmed ? 'good' : 'warn'} />
                    <ActionStat label="Transaction amount" value={`GHS ${formatAmount(parsedAmount)}`} />
                    <ActionStat label="Selected account" value={selectedAccount?.id || 'Not selected'} subtle />
                    <ActionStat label="Commit readiness" value={commitHint} tone={canCommit ? 'good' : 'warn'} subtle />
                  </div>
                </div>

                <div className="mt-4 rounded-[20px] border border-slate-200 bg-white px-4 py-4 dark:border-slate-700 dark:bg-slate-950/40">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">Transaction review</div>
                  <div className="mt-3 grid gap-3 sm:grid-cols-2">
                    <ActionStat label="Customer" value={customer?.name || 'Not loaded'} subtle />
                    <ActionStat label="Presenter mode" value={verificationDraft.mode === 'ACCOUNT_HOLDER' ? 'Account holder' : 'Third-party presenter'} subtle />
                    <ActionStat label="Presented Ghana Card" value={verificationDraft.presenterGhanaCard || 'Not captured'} subtle />
                    <ActionStat label="BOG control date" value={BOG_GHANA_CARD_EFFECTIVE_DATE} subtle />
                  </div>
                </div>

                {(error || successMsg || pendingMsg) && (
                  <div className="mt-4 space-y-3">
                    {error && <Banner tone="error" message={error} icon={<AlertCircle className="h-5 w-5" />} />}
                    {successMsg && <Banner tone="success" message={successMsg} icon={<CheckCircle className="h-5 w-5" />} />}
                    {pendingMsg && <Banner tone="warning" message={pendingMsg} icon={<ShieldCheck className="h-5 w-5" />} />}
                  </div>
                )}

                <div className="mt-5 flex gap-3">
                  <button onClick={clearForm} className="flex-1 rounded-2xl border border-slate-300 px-4 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800">
                    Clear
                  </button>
                  <button onClick={handleSubmit} disabled={!canCommit} className="flex-1 rounded-2xl bg-brand-600 px-4 py-3 text-sm font-semibold text-white hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-50">
                    Post Transaction
                  </button>
                </div>
              </div>
            </aside>
          </div>
        </section>

      </div>
    </div>
  );
};

function InfoCard({ label, value, helper, icon }: { label: string; value: string; helper: string; icon: React.ReactNode }) {
  return (
    <div className="rounded-[20px] border border-slate-200 bg-slate-50 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/50">
      <div className="flex items-center justify-between gap-3 text-slate-500 dark:text-slate-400">
        <span className="text-xs font-semibold uppercase tracking-[0.18em]">{label}</span>
        {icon}
      </div>
      <div className="mt-2 font-semibold text-slate-950 dark:text-white">{value}</div>
      <div className="mt-1 text-sm text-slate-500 dark:text-slate-400">{helper}</div>
    </div>
  );
}

function HeroMetric({ label, value, helper, tone = 'default' }: { label: string; value: string; helper: string; tone?: 'default' | 'good' | 'warn' }) {
  const valueClass = tone === 'good'
    ? 'text-emerald-200'
    : tone === 'warn'
      ? 'text-amber-100'
      : 'text-white';

  return (
    <div className="rounded-[20px] border border-white/10 bg-white/5 px-4 py-4 backdrop-blur-sm">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-300">{label}</div>
      <div className={`mt-2 text-lg font-semibold ${valueClass}`}>{value}</div>
      <div className="mt-1 text-xs text-slate-400">{helper}</div>
    </div>
  );
}

function ActionStat({ label, value, tone = 'default', subtle = false }: { label: string; value: string; tone?: 'default' | 'good' | 'warn'; subtle?: boolean }) {
  const valueClass = tone === 'good'
    ? 'text-emerald-700 dark:text-emerald-300'
    : tone === 'warn'
      ? 'text-amber-700 dark:text-amber-200'
      : 'text-slate-950 dark:text-white';

  return (
    <div className="rounded-[16px] border border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-900/70">
      <div className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-400 dark:text-slate-500">{label}</div>
      <div className={`mt-1 ${subtle ? 'text-sm font-medium' : 'text-base font-semibold'} ${valueClass}`}>{value}</div>
    </div>
  );
}

function Banner({ tone, message, icon }: { tone: 'success' | 'warning' | 'error'; message: string; icon: React.ReactNode }) {
  const className = tone === 'success'
    ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
    : tone === 'warning'
      ? 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-100'
      : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200';

  return <div className={`flex items-start gap-3 rounded-[20px] border px-4 py-4 ${className}`}>{icon}<span>{message}</span></div>;
}

export default TellerTerminal;
