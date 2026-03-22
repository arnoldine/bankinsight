import React, { useState } from 'react';
import { AuditLog } from '../types';
import { Search, Filter, Shield, Clock, AlertCircle, CheckCircle, Lock } from 'lucide-react';

interface AuditTrailProps {
  logs: AuditLog[];
}

const normalizeStatus = (status: AuditLog['status'] | string) => status === 'FAILED' ? 'FAILURE' : status;

const AuditTrail: React.FC<AuditTrailProps> = ({ logs }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filterModule, setFilterModule] = useState<string>('ALL');

  const filteredLogs = logs.filter(log => {
    const search = searchTerm.toLowerCase();
    const matchesSearch =
      log.user.toLowerCase().includes(search) ||
      log.action.toLowerCase().includes(search) ||
      log.id.toLowerCase().includes(search) ||
      (log.entityId || '').toLowerCase().includes(search) ||
      (log.entityType || '').toLowerCase().includes(search);
    const matchesModule = filterModule === 'ALL' || log.module === filterModule;
    return matchesSearch && matchesModule;
  });

  const modules = ['ALL', ...Array.from(new Set(logs.map(log => log.module).filter(Boolean)))];

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-100 flex flex-col h-full overflow-hidden">
      <div className="p-6 border-b border-gray-200">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
              <Shield className="text-blue-700" size={24} />
              System Audit Trail
            </h2>
            <p className="text-gray-500 text-sm mt-1">
              Backend audit events captured from operational actions and system workflows.
              <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-800">
                <Lock size={10} className="mr-1" /> Read Only
              </span>
            </p>
          </div>
          <div className="text-xs text-gray-500">Showing backend audit records</div>
        </div>

        <div className="flex gap-4 items-center">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
            <input
              type="text"
              placeholder="Search user, action, event ID, or entity..."
              className="w-full pl-9 pr-4 py-2 bg-gray-50 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          <div className="flex items-center gap-2">
            <Filter size={16} className="text-gray-500" />
            <select
              className="bg-gray-50 border border-gray-300 text-gray-700 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block p-2"
              value={filterModule}
              onChange={(e) => setFilterModule(e.target.value)}
            >
              {modules.map((module) => (
                <option key={module} value={module}>{module === 'ALL' ? 'All Modules' : module}</option>
              ))}
            </select>
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-auto">
        <table className="w-full text-left text-sm">
          <thead className="bg-gray-50 text-gray-600 font-medium sticky top-0 z-10">
            <tr>
              <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Event ID</th>
              <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Timestamp</th>
              <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">User</th>
              <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Module</th>
              <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Action</th>
              <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Entity</th>
              <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Details</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {filteredLogs.map((log) => {
              const status = normalizeStatus(log.status);
              return (
                <tr key={log.id} className="hover:bg-gray-50 transition-colors group">
                  <td className="px-6 py-3 font-mono text-gray-500 text-xs">{log.id}</td>
                  <td className="px-6 py-3 text-gray-700 whitespace-nowrap flex items-center gap-2">
                    <Clock size={14} className="text-gray-400" />
                    {new Date(log.timestamp).toLocaleString()}
                  </td>
                  <td className="px-6 py-3 font-medium text-gray-900">
                    <span className="bg-blue-50 text-blue-700 px-2 py-0.5 rounded text-xs font-mono">
                      {log.user}
                    </span>
                  </td>
                  <td className="px-6 py-3 text-gray-600">{log.module}</td>
                  <td className="px-6 py-3 font-medium text-gray-800">{log.action}</td>
                  <td className="px-6 py-3 text-gray-500">
                    <div>{log.entityType || 'N/A'}</div>
                    <div className="font-mono text-xs">{log.entityId || '-'}</div>
                  </td>
                  <td className="px-6 py-3">
                    {status === 'SUCCESS' && (
                      <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-green-50 text-green-700">
                        <CheckCircle size={12} /> Success
                      </span>
                    )}
                    {status === 'FAILURE' && (
                      <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-red-50 text-red-700">
                        <AlertCircle size={12} /> Failure
                      </span>
                    )}
                    {status === 'WARNING' && (
                      <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-orange-50 text-orange-700">
                        <AlertCircle size={12} /> Warning
                      </span>
                    )}
                  </td>
                  <td className="px-6 py-3 text-gray-500 truncate max-w-xs" title={log.details || log.errorMessage || ''}>
                    {log.details || log.errorMessage || '-'}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        {filteredLogs.length === 0 && (
          <div className="flex flex-col items-center justify-center h-48 text-gray-500">
            <Search size={32} className="mb-2 opacity-20" />
            <p>No audit logs found matching criteria.</p>
          </div>
        )}
      </div>

      <div className="p-3 border-t border-gray-200 bg-gray-50 text-xs text-gray-500 flex justify-between items-center">
        <span>Showing {filteredLogs.length} records</span>
        <span className="font-mono">SOURCE: BACKEND_AUDIT_LOGS</span>
      </div>
    </div>
  );
};

export default AuditTrail;
