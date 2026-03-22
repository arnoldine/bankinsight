import React, { useState, useEffect } from 'react';
import { ArrowRightLeft, Building, Clock, Check, X, AlertCircle } from 'lucide-react';

interface InterBranchTransfer {
  id: string;
  fromBranchId: string;
  fromBranchCode: string;
  fromBranchName: string;
  toBranchId: string;
  toBranchCode: string;
  toBranchName: string;
  currency: string;
  amount: number;
  reference?: string;
  narration?: string;
  status: string;
  initiatedBy: string;
  initiatedByName: string;
  approvedBy?: string;
  approvedByName?: string;
  createdAt: string;
  approvedAt?: string;
  completedAt?: string;
  rejectionReason?: string;
}

export function InterBranchTransfers() {
  const [transfers, setTransfers] = useState<InterBranchTransfer[]>([]);
  const [loading, setLoading] = useState(true);
  const [viewMode, setViewMode] = useState<'all' | 'pending'>('all');
  const [showInitiateForm, setShowInitiateForm] = useState(false);
  const [branches, setBranches] = useState<any[]>([]);
  const [formData, setFormData] = useState({
    fromBranchId: '',
    toBranchId: '',
    currency: 'GHS',
    amount: '',
    reference: '',
    narration: ''
  });

  useEffect(() => {
    loadTransfers();
    loadBranches();
  }, [viewMode]);

  const loadTransfers = async () => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const endpoint = viewMode === 'pending' ? 'pending' : '';
      const res = await fetch(`http://localhost:5176/api/interbranchtransfer${endpoint ? '/' + endpoint : ''}`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        setTransfers(await res.json());
      }
    } catch (error) {
      console.error('Failed to load transfers:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadBranches = async () => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/branches', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        setBranches(await res.json());
      }
    } catch (error) {
      console.error('Failed to load branches:', error);
    }
  };

  const handleInitiate = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/interbranchtransfer', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          ...formData,
          amount: parseFloat(formData.amount)
        })
      });

      if (res.ok) {
        setShowInitiateForm(false);
        setFormData({
          fromBranchId: '',
          toBranchId: '',
          currency: 'GHS',
          amount: '',
          reference: '',
          narration: ''
        });
        loadTransfers();
      } else {
        const error = await res.json();
        alert(error.message || 'Failed to initiate transfer');
      }
    } catch (error) {
      console.error('Failed to initiate transfer:', error);
      alert('Failed to initiate transfer');
    }
  };

  const handleApprove = async (transferId: string, approved: boolean, rejectionReason?: string) => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/interbranchtransfer/approve', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          transferId,
          approved,
          rejectionReason
        })
      });

      if (res.ok) {
        loadTransfers();
      } else {
        const error = await res.json();
        alert(error.message || 'Action failed');
      }
    } catch (error) {
      console.error('Failed to approve/reject transfer:', error);
      alert('Action failed');
    }
  };

  const formatCurrency = (amount: number, currency: string = 'GHS') => {
    return `${currency} ${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  };

  const formatDateTime = (date?: string) => {
    if (!date) return '-';
    return new Date(date).toLocaleString();
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Pending': return 'text-yellow-600 bg-yellow-50';
      case 'Approved': return 'text-green-600 bg-green-50';
      case 'Rejected': return 'text-red-600 bg-red-50';
      default: return 'text-gray-600 bg-gray-50';
    }
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <ArrowRightLeft className="w-6 h-6 text-indigo-600" />
          <h2 className="text-xl font-semibold text-gray-900">Inter-Branch Transfers</h2>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setViewMode('all')}
            className={`px-4 py-2 rounded-lg transition-colors ${
              viewMode === 'all'
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            All
          </button>
          <button
            onClick={() => setViewMode('pending')}
            className={`px-4 py-2 rounded-lg transition-colors ${
              viewMode === 'pending'
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            Pending
          </button>
          <button
            onClick={() => setShowInitiateForm(true)}
            className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
          >
            Initiate Transfer
          </button>
        </div>
      </div>

      {showInitiateForm && (
        <form onSubmit={handleInitiate} className="mb-6 p-4 bg-gray-50 rounded-lg space-y-4">
          <h3 className="font-semibold text-gray-900">New Inter-Branch Transfer</h3>
          
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">From Branch</label>
              <select
                value={formData.fromBranchId}
                onChange={(e) => setFormData({ ...formData, fromBranchId: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              >
                <option value="">Select branch...</option>
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>
                    {branch.name} ({branch.code})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">To Branch</label>
              <select
                value={formData.toBranchId}
                onChange={(e) => setFormData({ ...formData, toBranchId: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              >
                <option value="">Select branch...</option>
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>
                    {branch.name} ({branch.code})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Currency</label>
              <select
                value={formData.currency}
                onChange={(e) => setFormData({ ...formData, currency: e.target.value })}
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
                value={formData.amount}
                onChange={(e) => setFormData({ ...formData, amount: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Reference</label>
              <input
                type="text"
                value={formData.reference}
                onChange={(e) => setFormData({ ...formData, reference: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Narration</label>
              <input
                type="text"
                value={formData.narration}
                onChange={(e) => setFormData({ ...formData, narration: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              />
            </div>
          </div>

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => setShowInitiateForm(false)}
              className="px-4 py-2 text-gray-600 hover:bg-gray-200 rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
            >
              Initiate
            </button>
          </div>
        </form>
      )}

      <div className="space-y-4">
        {loading ? (
          <div className="text-center py-8 text-gray-500">Loading transfers...</div>
        ) : transfers.length === 0 ? (
          <div className="text-center py-8 text-gray-500">No transfers found</div>
        ) : (
          transfers.map((transfer) => (
            <div
              key={transfer.id}
              className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors"
            >
              <div className="flex items-start justify-between mb-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <span className={`text-xs px-2 py-1 rounded ${getStatusColor(transfer.status)}`}>
                      {transfer.status}
                    </span>
                    <span className="text-sm text-gray-500">{transfer.id}</span>
                  </div>

                  <div className="flex items-center gap-4 mb-2">
                    <div className="flex items-center gap-2">
                      <Building className="w-4 h-4 text-gray-400" />
                      <span className="font-medium text-gray-900">{transfer.fromBranchName}</span>
                      <span className="text-sm text-gray-500">({transfer.fromBranchCode})</span>
                    </div>
                    <ArrowRightLeft className="w-4 h-4 text-gray-400" />
                    <div className="flex items-center gap-2">
                      <Building className="w-4 h-4 text-gray-400" />
                      <span className="font-medium text-gray-900">{transfer.toBranchName}</span>
                      <span className="text-sm text-gray-500">({transfer.toBranchCode})</span>
                    </div>
                  </div>

                  <div className="text-2xl font-bold text-gray-900 mb-2">
                    {formatCurrency(transfer.amount, transfer.currency)}
                  </div>

                  {transfer.narration && (
                    <div className="text-sm text-gray-600 mb-2">{transfer.narration}</div>
                  )}

                  <div className="grid grid-cols-2 gap-4 text-sm text-gray-600">
                    <div>
                      <div>Initiated by: {transfer.initiatedByName}</div>
                      <div className="text-xs text-gray-500">{formatDateTime(transfer.createdAt)}</div>
                    </div>
                    {transfer.approvedByName && (
                      <div>
                        <div>Approved by: {transfer.approvedByName}</div>
                        <div className="text-xs text-gray-500">{formatDateTime(transfer.approvedAt)}</div>
                      </div>
                    )}
                  </div>

                  {transfer.rejectionReason && (
                    <div className="mt-2 p-2 bg-red-50 rounded text-sm text-red-600 flex items-start gap-2">
                      <AlertCircle className="w-4 h-4 flex-shrink-0 mt-0.5" />
                      <span>{transfer.rejectionReason}</span>
                    </div>
                  )}
                </div>

                {transfer.status === 'Pending' && (
                  <div className="flex gap-2 ml-4">
                    <button
                      onClick={() => handleApprove(transfer.id, true)}
                      className="p-2 text-green-600 hover:bg-green-50 rounded-lg transition-colors"
                      title="Approve"
                    >
                      <Check className="w-5 h-5" />
                    </button>
                    <button
                      onClick={() => {
                        const reason = prompt('Rejection reason:');
                        if (reason) handleApprove(transfer.id, false, reason);
                      }}
                      className="p-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                      title="Reject"
                    >
                      <X className="w-5 h-5" />
                    </button>
                  </div>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
