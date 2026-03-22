import React, { useState, useEffect, useMemo } from 'react';
import { Loan, AmortizationSchedule, Customer, Group, Product } from '../types';
import { Plus, Calculator, Calendar, User, DollarSign, FileText, CheckCircle, XCircle, AlertCircle, ShieldAlert, BadgeCheck, ChevronRight, ChevronLeft, Upload, File, Check, Loader2, Users, PieChart, Wallet, Save, TrendingUp } from 'lucide-react';

interface LoanEngineProps {
  loans: Loan[];
  setLoans: React.Dispatch<React.SetStateAction<Loan[]>>;
  onDisburse: (loanId: string) => void;
  customers: Customer[];
  groups?: Group[];
  products: Product[]; // Now receives dynamic products
}

const STEPS = [
    { id: 1, label: 'Borrower Info' },
    { id: 2, label: 'Loan Terms' },
    { id: 3, label: 'Collateral & Docs' },
    { id: 4, label: 'Review & Submit' }
];

const LoanEngine: React.FC<LoanEngineProps> = ({ loans, setLoans, onDisburse, customers, groups = [], products = [] }) => {
  const [view, setView] = useState<'LIST' | 'WIZARD' | 'DETAILS'>('LIST');
  const [selectedLoan, setSelectedLoan] = useState<Loan | null>(null);
  const [schedule, setSchedule] = useState<AmortizationSchedule[]>([]);
  const [customerDetails, setCustomerDetails] = useState<Customer | null>(null);
  const [groupDetails, setGroupDetails] = useState<Group | null>(null);
  const [validationError, setValidationError] = useState<string>('');

  // Group Loan State
  const [memberSplits, setMemberSplits] = useState<Record<string, number>>({});
  const [memberContributions, setMemberContributions] = useState<Record<string, number>>({});

  // Filter for only LOAN products
  const loanProducts = useMemo(() => products.filter(p => p.type === 'LOAN' && p.status === 'ACTIVE'), [products]);

  // Wizard State
  const [wizardStep, setWizardStep] = useState(1);
  const [uploadedDocs, setUploadedDocs] = useState<string[]>([]);
  const [submissionStatus, setSubmissionStatus] = useState<'IDLE' | 'SUBMITTING' | 'SUCCESS'>('IDLE');

  // Origination Form State
  const [formData, setFormData] = useState({
    borrowerType: 'INDIVIDUAL' as 'INDIVIDUAL' | 'GROUP',
    cif: '', // Holds CIF for Individual, GroupID for Group
    productCode: '', // Changed to store ID instead of Name
    principal: 0,
    rate: 0,
    term: 0,
    collateralType: 'Unsecured',
    collateralValue: ''
  });

  // Set default product when products load
  useEffect(() => {
      if (loanProducts.length > 0 && !formData.productCode) {
          const defaultProd = loanProducts[0];
          setFormData(prev => ({
              ...prev,
              productCode: defaultProd.id,
              principal: defaultProd.minAmount || 0,
              rate: defaultProd.interestRate,
              term: defaultProd.defaultTerm || 12
          }));
      }
  }, [loanProducts]);

  // Validate Borrower when type or ID changes
  useEffect(() => {
    setCustomerDetails(null);
    setGroupDetails(null);
    setValidationError('');
    setMemberSplits({});

    if (formData.cif.length >= 3) {
        if (formData.borrowerType === 'INDIVIDUAL') {
            const cust = customers.find(c => c.id === formData.cif);
            if (cust) {
                setCustomerDetails(cust);
            } else {
                setValidationError('Customer not found');
            }
        } else {
            const grp = groups.find(g => g.id === formData.cif);
            if (grp) {
                setGroupDetails(grp);
                // Initialize splits
                const initialSplits: Record<string, number> = {};
                grp.members.forEach(m => initialSplits[m] = 0);
                setMemberSplits(initialSplits);
            } else {
                setValidationError('Group not found');
            }
        }
    }
  }, [formData.cif, formData.borrowerType, customers, groups]);

  // Reset wizard when opening new app
  useEffect(() => {
    if (view === 'WIZARD') {
        setWizardStep(1);
        setUploadedDocs([]);
        setSubmissionStatus('IDLE');
        // Reset form
        const defaultProd = loanProducts[0];
        setFormData({
            borrowerType: 'INDIVIDUAL',
            cif: '',
            productCode: defaultProd?.id || '',
            principal: defaultProd?.minAmount || 1000,
            rate: defaultProd?.interestRate || 0,
            term: defaultProd?.defaultTerm || 12,
            collateralType: 'Unsecured',
            collateralValue: ''
        });
        setCustomerDetails(null);
        setGroupDetails(null);
        setSchedule([]);
        setMemberSplits({});
    }
  }, [view]);

  // Reset contribution state when details view opens
  useEffect(() => {
      if (view === 'DETAILS') {
          setMemberContributions({});
      }
  }, [view, selectedLoan]);

  // Auto-calculate schedule when terms change
  useEffect(() => {
    if (view === 'WIZARD' && formData.principal > 0 && formData.term > 0) {
        calculateSchedule(formData.principal, formData.rate, formData.term);
    }
  }, [formData.principal, formData.rate, formData.term, view]);

  // Group Splits Logic
  const handleSplitChange = (memberId: string, value: string) => {
      const val = parseFloat(value) || 0;
      setMemberSplits(prev => {
          const next = { ...prev, [memberId]: val };
          // Calculate new total principal
          const newTotal = Object.values(next).reduce((a, b) => a + b, 0);
          setFormData(f => ({ ...f, principal: newTotal }));
          return next;
      });
  };

  // Update defaults when product changes
  const handleProductChange = (prodCode: string) => {
      const product = loanProducts.find(p => p.id === prodCode);
      if (product) {
          setFormData(prev => ({
              ...prev,
              productCode: prodCode,
              rate: product.interestRate,
              term: product.defaultTerm || 12,
              principal: Math.max(prev.principal, product.minAmount) // Ensure min amount
          }));
      }
  };

  // Determine required documents based on product
  const requiredDocs = useMemo(() => {
      if (formData.borrowerType === 'GROUP') {
          return ['Group Constitution', 'Member List', 'Guarantor Forms'];
      }
      const docs = ['National ID (Ghana Card)', 'Passport Photo'];
      // Logic could be extended to check Product metadata for doc requirements
      if (formData.collateralType === 'Landed Property') {
          docs.push('Proof of Address', 'Indenture / Land Title');
      } else {
          docs.push('Proof of Address');
      }
      return docs;
  }, [formData.productCode, formData.collateralType, formData.borrowerType]);

  const allPossibleDocs = useMemo(() => {
      const base = ['National ID (Ghana Card)', 'Proof of Address', 'Recent Pay Slip', 'Passport Photo', 'Indenture / Land Title', 'Business Registration Cert', 'Group Constitution', 'Member List', 'Guarantor Forms'];
      return Array.from(new Set([...base, ...requiredDocs]));
  }, [requiredDocs]);

  const calculateSchedule = (principal: number, rate: number, term: number) => {
    const newSchedule: AmortizationSchedule[] = [];
    const startDate = new Date();
    
    let payment = 0;
    
    if (term <= 0) return;

    if (rate <= 0) {
        payment = principal / term;
        let balance = principal;
        for (let i = 1; i <= term; i++) {
            balance -= payment;
            if (i === term && Math.abs(balance) < 0.01) balance = 0;
            startDate.setMonth(startDate.getMonth() + 1);
            newSchedule.push({
                period: i,
                dueDate: startDate.toISOString().split('T')[0],
                principal: payment,
                interest: 0,
                total: payment,
                balance: balance > 0 ? balance : 0,
                status: 'DUE'
            });
        }
    } else {
        const monthlyRate = (rate / 100) / 12;
        payment = (principal * monthlyRate) / (1 - Math.pow(1 + monthlyRate, -term));
        
        let balance = principal;
        for (let i = 1; i <= term; i++) {
            const interest = balance * monthlyRate;
            const principalPart = payment - interest;
            balance -= principalPart;
            if (i === term && Math.abs(balance) < 1) balance = 0;
            startDate.setMonth(startDate.getMonth() + 1);
            newSchedule.push({
                period: i,
                dueDate: startDate.toISOString().split('T')[0],
                principal: principalPart,
                interest: interest,
                total: payment,
                balance: balance > 0 ? balance : 0,
                status: 'DUE'
            });
        }
    }
    setSchedule(newSchedule);
  };

  const handleNextStep = () => {
    setValidationError('');
    const selectedProd = loanProducts.find(p => p.id === formData.productCode);

    if (wizardStep === 1) {
        if (formData.borrowerType === 'INDIVIDUAL' && !customerDetails) {
            setValidationError('Please select a valid customer.');
            return;
        }
        if (formData.borrowerType === 'GROUP' && !groupDetails) {
            setValidationError('Please select a valid group.');
            return;
        }
    }
    
    if (wizardStep === 2) {
        if (formData.principal <= 0 || formData.term <= 0) {
            setValidationError('Principal and Term must be positive.');
            return;
        }
        if (selectedProd) {
            if (formData.principal < selectedProd.minAmount) {
                setValidationError(`Minimum principal for this product is GHS ${selectedProd.minAmount.toLocaleString()}`);
                return;
            }
            if (selectedProd.maxAmount && formData.principal > selectedProd.maxAmount) {
                setValidationError(`Maximum principal for this product is GHS ${selectedProd.maxAmount.toLocaleString()}`);
                return;
            }
            if (selectedProd.minTerm && formData.term < selectedProd.minTerm) {
                setValidationError(`Minimum term is ${selectedProd.minTerm} months`);
                return;
            }
            if (selectedProd.maxTerm && formData.term > selectedProd.maxTerm) {
                setValidationError(`Maximum term is ${selectedProd.maxTerm} months`);
                return;
            }
        }
        if (formData.borrowerType === 'INDIVIDUAL' && customerDetails?.kycLevel === 'Tier 1' && formData.principal > 5000) {
             setValidationError('Tier 1 Customers limited to GHS 5,000.');
             return;
        }
        // Group Validation: Ensure all members have allocation if needed, or total matches
        if (formData.borrowerType === 'GROUP' && groupDetails) {
             const sum = Object.values(memberSplits).reduce((a,b)=>a+b, 0);
             if (Math.abs(sum - formData.principal) > 1) {
                 setValidationError(`Member splits sum (${sum}) does not match principal (${formData.principal}).`);
                 return;
             }
        }
    }

    if (wizardStep === 3) {
        const missing = requiredDocs.filter(doc => !uploadedDocs.includes(doc));
        if (missing.length > 0) {
            setValidationError(`Missing required documents: ${missing.join(', ')}`);
            return;
        }
        // Ensure schedule is fresh
        calculateSchedule(formData.principal, formData.rate, formData.term);
    }

    setWizardStep(prev => prev + 1);
  };

  const handleBackStep = () => {
      setWizardStep(prev => prev - 1);
  };

  const handleSimulateUpload = (docName: string) => {
      if (!uploadedDocs.includes(docName)) {
          setUploadedDocs(prev => [...prev, docName]);
          // Clear error if it was about docs
          if (validationError.startsWith('Missing required')) {
              setValidationError('');
          }
      }
  };

  const saveLoan = () => {
      setSubmissionStatus('SUBMITTING');
      const selectedProd = loanProducts.find(p => p.id === formData.productCode);
      
      // Simulate network request
      setTimeout(() => {
          const newLoan: Loan = {
              id: `LN${Date.now().toString().slice(-6)}`,
              cif: formData.borrowerType === 'INDIVIDUAL' ? formData.cif : '', // Legacy Field
              groupId: formData.borrowerType === 'GROUP' ? formData.cif : undefined,
              memberSplits: formData.borrowerType === 'GROUP' ? memberSplits : undefined,
              productName: selectedProd?.name || 'Unknown Loan',
              productCode: selectedProd?.id,
              principal: formData.principal,
              rate: formData.rate,
              termMonths: formData.term,
              disbursementDate: new Date().toISOString().split('T')[0],
              parBucket: '0',
              outstandingBalance: formData.principal,
              collateralType: formData.collateralType,
              status: 'PENDING'
          };
          setLoans(prev => [newLoan, ...prev]);
          setSubmissionStatus('SUCCESS');
      }, 2000);
  };

  // Group Contribution Helpers
  const handleContributionChange = (memberId: string, amount: string) => {
      setMemberContributions(prev => ({...prev, [memberId]: parseFloat(amount) || 0}));
  };

  const totalContribution = Object.values(memberContributions).reduce((a, b) => a + b, 0);

  const submitGroupRepayment = () => {
      if (totalContribution <= 0 || !selectedLoan) return;
      
      setLoans(prev => prev.map(l => {
          if (l.id === selectedLoan.id) {
              const newBal = l.outstandingBalance - totalContribution;
              return { ...l, outstandingBalance: Math.max(0, newBal) };
          }
          return l;
      }));
      
      // Update selected loan local state to reflect change immediately in view
      setSelectedLoan(prev => prev ? ({ ...prev, outstandingBalance: Math.max(0, prev.outstandingBalance - totalContribution) }) : null);
      
      setMemberContributions({});
      alert(`Group Repayment of GHS ${totalContribution.toLocaleString()} processed successfully.`);
  };

  return (
    <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
        <h2 className="text-lg font-bold text-gray-800 flex items-center gap-2">
            <Calculator className="text-blue-600" /> Loan Engine
        </h2>
        {view === 'LIST' && (
            <button 
                onClick={() => setView('WIZARD')}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 flex items-center gap-2"
            >
                <Plus size={16} /> New Application
            </button>
        )}
        {view !== 'LIST' && (
             <button 
                onClick={() => setView('LIST')}
                className="text-gray-600 hover:text-blue-600 text-sm font-medium"
            >
                &larr; Back to Portfolio
            </button>
        )}
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-6">
        
        {/* VIEW: LIST */}
        {view === 'LIST' && (
             <div className="overflow-x-auto">
                <table className="w-full text-sm text-left">
                    <thead className="bg-gray-100 text-gray-600 font-semibold uppercase text-xs">
                        <tr>
                            <th className="p-3">Loan ID</th>
                            <th className="p-3">Borrower</th>
                            <th className="p-3">Type</th>
                            <th className="p-3">Product</th>
                            <th className="p-3 text-right">Principal</th>
                            <th className="p-3 text-right">Outstanding</th>
                            <th className="p-3 text-center">Status</th>
                            <th className="p-3 text-center">Action</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {loans.map(loan => (
                            <tr key={loan.id} className="hover:bg-gray-50">
                                <td className="p-3 font-mono text-blue-600 font-medium">{loan.id}</td>
                                <td className="p-3">
                                    {loan.groupId ? 
                                        <span className="flex items-center gap-1"><Users size={12} className="text-purple-500"/> {loan.groupId}</span> : 
                                        <span className="flex items-center gap-1"><User size={12} className="text-gray-500"/> {loan.cif}</span>
                                    }
                                </td>
                                <td className="p-3 text-xs">{loan.groupId ? 'Group' : 'Individual'}</td>
                                <td className="p-3">{loan.productName}</td>
                                <td className="p-3 text-right font-mono">{loan.principal.toLocaleString()}</td>
                                <td className="p-3 text-right font-mono font-bold text-gray-800">{loan.outstandingBalance.toLocaleString()}</td>
                                <td className="p-3 text-center">
                                     <span className={`px-2 py-1 rounded-full text-xs font-bold border ${
                                        loan.status === 'ACTIVE' ? 'bg-green-50 text-green-700 border-green-200' : 
                                        loan.status === 'WRITTEN_OFF' ? 'bg-red-50 text-red-700 border-red-200' :
                                        'bg-gray-50 text-gray-700 border-gray-200'
                                    }`}>
                                        {loan.status}
                                    </span>
                                </td>
                                <td className="p-3 text-center">
                                    <button 
                                        onClick={() => { setSelectedLoan(loan); calculateSchedule(loan.principal, loan.rate, loan.termMonths); setView('DETAILS'); }}
                                        className="text-blue-600 hover:text-blue-800 text-xs font-medium"
                                    >
                                        View
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
             </div>
        )}

        {/* VIEW: WIZARD (ORIGINATE) */}
        {view === 'WIZARD' && (
            <div className="max-w-4xl mx-auto">
                {/* Submission Success State */}
                {submissionStatus === 'SUCCESS' ? (
                    <div className="flex flex-col items-center justify-center h-96 text-center animate-in fade-in zoom-in-95 duration-500">
                        <div className="w-20 h-20 bg-green-100 text-green-600 rounded-full flex items-center justify-center mb-6">
                            <CheckCircle size={48} />
                        </div>
                        <h3 className="text-2xl font-bold text-gray-900 mb-2">Application Submitted!</h3>
                        <p className="text-gray-500 max-w-md mb-8">
                            The loan application for <span className="font-bold text-gray-800">{formData.borrowerType === 'INDIVIDUAL' ? customerDetails?.name : groupDetails?.name}</span> has been successfully queued for approval.
                        </p>
                        <div className="flex gap-4">
                            <button onClick={() => setView('LIST')} className="px-6 py-2 bg-gray-100 text-gray-700 font-bold rounded-lg hover:bg-gray-200">
                                Return to Portfolio
                            </button>
                            <button onClick={() => setView('WIZARD')} className="px-6 py-2 bg-blue-600 text-white font-bold rounded-lg hover:bg-blue-700">
                                Start New Application
                            </button>
                        </div>
                    </div>
                ) : (
                    <>
                        {/* Stepper */}
                        <div className="flex items-center justify-between mb-8 px-4 relative">
                            <div className="absolute left-0 top-1/2 transform -translate-y-1/2 w-full h-1 bg-gray-200 -z-10"></div>
                            {STEPS.map((step) => (
                                <div key={step.id} className="flex flex-col items-center bg-white px-2">
                                    <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-sm border-2 mb-1 transition-colors ${
                                        wizardStep >= step.id 
                                        ? 'bg-blue-600 text-white border-blue-600' 
                                        : 'bg-white text-gray-400 border-gray-300'
                                    }`}>
                                        {step.id < wizardStep ? <Check size={16} /> : step.id}
                                    </div>
                                    <span className={`text-xs font-medium ${wizardStep >= step.id ? 'text-blue-800' : 'text-gray-400'}`}>
                                        {step.label}
                                    </span>
                                </div>
                            ))}
                        </div>

                        <div className="bg-white border border-gray-200 rounded-lg p-8 shadow-sm min-h-[400px] flex flex-col relative">
                            {submissionStatus === 'SUBMITTING' && (
                                <div className="absolute inset-0 bg-white/80 z-50 flex flex-col items-center justify-center rounded-lg">
                                    <Loader2 className="animate-spin text-blue-600 mb-2" size={32} />
                                    <span className="text-blue-700 font-bold">Submitting Application...</span>
                                </div>
                            )}
                            
                            {/* STEP 1: BORROWER */}
                            {wizardStep === 1 && (
                                <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
                                    <h3 className="text-xl font-bold text-gray-900">Select Borrower</h3>
                                    
                                    <div className="flex gap-4 mb-4">
                                        <button 
                                            className={`flex-1 py-3 border rounded-lg flex items-center justify-center gap-2 ${formData.borrowerType === 'INDIVIDUAL' ? 'bg-blue-50 border-blue-500 text-blue-700 font-bold' : 'border-gray-200 text-gray-500'}`}
                                            onClick={() => setFormData({...formData, borrowerType: 'INDIVIDUAL', cif: ''})}
                                        >
                                            <User size={18} /> Individual Client
                                        </button>
                                        <button 
                                            className={`flex-1 py-3 border rounded-lg flex items-center justify-center gap-2 ${formData.borrowerType === 'GROUP' ? 'bg-purple-50 border-purple-500 text-purple-700 font-bold' : 'border-gray-200 text-gray-500'}`}
                                            onClick={() => setFormData({...formData, borrowerType: 'GROUP', cif: ''})}
                                        >
                                            <Users size={18} /> Joint Liability Group
                                        </button>
                                    </div>

                                    <div className="max-w-md">
                                        <label className="block text-sm font-medium text-gray-700 mb-1">{formData.borrowerType === 'INDIVIDUAL' ? 'Customer CIF' : 'Group ID'}</label>
                                        <div className={`flex items-center gap-2 border rounded p-2.5 ${validationError && !customerDetails && !groupDetails ? 'border-red-300 bg-red-50' : 'bg-gray-50 border-gray-300'}`}>
                                            <User size={18} className={validationError ? "text-red-400" : "text-gray-400"} />
                                            <input 
                                                type="text" 
                                                value={formData.cif} 
                                                onChange={e => setFormData({...formData, cif: e.target.value})}
                                                placeholder={formData.borrowerType === 'INDIVIDUAL' ? "e.g. CIF100001" : "e.g. GRP-001"}
                                                className="bg-transparent focus:outline-none w-full text-sm font-medium"
                                                autoFocus
                                            />
                                            {(customerDetails || groupDetails) && (
                                                <span className="text-sm font-bold text-green-600 px-2 bg-green-100 rounded flex items-center gap-1">
                                                    <BadgeCheck size={14}/> Found
                                                </span>
                                            )}
                                        </div>
                                        {customerDetails && (
                                            <div className="mt-4 p-4 bg-blue-50 border border-blue-100 rounded-lg">
                                                <p className="font-bold text-blue-900 text-lg">{customerDetails.name}</p>
                                                <p className="text-sm text-blue-700 mb-3">{customerDetails.email} • {customerDetails.phone}</p>
                                                <div className="flex gap-2">
                                                    <span className={`text-xs px-2 py-1 rounded font-bold border ${customerDetails.riskRating === 'High' ? 'bg-red-100 border-red-200 text-red-700' : 'bg-green-100 border-green-200 text-green-700'}`}>
                                                        Risk: {customerDetails.riskRating}
                                                    </span>
                                                    <span className="text-xs px-2 py-1 rounded font-bold bg-white border border-blue-200 text-blue-700">
                                                        KYC: {customerDetails.kycLevel}
                                                    </span>
                                                </div>
                                            </div>
                                        )}
                                        {groupDetails && (
                                            <div className="mt-4 p-4 bg-purple-50 border border-purple-100 rounded-lg">
                                                <div className="flex justify-between items-start">
                                                    <div>
                                                        <p className="font-bold text-purple-900 text-lg">{groupDetails.name}</p>
                                                        <p className="text-sm text-purple-700 mb-2">Meeting: {groupDetails.meetingDay}</p>
                                                    </div>
                                                    <span className="text-xs px-2 py-1 rounded font-bold bg-white border border-purple-200 text-purple-700">
                                                        {groupDetails.status}
                                                    </span>
                                                </div>
                                                <div className="mt-3 pt-3 border-t border-purple-200">
                                                    <p className="text-xs font-bold text-purple-800 uppercase mb-2">Active Members</p>
                                                    <div className="flex flex-wrap gap-2">
                                                        {groupDetails.members.slice(0,5).map(mId => {
                                                            const mem = customers.find(c => c.id === mId);
                                                            return (
                                                                <span key={mId} className="text-xs bg-white px-2 py-1 rounded border border-purple-100 text-gray-600">
                                                                    {mem?.name || mId}
                                                                </span>
                                                            );
                                                        })}
                                                        {groupDetails.members.length > 5 && (
                                                            <span className="text-xs text-purple-500 italic flex items-center">+{groupDetails.members.length - 5} more</span>
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                        )}
                                        {!customerDetails && !groupDetails && (
                                            <p className="text-xs text-gray-500 mt-2">Enter a valid ID to proceed.</p>
                                        )}
                                    </div>
                                </div>
                            )}

                            {/* STEP 2: TERMS */}
                            {wizardStep === 2 && (
                                <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
                                    <h3 className="text-xl font-bold text-gray-900">Loan Terms</h3>
                                    
                                    {formData.borrowerType === 'GROUP' && groupDetails && (
                                        <div className="bg-purple-50 border border-purple-200 rounded-lg p-4">
                                            <h4 className="font-bold text-purple-900 text-sm mb-3 flex items-center gap-2"><Users size={16}/> Group Member Allocation</h4>
                                            <div className="grid grid-cols-2 gap-3 mb-2">
                                                {groupDetails.members.map(memberId => {
                                                    const cust = customers.find(c => c.id === memberId);
                                                    return (
                                                        <div key={memberId} className="flex justify-between items-center bg-white p-2 rounded border border-purple-100">
                                                            <div className="text-xs">
                                                                <div className="font-bold text-gray-700">{cust?.name}</div>
                                                                <div className="text-gray-400">{memberId}</div>
                                                            </div>
                                                            <input 
                                                                type="number" 
                                                                placeholder="0.00"
                                                                className="w-24 border rounded p-1 text-right text-sm font-mono focus:ring-1 focus:ring-purple-500 outline-none"
                                                                value={memberSplits[memberId] || ''}
                                                                onChange={e => handleSplitChange(memberId, e.target.value)}
                                                            />
                                                        </div>
                                                    )
                                                })}
                                            </div>
                                            <div className="text-right text-xs font-bold text-purple-700 mt-2">
                                                Total Allocated: {Object.values(memberSplits).reduce((a,b)=>a+b, 0).toLocaleString()} GHS
                                            </div>
                                        </div>
                                    )}

                                    <div className="grid grid-cols-2 gap-6">
                                        <div className="col-span-2">
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Loan Product</label>
                                            <select 
                                                value={formData.productCode}
                                                onChange={e => handleProductChange(e.target.value)}
                                                className="w-full border border-gray-300 rounded p-3 text-sm bg-white focus:ring-2 focus:ring-blue-500 outline-none"
                                            >
                                                {loanProducts.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                                            </select>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Principal (GHS)</label>
                                            <input 
                                                type="number" 
                                                value={formData.principal}
                                                onChange={e => setFormData({...formData, principal: Number(e.target.value)})}
                                                className={`w-full border border-gray-300 rounded p-3 text-lg font-mono font-bold focus:ring-2 focus:ring-blue-500 outline-none ${formData.borrowerType === 'GROUP' ? 'bg-gray-100 text-gray-500 cursor-not-allowed' : ''}`}
                                                readOnly={formData.borrowerType === 'GROUP'} // Auto-calculated for groups
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Interest Rate (%)</label>
                                            <input 
                                                type="number" 
                                                value={formData.rate}
                                                onChange={e => setFormData({...formData, rate: Number(e.target.value)})}
                                                className="w-full border border-gray-300 rounded p-3 bg-gray-50 text-gray-600 focus:outline-none"
                                                readOnly // Rate usually fixed by product
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Term (Months)</label>
                                            <input 
                                                type="number" 
                                                value={formData.term}
                                                onChange={e => setFormData({...formData, term: Number(e.target.value)})}
                                                className="w-full border border-gray-300 rounded p-3 text-lg font-mono focus:ring-2 focus:ring-blue-500 outline-none"
                                            />
                                        </div>
                                    </div>

                                    {/* Live Amortization Preview */}
                                    {schedule.length > 0 && (
                                        <div className="bg-blue-50 border border-blue-100 rounded-lg p-4 mt-6">
                                            <h4 className="font-bold text-blue-900 mb-3 flex items-center gap-2">
                                                <Calculator size={16}/> Payment Preview
                                            </h4>
                                            <div className="grid grid-cols-3 gap-4 mb-4">
                                                <div className="bg-white p-3 rounded border border-blue-100 shadow-sm">
                                                    <span className="text-xs text-gray-500 uppercase font-bold">Monthly Installment</span>
                                                    <span className="block text-lg font-mono font-bold text-blue-700">
                                                        {schedule[0]?.total.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}
                                                    </span>
                                                </div>
                                                <div className="bg-white p-3 rounded border border-blue-100 shadow-sm">
                                                    <span className="text-xs text-gray-500 uppercase font-bold">Total Interest</span>
                                                    <span className="block text-lg font-mono font-bold text-blue-700">
                                                        {schedule.reduce((acc, row) => acc + row.interest, 0).toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}
                                                    </span>
                                                </div>
                                                <div className="bg-white p-3 rounded border border-blue-100 shadow-sm">
                                                    <span className="text-xs text-gray-500 uppercase font-bold">Total Repayment</span>
                                                    <span className="block text-lg font-mono font-bold text-blue-700">
                                                        {schedule.reduce((acc, row) => acc + row.total, 0).toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}
                                                    </span>
                                                </div>
                                            </div>
                                            
                                            <details className="text-sm">
                                                <summary className="cursor-pointer text-blue-600 font-medium hover:text-blue-800">View Full Schedule</summary>
                                                <div className="mt-3 max-h-48 overflow-y-auto bg-white rounded border border-gray-200">
                                                    <table className="w-full text-xs text-right">
                                                        <thead className="bg-gray-50 text-gray-500 sticky top-0">
                                                            <tr>
                                                                <th className="p-2 text-center">#</th>
                                                                <th className="p-2">Principal</th>
                                                                <th className="p-2">Interest</th>
                                                                <th className="p-2">Total</th>
                                                                <th className="p-2">Balance</th>
                                                            </tr>
                                                        </thead>
                                                        <tbody>
                                                            {schedule.map(row => (
                                                                <tr key={row.period} className="border-t border-gray-50">
                                                                    <td className="p-2 text-center text-gray-400">{row.period}</td>
                                                                    <td className="p-2">{row.principal.toFixed(2)}</td>
                                                                    <td className="p-2">{row.interest.toFixed(2)}</td>
                                                                    <td className="p-2 font-bold">{row.total.toFixed(2)}</td>
                                                                    <td className="p-2 text-gray-500">{row.balance.toFixed(2)}</td>
                                                                </tr>
                                                            ))}
                                                        </tbody>
                                                    </table>
                                                </div>
                                            </details>
                                        </div>
                                    )}
                                </div>
                            )}

                            {/* STEP 3: COLLATERAL & DOCS */}
                            {wizardStep === 3 && (
                                <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
                                    <h3 className="text-xl font-bold text-gray-900">Collateral & Documents</h3>
                                    
                                    <div className="grid grid-cols-2 gap-6">
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Collateral Type</label>
                                            <select 
                                                value={formData.collateralType}
                                                onChange={e => setFormData({...formData, collateralType: e.target.value})}
                                                className="w-full border border-gray-300 rounded p-2.5 text-sm bg-white"
                                            >
                                                <option value="Unsecured">Unsecured</option>
                                                <option value="Cash Lien">Cash Lien</option>
                                                <option value="Landed Property">Landed Property</option>
                                                <option value="Vehicle">Vehicle</option>
                                                <option value="Guarantor">Guarantor</option>
                                                {formData.borrowerType === 'GROUP' && <option value="Group Guarantee">Group Guarantee</option>}
                                                {formData.borrowerType === 'GROUP' && <option value="Group Savings Lien">Group Savings Lien</option>}
                                            </select>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Estimated Value (GHS)</label>
                                            <input 
                                                type="text" 
                                                placeholder="Optional"
                                                value={formData.collateralValue}
                                                onChange={e => setFormData({...formData, collateralValue: e.target.value})}
                                                className="w-full border border-gray-300 rounded p-2.5 text-sm"
                                            />
                                        </div>
                                    </div>

                                    <div className="border-t pt-4">
                                        <div className="flex justify-between items-end mb-3">
                                            <h4 className="text-sm font-bold text-gray-700">Required Documents</h4>
                                            <span className="text-xs text-gray-500">{uploadedDocs.length} / {allPossibleDocs.length} Uploaded</span>
                                        </div>
                                        <div className="space-y-3">
                                            {allPossibleDocs.map((doc) => {
                                                const isRequired = requiredDocs.includes(doc);
                                                const isUploaded = uploadedDocs.includes(doc);
                                                return (
                                                    <div key={doc} className={`flex items-center justify-between p-3 border rounded-lg transition-colors ${
                                                        isUploaded ? 'bg-green-50 border-green-200' : 
                                                        isRequired ? 'bg-white border-orange-200 hover:bg-orange-50' : 
                                                        'bg-white border-gray-200 hover:bg-gray-50'
                                                    }`}>
                                                        <div className="flex items-center gap-3">
                                                            <div className={`p-2 rounded-full ${isUploaded ? 'bg-green-100 text-green-600' : isRequired ? 'bg-orange-100 text-orange-500' : 'bg-gray-100 text-gray-400'}`}>
                                                                <File size={18} />
                                                            </div>
                                                            <div>
                                                                <span className={`text-sm block ${isUploaded ? 'text-gray-900 font-bold' : 'text-gray-700'}`}>{doc}</span>
                                                                {isRequired && !isUploaded && <span className="text-[10px] text-orange-600 font-bold uppercase tracking-wide">Required</span>}
                                                                {!isRequired && !isUploaded && <span className="text-[10px] text-gray-400 uppercase tracking-wide">Optional</span>}
                                                            </div>
                                                        </div>
                                                        {isUploaded ? (
                                                            <span className="flex items-center gap-1 text-xs font-bold text-green-600"><Check size={14}/> Uploaded</span>
                                                        ) : (
                                                            <button 
                                                                onClick={() => handleSimulateUpload(doc)}
                                                                className="text-xs flex items-center gap-1 text-blue-600 hover:text-blue-800 font-medium px-3 py-1.5 border border-blue-200 rounded hover:bg-blue-50"
                                                            >
                                                                <Upload size={14} /> Upload
                                                            </button>
                                                        )}
                                                    </div>
                                                );
                                            })}
                                        </div>
                                    </div>
                                </div>
                            )}

                            {/* STEP 4: REVIEW */}
                            {wizardStep === 4 && (
                                <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
                                    <h3 className="text-xl font-bold text-gray-900">Review Application</h3>
                                    
                                    <div className="grid grid-cols-2 gap-4 text-sm">
                                        <div className="bg-gray-50 p-4 rounded-lg">
                                            <span className="block text-gray-500 text-xs uppercase mb-1">Borrower</span>
                                            <span className="font-bold text-gray-900 block">{formData.borrowerType === 'INDIVIDUAL' ? customerDetails?.name : groupDetails?.name}</span>
                                            <span className="text-gray-500">{formData.cif}</span>
                                        </div>
                                        <div className="bg-gray-50 p-4 rounded-lg">
                                            <span className="block text-gray-500 text-xs uppercase mb-1">Product</span>
                                            <span className="font-bold text-gray-900 block">{loanProducts.find(p => p.id === formData.productCode)?.name}</span>
                                            <span className="text-gray-500">{formData.rate}% Interest</span>
                                        </div>
                                        <div className="bg-blue-50 p-4 rounded-lg border border-blue-100">
                                            <span className="block text-blue-500 text-xs uppercase mb-1">Principal</span>
                                            <span className="font-bold text-blue-900 block text-lg">{formData.principal.toLocaleString()} GHS</span>
                                        </div>
                                        <div className="bg-blue-50 p-4 rounded-lg border border-blue-100">
                                            <span className="block text-blue-500 text-xs uppercase mb-1">Total Payment</span>
                                            <span className="font-bold text-blue-900 block text-lg">
                                                {schedule.reduce((acc, curr) => acc + curr.total, 0).toLocaleString(undefined, {maximumFractionDigits: 2})} GHS
                                            </span>
                                        </div>
                                    </div>

                                    {/* Mini Schedule Preview */}
                                    <div className="border rounded-lg overflow-hidden">
                                        <div className="bg-gray-100 px-4 py-2 text-xs font-bold text-gray-600 uppercase">Amortization Preview</div>
                                        <div className="max-h-40 overflow-y-auto">
                                            <table className="w-full text-xs text-right">
                                                <thead className="bg-gray-50 text-gray-500">
                                                    <tr>
                                                        <th className="p-2 text-left">Period</th>
                                                        <th className="p-2">Principal</th>
                                                        <th className="p-2">Interest</th>
                                                        <th className="p-2">Total</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    {schedule.map(row => (
                                                        <tr key={row.period} className="border-t border-gray-100">
                                                            <td className="p-2 text-left">{row.period}</td>
                                                            <td className="p-2">{row.principal.toFixed(2)}</td>
                                                            <td className="p-2">{row.interest.toFixed(2)}</td>
                                                            <td className="p-2 font-bold">{row.total.toFixed(2)}</td>
                                                        </tr>
                                                    ))}
                                                </tbody>
                                            </table>
                                        </div>
                                    </div>
                                    
                                    <div className="bg-yellow-50 p-3 rounded text-xs text-yellow-800 border border-yellow-200 flex gap-2">
                                        <ShieldAlert size={16} className="shrink-0" />
                                        <span>
                                            By submitting this application, you confirm that all KYC documents have been verified and the borrower meets the eligibility criteria for the selected product.
                                        </span>
                                    </div>
                                </div>
                            )}

                            {/* Footer Actions */}
                            <div className="mt-auto pt-6 border-t border-gray-100 flex justify-between items-center">
                                {validationError && (
                                    <div className="text-red-600 text-sm flex items-center gap-2 font-medium">
                                        <AlertCircle size={16} /> {validationError}
                                    </div>
                                )}
                                {!validationError && <div></div>} {/* Spacer */}
                                
                                <div className="flex gap-3">
                                    <button 
                                        onClick={wizardStep === 1 ? () => setView('LIST') : handleBackStep}
                                        className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm font-medium flex items-center gap-1"
                                    >
                                        {wizardStep === 1 ? 'Cancel' : <><ChevronLeft size={16}/> Back</>}
                                    </button>
                                    
                                    {wizardStep < 4 ? (
                                        <button 
                                            onClick={handleNextStep}
                                            className="px-6 py-2 bg-blue-600 text-white rounded-lg text-sm font-bold hover:bg-blue-700 flex items-center gap-2"
                                        >
                                            Next Step <ChevronRight size={16} />
                                        </button>
                                    ) : (
                                        <button 
                                            onClick={saveLoan}
                                            className="px-6 py-2 bg-green-600 text-white rounded-lg text-sm font-bold hover:bg-green-700 flex items-center gap-2 shadow-sm"
                                        >
                                            <CheckCircle size={16} /> Submit Application
                                        </button>
                                    )}
                                </div>
                            </div>

                        </div>
                    </>
                )}
            </div>
        )}

        {/* VIEW: DETAILS (EXISTING LOAN) */}
        {view === 'DETAILS' && selectedLoan && (
            <div className="space-y-6">
                <div className="flex justify-between items-start bg-blue-50 p-6 rounded-lg border border-blue-100">
                    <div>
                        <div className="flex items-center gap-2 mb-2">
                            <span className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs font-bold rounded">{selectedLoan.id}</span>
                            <span className={`px-2 py-0.5 rounded text-xs font-bold border ${
                                selectedLoan.status === 'ACTIVE' ? 'bg-green-100 text-green-700 border-green-200' : 'bg-gray-100 text-gray-700 border-gray-200'
                            }`}>{selectedLoan.status}</span>
                        </div>
                        <h3 className="text-xl font-bold text-blue-900 mb-1">{selectedLoan.productName}</h3>
                        <div className="flex gap-4 text-sm text-blue-800">
                            {selectedLoan.groupId ? 
                                <span className="flex items-center gap-1"><Users size={14}/> Group: {selectedLoan.groupId}</span> :
                                <span className="flex items-center gap-1"><User size={14}/> Client: {selectedLoan.cif}</span>
                            }
                            <span className="flex items-center gap-1"><Calendar size={14}/> {selectedLoan.termMonths} Months</span>
                            <span className="flex items-center gap-1"><FileText size={14}/> {selectedLoan.rate}% p.a.</span>
                        </div>
                    </div>
                    <div className="text-right">
                         <span className="block text-sm text-blue-600 uppercase font-bold">Outstanding Balance</span>
                         <span className="text-3xl font-mono text-blue-900 font-bold">
                             {selectedLoan.outstandingBalance.toLocaleString()}
                        </span>
                        <span className="text-sm text-blue-600 ml-1">GHS</span>
                    </div>
                </div>

                {/* Group Repayment Section - Only for Group Loans */}
                {selectedLoan.groupId && groups.find(g => g.id === selectedLoan.groupId) && (
                    <div className="bg-purple-50 border border-purple-200 rounded-lg p-6">
                        <div className="flex justify-between items-center mb-4">
                            <h4 className="font-bold text-purple-900 flex items-center gap-2">
                                <Users size={18} /> Group Repayment Console
                            </h4>
                            <div className="text-sm font-bold text-purple-700">
                                Total Collection: GHS {totalContribution.toLocaleString()}
                            </div>
                        </div>
                        
                        {/* Member Breakdown */}
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 mb-4">
                            {groups.find(g => g.id === selectedLoan.groupId)?.members.map(mId => {
                                const mem = customers.find(c => c.id === mId);
                                // Determine expected amount based on split ratio for the current installment
                                const split = selectedLoan.memberSplits?.[mId] || 0;
                                const ratio = selectedLoan.principal > 0 ? split / selectedLoan.principal : 0;
                                
                                // Find current due schedule
                                const currentSchedule = schedule.find(s => s.status === 'DUE') || schedule[0];
                                const expectedInstallment = currentSchedule ? currentSchedule.total * ratio : 0;
                                const currentPaid = memberContributions[mId] || 0;
                                const isFulfilled = currentPaid >= expectedInstallment;

                                return (
                                    <div key={mId} className={`bg-white p-3 rounded border flex flex-col gap-2 ${isFulfilled ? 'border-green-300 ring-1 ring-green-100' : 'border-purple-100'}`}>
                                        <div className="flex justify-between items-start">
                                            <div>
                                                <div className="font-bold text-gray-800 text-sm truncate">{mem?.name || mId}</div>
                                                <div className="text-[10px] text-gray-500">Loan Share: {split.toLocaleString()}</div>
                                            </div>
                                            {isFulfilled && <CheckCircle size={16} className="text-green-500" />}
                                        </div>
                                        
                                        <div className="flex justify-between items-center mt-1">
                                            <span className="text-[10px] text-gray-500">Due: <span className="font-bold">{expectedInstallment.toFixed(2)}</span></span>
                                            <input 
                                                type="number" 
                                                placeholder="0.00"
                                                className="w-24 border border-gray-300 rounded p-1 text-right text-sm font-mono focus:ring-2 focus:ring-purple-500 outline-none"
                                                value={memberContributions[mId] || ''}
                                                onChange={(e) => handleContributionChange(mId, e.target.value)}
                                            />
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                        
                        <div className="flex justify-end gap-3">
                            <button 
                                onClick={submitGroupRepayment}
                                disabled={totalContribution <= 0}
                                className="px-4 py-2 bg-purple-600 text-white rounded font-bold hover:bg-purple-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                            >
                                <Save size={16}/> Post Repayment
                            </button>
                        </div>
                    </div>
                )}

                {/* Amortization Schedule */}
                <div className="bg-white border border-gray-200 rounded-lg overflow-hidden shadow-sm">
                    <div className="p-4 bg-gray-50 border-b border-gray-200 flex justify-between items-center">
                        <h4 className="font-bold text-gray-700">Repayment Schedule</h4>
                    </div>
                    <div className="max-h-96 overflow-y-auto">
                        <table className="w-full text-sm text-right">
                            <thead className="bg-gray-50 text-gray-500 font-medium text-xs sticky top-0 shadow-sm">
                                <tr>
                                    <th className="p-3 text-center">#</th>
                                    <th className="p-3 text-center">Due Date</th>
                                    <th className="p-3">Principal</th>
                                    <th className="p-3">Interest</th>
                                    <th className="p-3">Total Installment</th>
                                    <th className="p-3">Balance</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-gray-100 font-mono">
                                {schedule.map((row) => (
                                    <tr key={row.period} className="hover:bg-gray-50">
                                        <td className="p-3 text-center text-gray-500">{row.period}</td>
                                        <td className="p-3 text-center text-gray-600">{row.dueDate}</td>
                                        <td className="p-3">{row.principal.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</td>
                                        <td className="p-3 text-red-600">{row.interest.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</td>
                                        <td className="p-3 font-bold">{row.total.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</td>
                                        <td className="p-3 text-gray-400">{row.balance.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>

                {selectedLoan.status === 'PENDING' && (
                     <div className="flex justify-end gap-3 pt-4 border-t">
                        <button 
                            onClick={() => { onDisburse(selectedLoan.id); setView('LIST'); }} 
                            className="px-6 py-2 bg-green-600 text-white rounded font-medium hover:bg-green-700 flex items-center gap-2"
                        >
                            <CheckCircle size={16} /> Approve & Disburse Funds
                        </button>
                    </div>
                )}
            </div>
        )}
      </div>
    </div>
  );
};

export default LoanEngine;