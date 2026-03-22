import React, { useMemo, useState } from 'react';
import { Package, Plus, Save, Edit2, Settings, Users, CalendarDays, ShieldCheck, Search } from 'lucide-react';
import { Product } from '../types';

interface ProductDesignerProps {
  products: Product[];
  onCreateProduct: (product: Product) => void;
  onUpdateProduct: (id: string, updates: Partial<Product>) => void;
}

const emptyGroupRules = {
  minMembersRequired: 5,
  maxMembersAllowed: 30,
  requiresCompulsorySavings: false,
  requiresGroupApprovalMeeting: true,
  requiresJointLiability: true,
  allowTopUp: false,
  allowReschedule: true,
  defaultRepaymentFrequency: 'Weekly' as const,
  defaultInterestMethod: 'Flat' as const,
};

const emptyEligibilityRules = {
  requiresKycComplete: true,
  blockOnSevereArrears: true,
  requireCreditBureauCheck: false,
};

const ProductDesigner: React.FC<ProductDesignerProps> = ({ products, onCreateProduct, onUpdateProduct }) => {
  const [selectedType, setSelectedType] = useState<Product['type'] | 'ALL'>('ALL');
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [formData, setFormData] = useState<Partial<Product>>({
    type: 'SAVINGS',
    currency: 'GHS',
    status: 'ACTIVE',
    interestRate: 0,
    minAmount: 0,
    lendingMethodology: 'INDIVIDUAL',
    defaultRepaymentFrequency: 'Monthly',
    allowedRepaymentFrequencies: ['Monthly'],
    groupRules: { ...emptyGroupRules },
    eligibilityRules: { ...emptyEligibilityRules },
  });

  const filteredProducts = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return products.filter((product) => {
      const matchesType = selectedType === 'ALL' || product.type === selectedType;
      const matchesQuery = !query || [product.id, product.name, product.description, product.type, product.lendingMethodology].some((value) => String(value || '').toLowerCase().includes(query));
      return matchesType && matchesQuery;
    });
  }, [products, searchQuery, selectedType]);
  const activeProducts = products.filter((product) => product.status === 'ACTIVE').length;
  const groupEnabledProducts = products.filter((product) => product.type === 'LOAN' && (product.isGroupLoanEnabled || product.lendingMethodology === 'GROUP' || product.lendingMethodology === 'HYBRID')).length;
  const showGroupSection = formData.type === 'LOAN' && (formData.isGroupLoanEnabled || formData.lendingMethodology === 'GROUP' || formData.lendingMethodology === 'HYBRID');

  const handleEdit = (product: Product) => {
    setEditingProduct(product);
    setFormData({
      ...product,
      groupRules: product.groupRules ?? { ...emptyGroupRules },
      eligibilityRules: product.eligibilityRules ?? { ...emptyEligibilityRules },
      allowedRepaymentFrequencies: product.allowedRepaymentFrequencies ?? (product.defaultRepaymentFrequency ? [product.defaultRepaymentFrequency] : ['Monthly']),
    });
    setIsCreating(false);
  };

  const handleCreateNew = () => {
    setEditingProduct(null);
    setFormData({
      id: '',
      name: '',
      description: '',
      type: 'SAVINGS',
      currency: 'GHS',
      interestRate: 0,
      minAmount: 0,
      status: 'ACTIVE',
      interestMethod: 'COMPOUND',
      lendingMethodology: 'INDIVIDUAL',
      defaultRepaymentFrequency: 'Monthly',
      allowedRepaymentFrequencies: ['Monthly'],
      groupRules: { ...emptyGroupRules },
      eligibilityRules: { ...emptyEligibilityRules },
    });
    setIsCreating(true);
  };

  const handleSave = () => {
    if (!formData.id || !formData.name) return;
    const payload = {
      ...formData,
      isGroupLoanEnabled: showGroupSection,
      requiresGroup: showGroupSection,
      supportsWeeklyRepayment: showGroupSection && (formData.allowedRepaymentFrequencies || []).includes('Weekly'),
      minimumGroupSize: formData.groupRules?.minMembersRequired,
      maximumGroupSize: formData.groupRules?.maxMembersAllowed,
      requiresCompulsorySavings: formData.groupRules?.requiresCompulsorySavings,
      requiresGroupApprovalMeeting: formData.groupRules?.requiresGroupApprovalMeeting,
      supportsJointLiability: formData.groupRules?.requiresJointLiability,
      allowTopUpWithinGroup: formData.groupRules?.allowTopUp,
      allowRescheduleWithinGroup: formData.groupRules?.allowReschedule,
      defaultRepaymentFrequency: showGroupSection ? formData.groupRules?.defaultRepaymentFrequency : formData.defaultRepaymentFrequency,
    } as Product;

    if (isCreating) onCreateProduct(payload);
    else if (editingProduct) onUpdateProduct(editingProduct.id, payload);

    setIsCreating(false);
    setEditingProduct(null);
  };

  return (
    <div className="flex h-full bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <div className="w-1/3 border-r border-gray-200 flex flex-col bg-stone-50">
        <div className="p-4 border-b border-gray-200 bg-white">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-bold text-gray-800 flex items-center gap-2"><Package className="text-amber-700" /> Product Registry</h2>
            <button onClick={handleCreateNew} className="p-2 bg-amber-600 text-white rounded-lg hover:bg-amber-700 transition-colors"><Plus size={16} /></button>
          </div>
          <div className="grid grid-cols-2 gap-2 mb-4">
            <div className="rounded-xl border border-emerald-100 bg-emerald-50 px-3 py-2">
              <p className="text-[10px] uppercase tracking-[0.18em] text-emerald-700">Active</p>
              <p className="mt-1 text-lg font-bold text-slate-900">{activeProducts}</p>
            </div>
            <div className="rounded-xl border border-blue-100 bg-blue-50 px-3 py-2">
              <p className="text-[10px] uppercase tracking-[0.18em] text-blue-700">Group-ready</p>
              <p className="mt-1 text-lg font-bold text-slate-900">{groupEnabledProducts}</p>
            </div>
          </div>
          <label className="relative block mb-4">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
            <input
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search code, name, type, or methodology"
              className="w-full rounded-xl border border-gray-200 bg-stone-50 py-2.5 pl-10 pr-3 text-sm text-slate-900 outline-none transition focus:border-amber-400 focus:bg-white"
            />
          </label>
          <div className="flex gap-2 overflow-x-auto pb-2">
            {['ALL', 'SAVINGS', 'CURRENT', 'LOAN', 'FIXED_DEPOSIT'].map(type => (
              <button key={type} onClick={() => setSelectedType(type as any)} className={`px-3 py-1 text-xs font-bold rounded-full whitespace-nowrap ${selectedType === type ? 'bg-amber-100 text-amber-800 border border-amber-200' : 'bg-white border border-gray-300 text-gray-600 hover:bg-gray-100'}`}>
                {type.replace('_', ' ')}
              </button>
            ))}
          </div>
        </div>
        <div className="flex-1 overflow-y-auto p-2 space-y-2">
          {filteredProducts.map(product => (
            <button key={product.id} onClick={() => handleEdit(product)} className={`w-full text-left p-3 rounded-lg border transition-all ${editingProduct?.id === product.id ? 'bg-amber-50 border-amber-300 shadow-sm' : 'bg-white border-gray-200 hover:border-amber-200 hover:shadow-sm'}`}>
              <div className="flex justify-between items-start mb-1">
                <span className="font-bold text-gray-800 text-sm">{product.name}</span>
                <span className={`text-[10px] px-1.5 py-0.5 rounded font-bold ${product.status === 'ACTIVE' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>{product.status}</span>
              </div>
              <div className="flex justify-between items-center text-xs text-gray-500">
                <span className="font-mono bg-gray-100 px-1 rounded">{product.id}</span>
                <span>{product.lendingMethodology || product.type}</span>
              </div>
            </button>
          ))}
        </div>
      </div>

      <div className="flex-1 bg-white flex flex-col">
        {(editingProduct || isCreating) ? (
          <>
            <div className="p-6 border-b border-gray-200 flex justify-between items-center bg-stone-50">
              <div>
                <h3 className="text-xl font-bold text-gray-900 flex items-center gap-2">{isCreating ? <Plus size={20}/> : <Edit2 size={20}/>}{isCreating ? 'Define New Product' : `Edit ${formData.name}`}</h3>
                <p className="text-sm text-gray-500 mt-1">Loan products can now carry group-lending methodology and policy rules directly from Product Designer.</p>
              </div>
              <button onClick={handleSave} className="px-6 py-2 bg-gradient-to-r from-emerald-600 to-green-700 text-white font-bold rounded-lg hover:from-emerald-700 hover:to-green-800 shadow-md flex items-center gap-2"><Save size={18} /> Save Product</button>
            </div>

            <div className="flex-1 overflow-y-auto p-8">
              <div className="grid grid-cols-2 gap-6 max-w-5xl">
                <section className="col-span-2 bg-amber-50 p-4 rounded-lg border border-amber-100">
                  <h4 className="text-sm font-bold text-amber-900 uppercase mb-3 flex items-center gap-2"><Settings size={14}/> General Configuration</h4>
                  <div className="grid grid-cols-3 gap-4">
                    <input value={formData.id || ''} disabled={!isCreating} onChange={e => setFormData({ ...formData, id: e.target.value })} className="w-full p-2 border border-gray-300 rounded text-sm font-mono disabled:bg-gray-100" placeholder="Product Code" />
                    <input value={formData.name || ''} onChange={e => setFormData({ ...formData, name: e.target.value })} className="col-span-2 w-full p-2 border border-gray-300 rounded text-sm" placeholder="Product Name" />
                    <input value={formData.description || ''} onChange={e => setFormData({ ...formData, description: e.target.value })} className="col-span-3 w-full p-2 border border-gray-300 rounded text-sm" placeholder="Description" />
                    <select value={formData.type || 'SAVINGS'} onChange={e => setFormData({ ...formData, type: e.target.value as Product['type'] })} className="w-full p-2 border border-gray-300 rounded text-sm bg-white">
                      <option value="SAVINGS">Savings</option><option value="CURRENT">Current</option><option value="FIXED_DEPOSIT">Fixed Deposit</option><option value="LOAN">Loan</option>
                    </select>
                    <select value={formData.currency || 'GHS'} onChange={e => setFormData({ ...formData, currency: e.target.value as Product['currency'] })} className="w-full p-2 border border-gray-300 rounded text-sm bg-white"><option value="GHS">GHS</option><option value="USD">USD</option></select>
                    <select value={formData.status || 'ACTIVE'} onChange={e => setFormData({ ...formData, status: e.target.value as Product['status'] })} className="w-full p-2 border border-gray-300 rounded text-sm bg-white"><option value="ACTIVE">Active</option><option value="RETIRED">Retired</option></select>
                  </div>
                </section>

                <section className="bg-white p-4 rounded-lg border border-gray-200">
                  <h4 className="text-sm font-bold text-gray-800 uppercase mb-3">Interest Configuration</h4>
                  <div className="space-y-3">
                    <input type="number" value={formData.interestRate || 0} onChange={e => setFormData({ ...formData, interestRate: Number(e.target.value) })} className="w-full p-2 border border-gray-300 rounded text-sm" placeholder="Interest Rate" />
                    <select value={formData.interestMethod || 'NONE'} onChange={e => setFormData({ ...formData, interestMethod: e.target.value as any })} className="w-full p-2 border border-gray-300 rounded text-sm bg-white"><option value="NONE">None</option><option value="COMPOUND">Compound</option><option value="FLAT">Flat</option><option value="REDUCING_BALANCE">Reducing Balance</option></select>
                  </div>
                </section>

                <section className="bg-white p-4 rounded-lg border border-gray-200">
                  <h4 className="text-sm font-bold text-gray-800 uppercase mb-3">Limits & Terms</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <input type="number" value={formData.minAmount || 0} onChange={e => setFormData({ ...formData, minAmount: Number(e.target.value) })} className="w-full p-2 border border-gray-300 rounded text-sm" placeholder="Min Amount" />
                    <input type="number" value={formData.maxAmount || 0} onChange={e => setFormData({ ...formData, maxAmount: Number(e.target.value) })} className="w-full p-2 border border-gray-300 rounded text-sm" placeholder="Max Amount" />
                    <input type="number" value={formData.minTerm || 0} onChange={e => setFormData({ ...formData, minTerm: Number(e.target.value) })} className="w-full p-2 border border-gray-300 rounded text-sm" placeholder="Min Term" />
                    <input type="number" value={formData.maxTerm || 0} onChange={e => setFormData({ ...formData, maxTerm: Number(e.target.value) })} className="w-full p-2 border border-gray-300 rounded text-sm" placeholder="Max Term" />
                  </div>
                </section>

                {formData.type === 'LOAN' && (
                  <section className="col-span-2 bg-slate-50 p-4 rounded-lg border border-slate-200">
                    <h4 className="text-sm font-bold text-slate-900 uppercase mb-3 flex items-center gap-2"><Users size={14}/> Lending Methodology</h4>
                    <div className="grid grid-cols-3 gap-4">
                      <select value={formData.lendingMethodology || 'INDIVIDUAL'} onChange={e => setFormData({ ...formData, lendingMethodology: e.target.value as any, isGroupLoanEnabled: e.target.value !== 'INDIVIDUAL' })} className="w-full p-2 border border-gray-300 rounded text-sm bg-white"><option value="INDIVIDUAL">Individual</option><option value="GROUP">Group</option><option value="HYBRID">Hybrid</option></select>
                      <select value={(formData.allowedRepaymentFrequencies || ['Monthly']).join(',')} onChange={e => setFormData({ ...formData, allowedRepaymentFrequencies: e.target.value === 'Weekly,Monthly' ? ['Weekly', 'Monthly'] : [e.target.value as 'Weekly' | 'Monthly'] })} className="w-full p-2 border border-gray-300 rounded text-sm bg-white"><option value="Monthly">Monthly only</option><option value="Weekly">Weekly only</option><option value="Weekly,Monthly">Weekly and Monthly</option></select>
                      <label className="flex items-center gap-2 text-sm font-medium"><input type="checkbox" checked={!!formData.requiresCenter} onChange={e => setFormData({ ...formData, requiresCenter: e.target.checked })} /> Requires center</label>
                    </div>
                  </section>
                )}

                {showGroupSection && (
                  <>
                    <section className="col-span-2 bg-emerald-50 p-4 rounded-lg border border-emerald-100">
                      <h4 className="text-sm font-bold text-emerald-900 uppercase mb-3 flex items-center gap-2"><CalendarDays size={14}/> Group Lending Rules</h4>
                      <div className="grid grid-cols-4 gap-3 text-sm">
                        <input type="number" value={formData.groupRules?.minMembersRequired || 5} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, minMembersRequired: Number(e.target.value) } })} className="p-2 border border-gray-300 rounded" placeholder="Min members" />
                        <input type="number" value={formData.groupRules?.maxMembersAllowed || 30} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, maxMembersAllowed: Number(e.target.value) } })} className="p-2 border border-gray-300 rounded" placeholder="Max members" />
                        <input type="number" value={formData.groupRules?.maxCycleNumber || 5} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, maxCycleNumber: Number(e.target.value) } })} className="p-2 border border-gray-300 rounded" placeholder="Max cycle" />
                        <select value={formData.groupRules?.defaultRepaymentFrequency || 'Weekly'} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, defaultRepaymentFrequency: e.target.value as any } })} className="p-2 border border-gray-300 rounded bg-white"><option value="Weekly">Weekly</option><option value="Monthly">Monthly</option></select>
                        <label className="flex items-center gap-2"><input type="checkbox" checked={!!formData.groupRules?.requiresJointLiability} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, requiresJointLiability: e.target.checked } })} /> Joint liability</label>
                        <label className="flex items-center gap-2"><input type="checkbox" checked={!!formData.groupRules?.requiresCompulsorySavings} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, requiresCompulsorySavings: e.target.checked } })} /> Compulsory savings</label>
                        <label className="flex items-center gap-2"><input type="checkbox" checked={!!formData.groupRules?.requiresGroupApprovalMeeting} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, requiresGroupApprovalMeeting: e.target.checked } })} /> Approval meeting</label>
                        <label className="flex items-center gap-2"><input type="checkbox" checked={!!formData.groupRules?.allowReschedule} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, allowReschedule: e.target.checked } })} /> Allow reschedule</label>
                      </div>
                    </section>

                    <section className="col-span-2 bg-sky-50 p-4 rounded-lg border border-sky-100">
                      <h4 className="text-sm font-bold text-sky-900 uppercase mb-3 flex items-center gap-2"><ShieldCheck size={14}/> Eligibility & Controls</h4>
                      <div className="grid grid-cols-3 gap-3 text-sm">
                        <label className="flex items-center gap-2"><input type="checkbox" checked={!!formData.eligibilityRules?.requiresKycComplete} onChange={e => setFormData({ ...formData, eligibilityRules: { ...formData.eligibilityRules!, requiresKycComplete: e.target.checked } })} /> KYC complete</label>
                        <label className="flex items-center gap-2"><input type="checkbox" checked={!!formData.eligibilityRules?.blockOnSevereArrears} onChange={e => setFormData({ ...formData, eligibilityRules: { ...formData.eligibilityRules!, blockOnSevereArrears: e.target.checked } })} /> Block severe arrears</label>
                        <label className="flex items-center gap-2"><input type="checkbox" checked={!!formData.eligibilityRules?.requireCreditBureauCheck} onChange={e => setFormData({ ...formData, eligibilityRules: { ...formData.eligibilityRules!, requireCreditBureauCheck: e.target.checked } })} /> Credit bureau check</label>
                        <input type="number" value={formData.eligibilityRules?.maxAllowedExposure || 0} onChange={e => setFormData({ ...formData, eligibilityRules: { ...formData.eligibilityRules!, maxAllowedExposure: Number(e.target.value) } })} className="p-2 border border-gray-300 rounded" placeholder="Max exposure" />
                        <input type="number" value={formData.eligibilityRules?.minimumCreditScore || 0} onChange={e => setFormData({ ...formData, eligibilityRules: { ...formData.eligibilityRules!, minimumCreditScore: Number(e.target.value) } })} className="p-2 border border-gray-300 rounded" placeholder="Min credit score" />
                        <input value={formData.groupRules?.allocationOrderJson || '["Penalty","Fees","Interest","Principal","Savings"]'} onChange={e => setFormData({ ...formData, groupRules: { ...formData.groupRules!, allocationOrderJson: e.target.value } })} className="p-2 border border-gray-300 rounded col-span-1" placeholder="Allocation order JSON" />
                      </div>
                    </section>
                  </>
                )}
              </div>
            </div>
          </>
        ) : (
          <div className="h-full grid place-items-center text-gray-500 p-10">
            <div className="max-w-md rounded-3xl border border-dashed border-slate-300 bg-stone-50 p-8 text-center">
              <Package className="mx-auto mb-4 h-10 w-10 text-amber-700" />
              <p className="text-lg font-semibold text-slate-900">Select a product or create a new one.</p>
              <p className="mt-2 text-sm text-slate-500">Use the registry on the left to open an existing product, or create a new one with group-lending rules inside the same designer.</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ProductDesigner;

