import React, { useState, useMemo } from 'react';
import { Users, FileText, AlertTriangle, CheckCircle, Phone, Mail, MapPin, Shield, Search, LifeBuoy } from 'lucide-react';
import { Customer, Account, Transaction } from '../types';
import PermissionGuard from './PermissionGuard';

interface CustomerServiceScreenProps {
  clients: Customer[];
  accounts: Account[];
  transactions: Transaction[];
  onResolveIssue?: (issueId: string, resolution: string) => void;
  onCreateTicket?: (ticket: any) => void;
}

const CustomerServiceScreen: React.FC<CustomerServiceScreenProps> = ({
  clients,
  accounts,
  transactions,
  onResolveIssue,
  onCreateTicket
}) => {
  const [activeTab, setActiveTab] = useState<'lookup' | 'tickets' | 'support' | 'history'>('lookup');
  const [searchCIF, setSearchCIF] = useState('');
  const [selectedClient, setSelectedClient] = useState<Customer | null>(null);
  const [ticketSubject, setTicketSubject] = useState('');
  const [ticketPriority, setTicketPriority] = useState('MEDIUM');
  const [ticketDescription, setTicketDescription] = useState('');

  const metrics = useMemo(() => {
    const openTickets = 12; // Mock
    const resolvedToday = 8; // Mock
    const avgResponseTime = 45; // minutes
    const customerSatisfaction = 4.2; // out of 5

    return {
      openTickets,
      resolvedToday,
      avgResponseTime,
      customerSatisfaction
    };
  }, []);

  const handleSearch = () => {
    const client = clients.find(c => c.cif === searchCIF);
    setSelectedClient(client || null);
  };

  const handleCreateTicket = (e: React.FormEvent) => {
    e.preventDefault();
    if (onCreateTicket && selectedClient) {
      onCreateTicket({
        clientCIF: selectedClient.cif,
        subject: ticketSubject,
        priority: ticketPriority,
        description: ticketDescription,
        createdAt: new Date().toISOString()
      });
      
      // Reset form
      setTicketSubject('');
      setTicketPriority('MEDIUM');
      setTicketDescription('');
      alert('Support ticket created successfully!');
    }
  };

  const clientAccounts = selectedClient 
    ? accounts.filter(a => a.cif === selectedClient.cif)
    : [];

  const clientTransactions = selectedClient
    ? transactions.filter(t => clientAccounts.some(a => a.id === t.accountId)).slice(0, 10)
    : [];

  return (
    <PermissionGuard permission={['ACCOUNT_READ', 'CLIENT_READ']} fallback={<div className="p-6 text-red-600">Access denied - Customer service permissions required</div>}>
      <div className="h-full flex flex-col rounded-[28px] border border-slate-200 bg-[linear-gradient(180deg,#f8fafc,#eef2f7)] overflow-hidden">
        {/* Header */}
        <div className="bg-white border-b border-slate-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Customer Service</h1>
              <p className="text-sm text-slate-600 mt-1">Account Support & Issue Resolution</p>
            </div>
            <div className="flex items-center gap-4">
              <div className="text-right">
                <p className="text-xs text-slate-600">Open Tickets</p>
                <p className="text-2xl font-bold text-slate-900">{metrics.openTickets}</p>
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
                  <p className="text-xs text-slate-600 uppercase font-semibold">Open Tickets</p>
                  <p className="text-2xl font-bold text-amber-600 mt-1">{metrics.openTickets}</p>
                </div>
                <div className="w-12 h-12 bg-amber-100 rounded-lg flex items-center justify-center">
                  <AlertTriangle className="w-6 h-6 text-amber-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Resolved Today</p>
                  <p className="text-2xl font-bold text-green-600 mt-1">{metrics.resolvedToday}</p>
                </div>
                <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                  <CheckCircle className="w-6 h-6 text-green-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Avg Response Time</p>
                  <p className="text-2xl font-bold text-blue-600 mt-1">{metrics.avgResponseTime}m</p>
                </div>
                <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                  <Phone className="w-6 h-6 text-blue-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Satisfaction Score</p>
                  <p className="text-2xl font-bold text-purple-600 mt-1">{metrics.customerSatisfaction}/5</p>
                </div>
                <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
                  <Users className="w-6 h-6 text-purple-600" />
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="px-6">
          <div className="flex gap-2 border-b border-slate-200">
            {[
              { id: 'lookup', label: 'Customer Lookup', icon: Search },
              { id: 'tickets', label: 'Support Tickets', icon: FileText },
              { id: 'support', label: 'Create Ticket', icon: AlertTriangle },
              { id: 'history', label: 'Service History', icon: CheckCircle }
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
          {activeTab === 'lookup' && (
            <div className="max-w-6xl mx-auto">
              <div className="bg-white rounded-lg border border-slate-200 p-6 mb-4">
                <div className="flex items-center gap-3 mb-4">
                  <LifeBuoy className="w-5 h-5 text-blue-600" />
                  <div>
                    <h3 className="text-lg font-semibold text-slate-900">Search Customer</h3>
                    <p className="text-sm text-slate-500">Find a customer quickly, review accounts, and reference recent transaction activity.</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <input
                    type="text"
                    value={searchCIF}
                    onChange={(e) => setSearchCIF(e.target.value)}
                    onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                    placeholder="Enter Customer CIF..."
                    className="flex-1 px-4 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  />
                  <button
                    onClick={handleSearch}
                    className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors flex items-center gap-2"
                  >
                    <Search className="w-4 h-4" />
                    Search
                  </button>
                </div>
              </div>

              {selectedClient && (
                <div className="space-y-4">
                  {/* Customer Details */}
                  <div className="bg-white rounded-lg border border-slate-200 p-6">
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <h3 className="text-xl font-bold text-slate-900">{selectedClient.firstName} {selectedClient.lastName}</h3>
                        <p className="text-sm text-slate-600">CIF: {selectedClient.cif}</p>
                      </div>
                      <div className="flex items-center gap-2 px-3 py-1 bg-green-100 text-green-700 rounded-full text-sm font-semibold">
                        <CheckCircle className="w-4 h-4" />
                        {selectedClient.status}
                      </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div className="flex items-start gap-3">
                        <Mail className="w-5 h-5 text-slate-400 mt-0.5" />
                        <div>
                          <p className="text-xs text-slate-600">Email</p>
                          <p className="font-medium text-slate-900">{selectedClient.email || 'N/A'}</p>
                        </div>
                      </div>
                      <div className="flex items-start gap-3">
                        <Phone className="w-5 h-5 text-slate-400 mt-0.5" />
                        <div>
                          <p className="text-xs text-slate-600">Phone</p>
                          <p className="font-medium text-slate-900">{selectedClient.phoneNumber || 'N/A'}</p>
                        </div>
                      </div>
                      <div className="flex items-start gap-3">
                        <MapPin className="w-5 h-5 text-slate-400 mt-0.5" />
                        <div>
                          <p className="text-xs text-slate-600">City</p>
                          <p className="font-medium text-slate-900">{selectedClient.city || 'N/A'}</p>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Accounts */}
                  <div className="bg-white rounded-lg border border-slate-200 p-6">
                    <h4 className="font-semibold text-slate-900 mb-4">Customer Accounts ({clientAccounts.length})</h4>
                    <div className="space-y-2">
                      {clientAccounts.map(account => (
                        <div key={account.id} className="p-4 bg-slate-50 rounded-lg border border-slate-200">
                          <div className="flex items-center justify-between">
                            <div>
                              <p className="font-medium text-slate-900">{account.id}</p>
                              <p className="text-sm text-slate-600">{account.productCode}</p>
                            </div>
                            <div className="text-right">
                              <p className="text-lg font-bold text-slate-900">GHS {account.balance.toFixed(2)}</p>
                              <p className="text-xs text-slate-600">Available: GHS {(account.balance - account.lienAmount).toFixed(2)}</p>
                            </div>
                          </div>
                        </div>
                      ))}
                      {clientAccounts.length === 0 && (
                        <p className="text-center text-slate-600 py-4">No accounts found</p>
                      )}
                    </div>
                  </div>

                  {/* Recent Transactions */}
                  <div className="bg-white rounded-lg border border-slate-200 p-6">
                    <h4 className="font-semibold text-slate-900 mb-4">Recent Transactions</h4>
                    <div className="space-y-2">
                      {clientTransactions.map((txn, idx) => (
                        <div key={idx} className="p-3 bg-slate-50 rounded-lg border border-slate-200">
                          <div className="flex items-center justify-between">
                            <div>
                              <p className="font-medium text-slate-900">{txn.type}</p>
                              <p className="text-sm text-slate-600">{new Date(txn.date).toLocaleDateString()}</p>
                            </div>
                            <div className="text-right">
                              <p className={`font-bold ${txn.amount < 0 ? 'text-red-600' : 'text-green-600'}`}>
                                GHS {Math.abs(txn.amount).toFixed(2)}
                              </p>
                              <p className="text-xs text-slate-600">{txn.accountId}</p>
                            </div>
                          </div>
                        </div>
                      ))}
                      {clientTransactions.length === 0 && (
                        <p className="text-center text-slate-600 py-4">No recent transactions</p>
                      )}
                    </div>
                  </div>
                </div>
              )}

              {!selectedClient && searchCIF && (
                <div className="bg-amber-50 border border-amber-200 rounded-lg p-6 text-center">
                  <AlertTriangle className="w-12 h-12 text-amber-600 mx-auto mb-3" />
                  <p className="text-slate-700">No customer found with CIF: {searchCIF}</p>
                </div>
              )}
            </div>
          )}

          {activeTab === 'tickets' && (
            <div className="space-y-4">
              <div className="bg-white rounded-lg border border-slate-200 p-4 hover:border-blue-300 transition-colors">
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-3">
                    <div className="w-10 h-10 bg-red-100 rounded-lg flex items-center justify-center">
                      <AlertTriangle className="w-5 h-5 text-red-600" />
                    </div>
                    <div>
                      <h4 className="font-semibold text-slate-900">Account Access Issue</h4>
                      <p className="text-sm text-slate-600 mt-1">CIF: CL000123 - Customer unable to login</p>
                      <p className="text-xs text-slate-500 mt-1">Created 2 hours ago</p>
                    </div>
                  </div>
                  <div className="px-3 py-1 bg-red-100 text-red-700 rounded-full text-xs font-semibold">
                    HIGH
                  </div>
                </div>
              </div>

              <div className="bg-white rounded-lg border border-slate-200 p-4 hover:border-blue-300 transition-colors">
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-3">
                    <div className="w-10 h-10 bg-amber-100 rounded-lg flex items-center justify-center">
                      <FileText className="w-5 h-5 text-amber-600" />
                    </div>
                    <div>
                      <h4 className="font-semibold text-slate-900">Statement Request</h4>
                      <p className="text-sm text-slate-600 mt-1">CIF: CL000456 - Requesting 6-month statement</p>
                      <p className="text-xs text-slate-500 mt-1">Created 5 hours ago</p>
                    </div>
                  </div>
                  <div className="px-3 py-1 bg-amber-100 text-amber-700 rounded-full text-xs font-semibold">
                    MEDIUM
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'support' && (
            <div className="max-w-2xl mx-auto">
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <h3 className="text-lg font-semibold text-slate-900 mb-4">Create Support Ticket</h3>
                
                {!selectedClient && (
                  <div className="p-4 bg-amber-50 border border-amber-200 rounded-lg mb-4">
                    <p className="text-sm text-amber-800">
                      <AlertTriangle className="w-4 h-4 inline mr-2" />
                      Please search and select a customer from the Lookup tab first
                    </p>
                  </div>
                )}

                {selectedClient && (
                  <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg mb-4">
                    <p className="text-sm text-blue-800">
                      Creating ticket for: <strong>{selectedClient.firstName} {selectedClient.lastName}</strong> (CIF: {selectedClient.cif})
                    </p>
                  </div>
                )}

                <form onSubmit={handleCreateTicket} className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Subject</label>
                    <input
                      type="text"
                      value={ticketSubject}
                      onChange={(e) => setTicketSubject(e.target.value)}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      placeholder="Brief description of the issue"
                      required
                      disabled={!selectedClient}
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Priority</label>
                    <select
                      value={ticketPriority}
                      onChange={(e) => setTicketPriority(e.target.value)}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      disabled={!selectedClient}
                    >
                      <option value="LOW">Low</option>
                      <option value="MEDIUM">Medium</option>
                      <option value="HIGH">High</option>
                      <option value="URGENT">Urgent</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
                    <textarea
                      value={ticketDescription}
                      onChange={(e) => setTicketDescription(e.target.value)}
                      rows={6}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      placeholder="Detailed description of the issue and any troubleshooting steps taken"
                      required
                      disabled={!selectedClient}
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={!selectedClient}
                    className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-slate-300 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-lg transition-colors flex items-center justify-center gap-2"
                  >
                    <FileText className="w-5 h-5" />
                    Create Ticket
                  </button>
                </form>
              </div>
            </div>
          )}

          {activeTab === 'history' && (
            <div className="space-y-4">
              <div className="bg-white rounded-lg border border-slate-200 p-4">
                <div className="flex items-start gap-3">
                  <CheckCircle className="w-5 h-5 text-green-600 mt-0.5" />
                  <div className="flex-1">
                    <h4 className="font-semibold text-slate-900">Password Reset</h4>
                    <p className="text-sm text-slate-600 mt-1">CIF: CL000789 - Successfully reset password</p>
                    <p className="text-xs text-slate-500 mt-1">Resolved yesterday at 14:32</p>
                  </div>
                </div>
              </div>

              <div className="bg-white rounded-lg border border-slate-200 p-4">
                <div className="flex items-start gap-3">
                  <CheckCircle className="w-5 h-5 text-green-600 mt-0.5" />
                  <div className="flex-1">
                    <h4 className="font-semibold text-slate-900">Transaction Inquiry</h4>
                    <p className="text-sm text-slate-600 mt-1">CIF: CL000234 - Clarified debit transaction</p>
                    <p className="text-xs text-slate-500 mt-1">Resolved yesterday at 11:15</p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </PermissionGuard>
  );
};

export default CustomerServiceScreen;


