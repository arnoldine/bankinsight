import React, { useState, useMemo } from 'react';
import { Shield, UserCheck, AlertTriangle, FileText, Search, CheckCircle, XCircle, Eye } from 'lucide-react';
import { Customer } from '../types';
import PermissionGuard from './PermissionGuard';

interface ComplianceOfficerScreenProps {
  clients: Customer[];
  onVerifyKYC?: (clientCIF: string, status: string, notes: string) => void;
  onFlagTransaction?: (transactionId: string, reason: string) => void;
  onUpdateRiskScore?: (clientCIF: string, score: number) => void;
}

const ComplianceOfficerScreen: React.FC<ComplianceOfficerScreenProps> = ({
  clients,
  onVerifyKYC,
  onFlagTransaction,
  onUpdateRiskScore
}) => {
  const [activeTab, setActiveTab] = useState<'kyc' | 'aml' | 'sanctions' | 'reports'>('kyc');
  const [selectedClient, setSelectedClient] = useState<Customer | null>(null);
  const [kycStatus, setKycStatus] = useState('');
  const [kycNotes, setKycNotes] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  const metrics = useMemo(() => {
    const pendingKYC = clients.filter(c => c.status === 'PENDING').length;
    const flaggedTransactions = 3; // Mock
    const highRiskClients = clients.filter(c => c.riskRating === 'High').length;
    const complianceScore = 94; // Mock

    return {
      pendingKYC,
      flaggedTransactions,
      highRiskClients,
      complianceScore
    };
  }, [clients]);

  const pendingKYCClients = clients.filter(c => 
    c.status === 'PENDING' || !c.kycVerified
  );

  const highRiskClients = clients.filter(c => 
    c.riskRating === 'High' || c.riskRating === 'Medium'
  );

  const handleVerifyKYC = (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedClient && onVerifyKYC) {
      onVerifyKYC(selectedClient.cif, kycStatus, kycNotes);
      alert(`KYC verification ${kycStatus} for ${selectedClient.firstName} ${selectedClient.lastName}`);
      setSelectedClient(null);
      setKycStatus('');
      setKycNotes('');
    }
  };

  const filteredClients = searchTerm
    ? clients.filter(c => 
        c.cif.toLowerCase().includes(searchTerm.toLowerCase()) ||
        c.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        c.lastName.toLowerCase().includes(searchTerm.toLowerCase())
      )
    : clients;

  return (
    <PermissionGuard permission={['CLIENT_READ', 'AUDIT_READ']} fallback={<div className="p-6 text-red-600">Access denied - Compliance permissions required</div>}>
      <div className="h-full flex flex-col bg-slate-50">
        {/* Header */}
        <div className="bg-white border-b border-slate-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Compliance Officer</h1>
              <p className="text-sm text-slate-600 mt-1">KYC, AML & Regulatory Compliance Monitoring</p>
            </div>
            <div className="flex items-center gap-4">
              <div className="text-right">
                <p className="text-xs text-slate-600">Compliance Score</p>
                <div className="flex items-center gap-2 mt-1">
                  <div className="w-24 h-2 bg-slate-200 rounded-full overflow-hidden">
                    <div 
                      className="h-full bg-green-600 rounded-full" 
                      style={{ width: `${metrics.complianceScore}%` }}
                    />
                  </div>
                  <span className="font-semibold text-green-600">{metrics.complianceScore}%</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Metrics Cards */}
        <div className="px-6 py-4">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Pending KYC</p>
                  <p className="text-2xl font-bold text-amber-600 mt-1">{metrics.pendingKYC}</p>
                </div>
                <div className="w-12 h-12 bg-amber-100 rounded-lg flex items-center justify-center">
                  <UserCheck className="w-6 h-6 text-amber-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Flagged Transactions</p>
                  <p className="text-2xl font-bold text-red-600 mt-1">{metrics.flaggedTransactions}</p>
                </div>
                <div className="w-12 h-12 bg-red-100 rounded-lg flex items-center justify-center">
                  <AlertTriangle className="w-6 h-6 text-red-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">High Risk Clients</p>
                  <p className="text-2xl font-bold text-orange-600 mt-1">{metrics.highRiskClients}</p>
                </div>
                <div className="w-12 h-12 bg-orange-100 rounded-lg flex items-center justify-center">
                  <Shield className="w-6 h-6 text-orange-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Compliance Score</p>
                  <p className="text-2xl font-bold text-green-600 mt-1">{metrics.complianceScore}%</p>
                </div>
                <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                  <CheckCircle className="w-6 h-6 text-green-600" />
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="px-6">
          <div className="flex gap-2 border-b border-slate-200">
            {[
              { id: 'kyc', label: 'KYC Verification', icon: UserCheck },
              { id: 'aml', label: 'AML Monitoring', icon: AlertTriangle },
              { id: 'sanctions', label: 'Sanctions Screening', icon: Shield },
              { id: 'reports', label: 'Compliance Reports', icon: FileText }
            ].map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as any)}
                className={`flex items-center gap-2 px-4 py-3 font-medium transition-colors ${
                  activeTab === tab.id
                    ? 'text-blue-600 border-b-2 border-blue-600'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                <tab.icon className="w-4 h-4" />
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        {/* Content Area */}
        <div className="flex-1 overflow-auto px-6 py-4">
          {activeTab === 'kyc' && (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              {/* Pending KYC List */}
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <h3 className="font-semibold text-slate-900 mb-4">Pending KYC Verifications ({pendingKYCClients.length})</h3>
                <div className="space-y-2 max-h-96 overflow-auto">
                  {pendingKYCClients.map(client => (
                    <div 
                      key={client.cif}
                      onClick={() => setSelectedClient(client)}
                      className={`p-4 rounded-lg border cursor-pointer transition-colors ${
                        selectedClient?.cif === client.cif
                          ? 'bg-blue-50 border-blue-300'
                          : 'bg-slate-50 border-slate-200 hover:border-slate-300'
                      }`}
                    >
                      <div className="flex items-start justify-between">
                        <div>
                          <p className="font-medium text-slate-900">{client.firstName} {client.lastName}</p>
                          <p className="text-sm text-slate-600">CIF: {client.cif}</p>
                          <p className="text-xs text-slate-500 mt-1">Registered: {new Date(client.registrationDate).toLocaleDateString()}</p>
                        </div>
                        <div className="px-2 py-1 bg-amber-100 text-amber-700 rounded text-xs font-semibold">
                          PENDING
                        </div>
                      </div>
                    </div>
                  ))}
                  {pendingKYCClients.length === 0 && (
                    <p className="text-center text-slate-600 py-4">No pending KYC verifications</p>
                  )}
                </div>
              </div>

              {/* KYC Verification Form */}
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <h3 className="font-semibold text-slate-900 mb-4">KYC Verification</h3>
                
                {!selectedClient && (
                  <div className="flex flex-col items-center justify-center py-12 text-slate-400">
                    <UserCheck className="w-16 h-16 mb-3" />
                    <p>Select a client to verify KYC</p>
                  </div>
                )}

                {selectedClient && (
                  <form onSubmit={handleVerifyKYC} className="space-y-4">
                    <div className="p-4 bg-slate-50 rounded-lg border border-slate-200">
                      <p className="text-sm text-slate-600">Customer</p>
                      <p className="font-semibold text-slate-900">{selectedClient.firstName} {selectedClient.lastName}</p>
                      <p className="text-sm text-slate-600">CIF: {selectedClient.cif}</p>
                      <p className="text-sm text-slate-600">Email: {selectedClient.email || 'N/A'}</p>
                      <p className="text-sm text-slate-600">Phone: {selectedClient.phoneNumber || 'N/A'}</p>
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">Verification Documents</label>
                      <div className="space-y-2">
                        <div className="flex items-center gap-3 p-3 bg-slate-50 rounded-lg border border-slate-200">
                          <FileText className="w-5 h-5 text-slate-400" />
                          <div className="flex-1">
                            <p className="text-sm font-medium text-slate-900">National ID Card</p>
                            <p className="text-xs text-slate-600">GHA-123456789-0</p>
                          </div>
                          <Eye className="w-5 h-5 text-blue-600 cursor-pointer" />
                        </div>
                        <div className="flex items-center gap-3 p-3 bg-slate-50 rounded-lg border border-slate-200">
                          <FileText className="w-5 h-5 text-slate-400" />
                          <div className="flex-1">
                            <p className="text-sm font-medium text-slate-900">Proof of Address</p>
                            <p className="text-xs text-slate-600">Utility Bill</p>
                          </div>
                          <Eye className="w-5 h-5 text-blue-600 cursor-pointer" />
                        </div>
                      </div>
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">Verification Status</label>
                      <select
                        value={kycStatus}
                        onChange={(e) => setKycStatus(e.target.value)}
                        className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        required
                      >
                        <option value="">Select status...</option>
                        <option value="APPROVED">Approve KYC</option>
                        <option value="REJECTED">Reject KYC</option>
                        <option value="PENDING_INFO">Request More Information</option>
                      </select>
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">Verification Notes</label>
                      <textarea
                        value={kycNotes}
                        onChange={(e) => setKycNotes(e.target.value)}
                        rows={4}
                        className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        placeholder="Add notes about document verification, identity checks, etc."
                        required
                      />
                    </div>

                    <button
                      type="submit"
                      className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-3 rounded-lg transition-colors flex items-center justify-center gap-2"
                    >
                      <CheckCircle className="w-5 h-5" />
                      Submit Verification
                    </button>
                  </form>
                )}
              </div>
            </div>
          )}

          {activeTab === 'aml' && (
            <div className="space-y-4">
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <h3 className="font-semibold text-slate-900 mb-4">Flagged Transactions</h3>
                
                <div className="space-y-3">
                  <div className="p-4 bg-red-50 rounded-lg border border-red-200">
                    <div className="flex items-start justify-between">
                      <div className="flex items-start gap-3">
                        <div className="w-10 h-10 bg-red-200 rounded-lg flex items-center justify-center">
                          <AlertTriangle className="w-5 h-5 text-red-700" />
                        </div>
                        <div>
                          <h4 className="font-semibold text-slate-900">Large Cash Deposit</h4>
                          <p className="text-sm text-slate-600 mt-1">GHS 150,000.00 cash deposit</p>
                          <p className="text-xs text-slate-500 mt-1">ACC: 1001234567 - CIF: CL000123</p>
                          <p className="text-xs text-slate-500">Flagged: Exceeds reporting threshold</p>
                        </div>
                      </div>
                      <div className="px-3 py-1 bg-red-200 text-red-800 rounded-full text-xs font-semibold">
                        HIGH RISK
                      </div>
                    </div>
                    <div className="mt-3 flex gap-2">
                      <button className="px-4 py-2 bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 rounded-lg text-sm font-medium">
                        Review
                      </button>
                      <button className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg text-sm font-medium">
                        File SAR
                      </button>
                    </div>
                  </div>

                  <div className="p-4 bg-amber-50 rounded-lg border border-amber-200">
                    <div className="flex items-start justify-between">
                      <div className="flex items-start gap-3">
                        <div className="w-10 h-10 bg-amber-200 rounded-lg flex items-center justify-center">
                          <AlertTriangle className="w-5 h-5 text-amber-700" />
                        </div>
                        <div>
                          <h4 className="font-semibold text-slate-900">Unusual Transaction Pattern</h4>
                          <p className="text-sm text-slate-600 mt-1">Multiple small deposits (structuring suspected)</p>
                          <p className="text-xs text-slate-500 mt-1">ACC: 1001234568 - CIF: CL000456</p>
                          <p className="text-xs text-slate-500">Flagged: 15 deposits below GHS 10,000 in 24 hours</p>
                        </div>
                      </div>
                      <div className="px-3 py-1 bg-amber-200 text-amber-800 rounded-full text-xs font-semibold">
                        MEDIUM RISK
                      </div>
                    </div>
                    <div className="mt-3 flex gap-2">
                      <button className="px-4 py-2 bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 rounded-lg text-sm font-medium">
                        Review
                      </button>
                      <button className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg text-sm font-medium">
                        Clear Flag
                      </button>
                    </div>
                  </div>

                  <div className="p-4 bg-orange-50 rounded-lg border border-orange-200">
                    <div className="flex items-start justify-between">
                      <div className="flex items-start gap-3">
                        <div className="w-10 h-10 bg-orange-200 rounded-lg flex items-center justify-center">
                          <Shield className="w-5 h-5 text-orange-700" />
                        </div>
                        <div>
                          <h4 className="font-semibold text-slate-900">High-Risk Country Transfer</h4>
                          <p className="text-sm text-slate-600 mt-1">International transfer to high-risk jurisdiction</p>
                          <p className="text-xs text-slate-500 mt-1">ACC: 1001234569 - CIF: CL000789</p>
                          <p className="text-xs text-slate-500">Flagged: Recipient country on watchlist</p>
                        </div>
                      </div>
                      <div className="px-3 py-1 bg-orange-200 text-orange-800 rounded-full text-xs font-semibold">
                        REVIEW REQUIRED
                      </div>
                    </div>
                    <div className="mt-3 flex gap-2">
                      <button className="px-4 py-2 bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 rounded-lg text-sm font-medium">
                        Review
                      </button>
                      <button className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg text-sm font-medium">
                        Block Transfer
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'sanctions' && (
            <div className="max-w-4xl mx-auto">
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <h3 className="text-lg font-semibold text-slate-900 mb-4">Sanctions Screening</h3>
                
                <div className="mb-6">
                  <div className="flex gap-3">
                    <input
                      type="text"
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      placeholder="Search client by CIF or name..."
                      className="flex-1 px-4 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    />
                    <button className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors flex items-center gap-2">
                      <Search className="w-4 h-4" />
                      Screen
                    </button>
                  </div>
                </div>

                <div className="space-y-3">
                  {filteredClients.slice(0, 5).map(client => (
                    <div key={client.cif} className="p-4 bg-slate-50 rounded-lg border border-slate-200">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="font-medium text-slate-900">{client.firstName} {client.lastName}</p>
                          <p className="text-sm text-slate-600">CIF: {client.cif}</p>
                        </div>
                        <div className="flex items-center gap-2">
                          <CheckCircle className="w-5 h-5 text-green-600" />
                          <span className="text-sm font-semibold text-green-600">Clear</span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {activeTab === 'reports' && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <div className="bg-white rounded-lg border border-slate-200 p-6 hover:border-blue-300 transition-colors cursor-pointer">
                <FileText className="w-10 h-10 text-blue-600 mb-3" />
                <h3 className="font-semibold text-slate-900 mb-2">Suspicious Activity Reports (SAR)</h3>
                <p className="text-sm text-slate-600">View and file SARs</p>
              </div>
              <div className="bg-white rounded-lg border border-slate-200 p-6 hover:border-blue-300 transition-colors cursor-pointer">
                <Shield className="w-10 h-10 text-green-600 mb-3" />
                <h3 className="font-semibold text-slate-900 mb-2">Currency Transaction Reports (CTR)</h3>
                <p className="text-sm text-slate-600">Large transaction reporting</p>
              </div>
              <div className="bg-white rounded-lg border border-slate-200 p-6 hover:border-blue-300 transition-colors cursor-pointer">
                <UserCheck className="w-10 h-10 text-purple-600 mb-3" />
                <h3 className="font-semibold text-slate-900 mb-2">KYC Compliance Summary</h3>
                <p className="text-sm text-slate-600">Overall KYC status report</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </PermissionGuard>
  );
};

export default ComplianceOfficerScreen;

