import React, { useState, useEffect } from 'react';
import { useBankingSystem } from '../../hooks/useBankingSystem';
import { Permissions } from '../../lib/Permissions';
import { RefreshCw } from 'lucide-react';
import { httpClient } from '../services/httpClient';

export default function TaskInbox() {
  const [tasks, setTasks] = useState<any[]>([]);
  const [claimableTasks, setClaimableTasks] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'my' | 'claimable'>('my');
  const { hasPermission } = useBankingSystem();
  
  const fetchTasks = async () => {
    setLoading(true);
    try {
      const myRes = await httpClient.get<any[]>('/WorkflowRuntime/tasks/my');
      setTasks(myRes || []);

      if (hasPermission(Permissions.Tasks?.Claim || 'tasks.claim')) {
        const claimRes = await httpClient.get<any[]>('/WorkflowRuntime/tasks/claimable');
        setClaimableTasks(claimRes || []);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTasks();
  }, [activeTab]);

  const handleClaim = async (taskId: string) => {
    try {
      await httpClient.post(`/WorkflowRuntime/tasks/${taskId}/claim`);
      fetchTasks();
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div className="p-6 h-full flex flex-col">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Task Inbox</h2>
          <p className="text-sm text-gray-500">Manage your assigned and claimable workflow tasks.</p>
        </div>
        <button onClick={fetchTasks} className="p-2 bg-slate-100 dark:bg-slate-800 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700">
          <RefreshCw size={20} className={loading ? 'animate-spin' : ''} />
        </button>
      </div>
      
      <div className="flex gap-4 mb-6 border-b border-gray-200 dark:border-gray-700">
        <button
          className={`pb-2 px-4 ${activeTab === 'my' ? 'border-b-2 border-brand-500 text-brand-500 font-medium' : 'text-gray-500'}`}
          onClick={() => setActiveTab('my')}
        >
          My Tasks ({tasks.length})
        </button>
        {hasPermission(Permissions.Tasks?.Claim || 'tasks.claim') && (
          <button
            className={`pb-2 px-4 ${activeTab === 'claimable' ? 'border-b-2 border-brand-500 text-brand-500 font-medium' : 'text-gray-500'}`}
            onClick={() => setActiveTab('claimable')}
          >
            Claimable Queue ({claimableTasks.length})
          </button>
        )}
      </div>
      
      <div className="bg-white dark:bg-drk-800 rounded-xl shadow-sm border border-slate-200 dark:border-drk-700 flex-1 overflow-y-auto">
        {activeTab === 'my' ? (
          tasks.map(t => (
            <div key={t.id} className="p-4 border-b border-slate-100 dark:border-drk-700 flex justify-between items-center bg-white dark:bg-drk-800 hover:bg-slate-50 dark:hover:bg-drk-700 transition">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white">{t.processStepDefinition?.stepName || 'Task'}</h4>
                <p className="text-sm text-gray-500 mt-1">Status: {t.status} | Process: {t.processInstance?.entityType} #{t.processInstance?.entityId}</p>
              </div>
              <div className="flex items-center gap-3">
                 <button className="px-4 py-2 bg-brand-500 text-white rounded-lg hover:bg-brand-600 shadow-sm font-medium transition-colors">Complete</button>
                 <button className="px-4 py-2 bg-rose-500 text-white rounded-lg hover:bg-rose-600 shadow-sm font-medium transition-colors">Reject</button>
              </div>
            </div>
          ))
        ) : (
          claimableTasks.map(t => (
            <div key={t.id} className="p-4 border-b border-slate-100 dark:border-drk-700 flex justify-between items-center bg-white dark:bg-drk-800 hover:bg-slate-50 dark:hover:bg-drk-700 transition">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white">{t.processStepDefinition?.stepName || 'Task'}</h4>
                <p className="text-sm text-gray-500 mt-1">Status: {t.status} | Assigned Queue: {t.assignedRoleCode || t.assignedPermissionCode}</p>
              </div>
              <div>
                 <button onClick={() => handleClaim(t.id)} className="px-4 py-2 bg-slate-700 text-white rounded-lg hover:bg-slate-800 shadow-sm font-medium transition-colors">Claim Ticket</button>
              </div>
            </div>
          ))
        )}
        {(activeTab === 'my' ? tasks : claimableTasks).length === 0 && !loading && (
           <div className="p-10 text-center text-gray-500 dark:text-gray-400">No active tasks in this queue.</div>
        )}
      </div>
    </div>
  );
}
