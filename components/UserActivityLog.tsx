import React, { useState, useEffect } from 'react';
import { Activity, Filter, Calendar, User, Eye } from 'lucide-react';

interface UserActivityItem {
  id: number;
  staffId: string;
  staffName: string;
  action: string;
  entityType: string;
  entityId?: string;
  ipAddress?: string;
  createdAt: string;
}

interface ActivityReport {
  staffId: string;
  staffName: string;
  totalActions: number;
  actionCounts: Record<string, number>;
  firstActivity: string;
  lastActivity: string;
}

export function UserActivityLog() {
  const [activities, setActivities] = useState<UserActivityItem[]>([]);
  const [report, setReport] = useState<ActivityReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [viewMode, setViewMode] = useState<'recent' | 'report'>('recent');
  const [selectedStaffId, setSelectedStaffId] = useState<string>('');
  const [limit, setLimit] = useState(50);

  useEffect(() => {
    loadActivities();
  }, [limit]);

  const loadActivities = async () => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch(`http://localhost:5176/api/useractivity/recent?limit=${limit}`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        setActivities(await res.json());
      }
    } catch (error) {
      console.error('Failed to load activities:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadReport = async () => {
    if (!selectedStaffId) return;

    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch(`http://localhost:5176/api/useractivity/report/${selectedStaffId}`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        setReport(await res.json());
      }
    } catch (error) {
      console.error('Failed to load report:', error);
    }
  };

  const formatDateTime = (date: string) => {
    return new Date(date).toLocaleString();
  };

  const getActionColor = (action: string) => {
    if (action.includes('CREATE') || action.includes('ADD')) return 'text-green-600 bg-green-50';
    if (action.includes('UPDATE') || action.includes('EDIT')) return 'text-blue-600 bg-blue-50';
    if (action.includes('DELETE') || action.includes('REMOVE')) return 'text-red-600 bg-red-50';
    if (action.includes('VIEW') || action.includes('READ')) return 'text-gray-600 bg-gray-50';
    return 'text-indigo-600 bg-indigo-50';
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Activity className="w-6 h-6 text-indigo-600" />
          <h2 className="text-xl font-semibold text-gray-900">User Activity Log</h2>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setViewMode('recent')}
            className={`px-4 py-2 rounded-lg transition-colors ${
              viewMode === 'recent'
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            Recent Activity
          </button>
          <button
            onClick={() => setViewMode('report')}
            className={`px-4 py-2 rounded-lg transition-colors ${
              viewMode === 'report'
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            Reports
          </button>
        </div>
      </div>

      {viewMode === 'recent' && (
        <>
          <div className="mb-4 flex items-center gap-4">
            <div className="flex items-center gap-2">
              <Filter className="w-5 h-5 text-gray-400" />
              <select
                value={limit}
                onChange={(e) => setLimit(parseInt(e.target.value))}
                className="border border-gray-300 rounded-lg px-3 py-2"
              >
                <option value={25}>Last 25</option>
                <option value={50}>Last 50</option>
                <option value={100}>Last 100</option>
              </select>
            </div>
            <button
              onClick={loadActivities}
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
            >
              Refresh
            </button>
          </div>

          <div className="space-y-2">
            {loading ? (
              <div className="text-center py-8 text-gray-500">Loading activities...</div>
            ) : activities.length === 0 ? (
              <div className="text-center py-8 text-gray-500">No activities found</div>
            ) : (
              activities.map((activity) => (
                <div
                  key={activity.id}
                  className="border border-gray-200 rounded-lg p-3 hover:bg-gray-50 transition-colors"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <User className="w-4 h-4 text-gray-400" />
                        <span className="font-medium text-gray-900">{activity.staffName}</span>
                        <span className={`text-xs px-2 py-1 rounded ${getActionColor(activity.action)}`}>
                          {activity.action}
                        </span>
                      </div>
                      
                      <div className="text-sm text-gray-600 space-y-1">
                        <div>
                          <span className="font-medium">{activity.entityType}</span>
                          {activity.entityId && <span className="ml-2">ID: {activity.entityId}</span>}
                        </div>
                        {activity.ipAddress && (
                          <div className="text-xs text-gray-500">From: {activity.ipAddress}</div>
                        )}
                      </div>
                    </div>
                    
                    <div className="text-xs text-gray-500 text-right">
                      {formatDateTime(activity.createdAt)}
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </>
      )}

      {viewMode === 'report' && (
        <div className="space-y-6">
          <div className="flex items-center gap-4">
            <input
              type="text"
              value={selectedStaffId}
              onChange={(e) => setSelectedStaffId(e.target.value)}
              placeholder="Enter Staff ID"
              className="flex-1 border border-gray-300 rounded-lg px-4 py-2"
            />
            <button
              onClick={loadReport}
              className="px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
            >
              Generate Report
            </button>
          </div>

          {report && (
            <div className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="bg-gradient-to-br from-blue-50 to-blue-100 rounded-lg p-4">
                  <div className="text-sm font-medium text-blue-900 mb-1">Total Actions</div>
                  <div className="text-2xl font-bold text-blue-900">{report.totalActions}</div>
                </div>
                
                <div className="bg-gradient-to-br from-green-50 to-green-100 rounded-lg p-4">
                  <div className="text-sm font-medium text-green-900 mb-1">User</div>
                  <div className="text-lg font-semibold text-green-900 truncate">{report.staffName}</div>
                </div>
                
                <div className="bg-gradient-to-br from-purple-50 to-purple-100 rounded-lg p-4">
                  <div className="text-sm font-medium text-purple-900 mb-1">Action Types</div>
                  <div className="text-2xl font-bold text-purple-900">
                    {Object.keys(report.actionCounts).length}
                  </div>
                </div>
              </div>

              <div>
                <h3 className="text-sm font-medium text-gray-700 mb-3">Actions Breakdown</h3>
                <div className="space-y-2">
                  {Object.entries(report.actionCounts).map(([action, count]) => (
                    <div key={action} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                      <span className="text-sm font-medium text-gray-700">{action}</span>
                      <div className="flex items-center gap-4">
                        <div className="w-32 bg-gray-200 rounded-full h-2">
                          <div
                            className="bg-indigo-600 h-2 rounded-full"
                            style={{ width: `${(count / report.totalActions) * 100}%` }}
                          />
                        </div>
                        <span className="text-sm font-semibold text-gray-900 w-12 text-right">{count}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4 text-sm">
                <div className="p-3 bg-gray-50 rounded-lg">
                  <div className="text-gray-600 mb-1">First Activity</div>
                  <div className="font-medium text-gray-900">{formatDateTime(report.firstActivity)}</div>
                </div>
                <div className="p-3 bg-gray-50 rounded-lg">
                  <div className="text-gray-600 mb-1">Last Activity</div>
                  <div className="font-medium text-gray-900">{formatDateTime(report.lastActivity)}</div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
