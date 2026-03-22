import React, { useState, useEffect } from 'react';
import { Vault, DollarSign, TrendingUp, TrendingDown, RefreshCw, Clock } from 'lucide-react';

interface BranchVault {
  id: string;
  branchId: string;
  branchCode: string;
  branchName: string;
  currency: string;
  cashOnHand: number;
  vaultLimit?: number;
  minBalance?: number;
  lastCountDate?: string;
  lastCountBy?: string;
  lastCountByName?: string;
  updatedAt: string;
}

export function VaultManagement() {
  const [vaults, setVaults] = useState<BranchVault[]>([]);
  const [loading, setLoading] = useState(true);
  const [showTransactionForm, setShowTransactionForm] = useState(false);
  const [showCountForm, setShowCountForm] = useState(false);
  const [selectedVault, setSelectedVault] = useState<BranchVault | null>(null);
  const [transactionForm, setTransactionForm] = useState({
    branchId: '',
    currency: 'GHS',
    amount: '',
    type: 'Deposit',
    reference: '',
    narration: ''
  });
  const [countForm, setCountForm] = useState({
    branchId: '',
    currency: 'GHS',
    amount: ''
  });

  useEffect(() => {
    loadVaults();
  }, []);

  const loadVaults = async () => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/vault', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        setVaults(await res.json());
      }
    } catch (error) {
      console.error('Failed to load vaults:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleTransactionSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/vault/transaction', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          ...transactionForm,
          amount: parseFloat(transactionForm.amount)
        })
      });

      if (res.ok) {
        setShowTransactionForm(false);
        setTransactionForm({
          branchId: '',
          currency: 'GHS',
          amount: '',
          type: 'Deposit',
          reference: '',
          narration: ''
        });
        loadVaults();
      } else {
        const error = await res.json();
        alert(error.message || 'Transaction failed');
      }
    } catch (error) {
      console.error('Failed to process transaction:', error);
      alert('Transaction failed');
    }
  };

  const handleCountSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/vault/count', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          ...countForm,
          amount: parseFloat(countForm.amount)
        })
      });

      if (res.ok) {
        setShowCountForm(false);
        setCountForm({ branchId: '', currency: 'GHS', amount: '' });
        loadVaults();
      }
    } catch (error) {
      console.error('Failed to record count:', error);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'GHS') => {
    return `${currency} ${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  };

  const formatDateTime = (date?: string) => {
    if (!date) return 'Never';
    return new Date(date).toLocaleString();
  };

  const getVaultStatus = (vault: BranchVault) => {
    if (vault.minBalance && vault.cashOnHand < vault.minBalance) {
      return { label: 'Below Minimum', color: 'text-red-600 bg-red-50' };
    }
    if (vault.vaultLimit && vault.cashOnHand > vault.vaultLimit * 0.9) {
      return { label: 'Near Limit', color: 'text-yellow-600 bg-yellow-50' };
    }
    return { label: 'Normal', color: 'text-green-600 bg-green-50' };
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Vault className="w-6 h-6 text-indigo-600" />
          <h2 className="text-xl font-semibold text-gray-900">Vault Management</h2>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setShowCountForm(true)}
            className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
          >
            <RefreshCw className="w-5 h-5" />
            Vault Count
          </button>
          <button
            onClick={() => setShowTransactionForm(true)}
            className="flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
          >
            <DollarSign className="w-5 h-5" />
            Transaction
          </button>
        </div>
      </div>

      {showTransactionForm && (
        <form onSubmit={handleTransactionSubmit} className="mb-6 p-4 bg-gray-50 rounded-lg space-y-4">
          <h3 className="font-semibold text-gray-900">Vault Transaction</h3>
          
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Branch</label>
              <select
                value={transactionForm.branchId}
                onChange={(e) => setTransactionForm({ ...transactionForm, branchId: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              >
                <option value="">Select branch...</option>
                {vaults.map((vault) => (
                  <option key={vault.id} value={vault.branchId}>
                    {vault.branchName} ({vault.branchCode})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Type</label>
              <select
                value={transactionForm.type}
                onChange={(e) => setTransactionForm({ ...transactionForm, type: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              >
                <option value="Deposit">Deposit</option>
                <option value="Withdrawal">Withdrawal</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Currency</label>
              <select
                value={transactionForm.currency}
                onChange={(e) => setTransactionForm({ ...transactionForm, currency: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              >
                <option value="GHS">GHS</option>
                <option value="USD">USD</option>
                <option value="EUR">EUR</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Amount</label>
              <input
                type="number"
                step="0.01"
                value={transactionForm.amount}
                onChange={(e) => setTransactionForm({ ...transactionForm, amount: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Reference</label>
              <input
                type="text"
                value={transactionForm.reference}
                onChange={(e) => setTransactionForm({ ...transactionForm, reference: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Narration</label>
              <input
                type="text"
                value={transactionForm.narration}
                onChange={(e) => setTransactionForm({ ...transactionForm, narration: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              />
            </div>
          </div>

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => setShowTransactionForm(false)}
              className="px-4 py-2 text-gray-600 hover:bg-gray-200 rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
            >
              Process
            </button>
          </div>
        </form>
      )}

      {showCountForm && (
        <form onSubmit={handleCountSubmit} className="mb-6 p-4 bg-gray-50 rounded-lg space-y-4">
          <h3 className="font-semibold text-gray-900">Record Vault Count</h3>
          
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Branch</label>
              <select
                value={countForm.branchId}
                onChange={(e) => setCountForm({ ...countForm, branchId: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              >
                <option value="">Select branch...</option>
                {vaults.map((vault) => (
                  <option key={vault.id} value={vault.branchId}>
                    {vault.branchName} ({vault.branchCode})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Currency</label>
              <select
                value={countForm.currency}
                onChange={(e) => setCountForm({ ...countForm, currency: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              >
                <option value="GHS">GHS</option>
                <option value="USD">USD</option>
                <option value="EUR">EUR</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Amount</label>
              <input
                type="number"
                step="0.01"
                value={countForm.amount}
                onChange={(e) => setCountForm({ ...countForm, amount: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              />
            </div>
          </div>

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => setShowCountForm(false)}
              className="px-4 py-2 text-gray-600 hover:bg-gray-200 rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
            >
              Record Count
            </button>
          </div>
        </form>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {loading ? (
          <div className="col-span-full text-center py-8 text-gray-500">Loading vaults...</div>
        ) : vaults.length === 0 ? (
          <div className="col-span-full text-center py-8 text-gray-500">No vaults found</div>
        ) : (
          vaults.map((vault) => {
            const status = getVaultStatus(vault);
            return (
              <div key={vault.id} className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <h3 className="font-semibold text-gray-900">{vault.branchName}</h3>
                    <p className="text-sm text-gray-500">{vault.branchCode} • {vault.currency}</p>
                  </div>
                  <span className={`text-xs px-2 py-1 rounded ${status.color}`}>
                    {status.label}
                  </span>
                </div>

                <div className="space-y-2 mb-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Cash on Hand</span>
                    <span className="font-semibold text-lg text-gray-900">
                      {formatCurrency(vault.cashOnHand, vault.currency)}
                    </span>
                  </div>

                  {vault.vaultLimit && (
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-gray-600">Vault Limit</span>
                      <span className="text-gray-900">{formatCurrency(vault.vaultLimit, vault.currency)}</span>
                    </div>
                  )}

                  {vault.minBalance && (
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-gray-600">Minimum Balance</span>
                      <span className="text-gray-900">{formatCurrency(vault.minBalance, vault.currency)}</span>
                    </div>
                  )}
                </div>

                <div className="pt-3 border-t border-gray-200 text-xs text-gray-500 space-y-1">
                  <div className="flex items-center gap-2">
                    <Clock className="w-3 h-3" />
                    <span>Last Count: {formatDateTime(vault.lastCountDate)}</span>
                  </div>
                  {vault.lastCountByName && (
                    <div>Counted by: {vault.lastCountByName}</div>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
