import { useMemo, useState } from 'react';
import { platformEnhancementService, type ProductSimulationResult } from '../services/platformEnhancementService';

interface ProductLite {
  id: string;
  name: string;
  type: string;
  currency?: string;
  interestRate?: number | null;
  minAmount?: number | null;
  maxAmount?: number | null;
  defaultTerm?: number | null;
  status?: string;
  lifecycleStatus?: string;
  versionNumber?: number;
}

interface Props {
  products: ProductLite[];
}

const money = (amount: number, currency = 'GHS') =>
  new Intl.NumberFormat('en-GH', { style: 'currency', currency }).format(amount || 0);

export default function ProductFactoryHub({ products }: Props) {
  const [productId, setProductId] = useState(products[0]?.id ?? '');
  const [amount, setAmount] = useState(1000);
  const [termMonths, setTermMonths] = useState(12);
  const [lifecycleStatus, setLifecycleStatus] = useState('PENDING_APPROVAL');
  const [simulation, setSimulation] = useState<ProductSimulationResult | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const selectedProduct = useMemo(() => products.find((item) => item.id === productId), [productId, products]);

  const runSimulation = async () => {
    if (!productId) return;
    const result = await platformEnhancementService.simulateProduct(productId, { amount, termMonths });
    setSimulation(result);
    setMessage(null);
  };

  const updateLifecycle = async () => {
    if (!productId) return;
    await platformEnhancementService.updateProductLifecycle(productId, {
      lifecycleStatus,
      effectiveFrom: lifecycleStatus === 'ACTIVE' ? new Date().toISOString() : null,
      notes: `Lifecycle moved to ${lifecycleStatus} from product factory.`,
    });
    setMessage(`Lifecycle updated to ${lifecycleStatus}. Refresh the product workspace to see the latest state.`);
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Product Factory</p>
        <h1 className="mt-2 text-2xl font-semibold text-slate-950">Lifecycle and simulation workbench</h1>
        <p className="mt-2 text-sm text-slate-600">Move products through governed lifecycle stages and simulate customer outcomes before release.</p>
      </div>

      {message && <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{message}</div>}

      <div className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="text-lg font-semibold text-slate-950">Lifecycle controls</h2>
          <div className="mt-4 space-y-4">
            <div>
              <label className="text-sm font-medium text-slate-700">Product</label>
              <select
                value={productId}
                onChange={(event) => setProductId(event.target.value)}
                className="mt-2 w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none focus:border-slate-400"
              >
                {products.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.name} ({product.id})
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-sm font-medium text-slate-700">Lifecycle state</label>
              <select
                value={lifecycleStatus}
                onChange={(event) => setLifecycleStatus(event.target.value)}
                className="mt-2 w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none focus:border-slate-400"
              >
                <option value="DRAFT">DRAFT</option>
                <option value="PENDING_APPROVAL">PENDING_APPROVAL</option>
                <option value="ACTIVE">ACTIVE</option>
                <option value="RETIRED">RETIRED</option>
              </select>
            </div>
            <button
              type="button"
              onClick={() => void updateLifecycle()}
              className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-800"
            >
              Update lifecycle
            </button>
            {selectedProduct && (
              <div className="rounded-2xl border border-slate-200 p-4 text-sm text-slate-600">
                <p className="font-semibold text-slate-900">{selectedProduct.name}</p>
                <p className="mt-2">Type: {selectedProduct.type}</p>
                <p>Current lifecycle: {selectedProduct.lifecycleStatus || 'DRAFT'}</p>
                <p>Version: {selectedProduct.versionNumber || 1}</p>
              </div>
            )}
          </div>
        </div>

        <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="text-lg font-semibold text-slate-950">Simulation sandbox</h2>
          <div className="mt-4 grid gap-4 md:grid-cols-2">
            <div>
              <label className="text-sm font-medium text-slate-700">Amount</label>
              <input
                type="number"
                value={amount}
                onChange={(event) => setAmount(Number(event.target.value))}
                className="mt-2 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-900 outline-none focus:border-slate-400"
              />
            </div>
            <div>
              <label className="text-sm font-medium text-slate-700">Term (months)</label>
              <input
                type="number"
                value={termMonths}
                onChange={(event) => setTermMonths(Number(event.target.value))}
                className="mt-2 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-900 outline-none focus:border-slate-400"
              />
            </div>
          </div>
          <div className="mt-4">
            <button
              type="button"
              onClick={() => void runSimulation()}
              className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-800"
            >
              Run simulation
            </button>
          </div>

          {simulation && (
            <div className="mt-6 rounded-2xl border border-slate-200 p-5">
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Projected interest</p>
                  <p className="mt-2 text-lg font-semibold text-slate-950">{money(simulation.projectedInterest, selectedProduct?.currency || 'GHS')}</p>
                </div>
                <div>
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Projected maturity value</p>
                  <p className="mt-2 text-lg font-semibold text-slate-950">{money(simulation.projectedMaturityValue, selectedProduct?.currency || 'GHS')}</p>
                </div>
              </div>
              {simulation.projectedInstallment != null && (
                <p className="mt-4 text-sm text-slate-600">Projected installment: {money(simulation.projectedInstallment, selectedProduct?.currency || 'GHS')}</p>
              )}
              <p className="mt-4 text-sm text-slate-600">{simulation.summary}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
