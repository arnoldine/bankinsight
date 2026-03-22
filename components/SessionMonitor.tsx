import React, { useState, useEffect } from 'react';
import { User, Shield, Clock, Activity, Eye, LogOut } from 'lucide-react';

interface Session {
  id: string;
  staffId: string;
  staffName: string;
  email: string;
  ipAddress: string;
  userAgent?: string;
  createdAt: string;
  expiresAt: string;
  lastActivity: string;
  isActive: boolean;
}

interface SessionStats {
  totalActiveSessions: number;
  totalSessionsToday: number;
  sessionsByBranch: Record<string, number>;
}

export function SessionMonitor() {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [stats, setStats] = useState<SessionStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedTab, setSelectedTab] = useState<'active' | 'stats'>('active');

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 30000); // Refresh every 30 seconds
    return () => clearInterval(interval);
  }, []);

  const loadData = async () => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const headers = {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      };

      const [sessionsRes, statsRes] = await Promise.all([
        fetch('http://localhost:5176/api/session/active', { headers }),
        fetch('http://localhost:5176/api/session/stats', { headers })
      ]);

      if (sessionsRes.ok && statsRes.ok) {
        setSessions(await sessionsRes.json());
        setStats(await statsRes.json());
      }
    } catch (error) {
      console.error('Failed to load session data:', error);
    } finally {
      setLoading(false);
    }
  };

  const invalidateSession = async (sessionId: string) => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch(`http://localhost:5176/api/session/${sessionId}/invalidate`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        loadData();
      }
    } catch (error) {
      console.error('Failed to invalidate session:', error);
    }
  };

  const formatDateTime = (date: string) => {
    return new Date(date).toLocaleString();
  };

  const formatDuration = (date: string) => {
    const diff = new Date().getTime() - new Date(date).getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    
    if (hours > 0) return `${hours}h ${minutes % 60}m ago`;
    return `${minutes}m ago`;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-gray-500">Loading session data...</div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Clock className="w-6 h-6 text-indigo-600" />
          <h2 className="text-xl font-semibold text-gray-900">Session Monitor</h2>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setSelectedTab('active')}
            className={`px-4 py-2 rounded-lg transition-colors ${
              selectedTab === 'active'
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            Active Sessions
          </button>
          <button
            onClick={() => setSelectedTab('stats')}
            className={`px-4 py-2 rounded-lg transition-colors ${
              selectedTab === 'stats'
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            Statistics
          </button>
        </div>
      </div>

      {selectedTab === 'active' && (
        <div className="space-y-4">
          {sessions.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              No active sessions
            </div>
          ) : (
            sessions.map((session) => (
              <div
                key={session.id}
                className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <User className="w-5 h-5 text-gray-400" />
                      <span className="font-medium text-gray-900">{session.staffName}</span>
                      <span className="text-sm text-gray-500">{session.email}</span>
                    </div>
                    
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2 text-gray-600">
                          <Activity className="w-4 h-4" />
                          <span>IP: {session.ipAddress}</span>
                        </div>
                        <div className="text-gray-500">
                          Created: {formatDateTime(session.createdAt)}
                        </div>
                      </div>
                      
                      <div className="space-y-1">
                        <div className="text-gray-600">
                          Last Activity: {formatDuration(session.lastActivity)}
                        </div>
                        <div className="text-gray-500">
                          Expires: {formatDateTime(session.expiresAt)}
                        </div>
                      </div>
                    </div>

                    {session.userAgent && (
                      <div className="mt-2 text-xs text-gray-400 truncate">
                        {session.userAgent}
                      </div>
                    )}
                  </div>

                  <button
                    onClick={() => invalidateSession(session.id)}
                    className="ml-4 p-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    title="Invalidate Session"
                  >
                    <LogOut className="w-5 h-5" />
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      )}

      {selectedTab === 'stats' && stats && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-gradient-to-br from-indigo-50 to-indigo-100 rounded-lg p-6">
            <div className="flex items-center gap-3 mb-2">
              <Activity className="w-6 h-6 text-indigo-600" />
              <span className="text-sm font-medium text-indigo-900">Active Now</span>
            </div>
            <div className="text-3xl font-bold text-indigo-900">{stats.totalActiveSessions}</div>
          </div>

          <div className="bg-gradient-to-br from-green-50 to-green-100 rounded-lg p-6">
            <div className="flex items-center gap-3 mb-2">
              <Clock className="w-6 h-6 text-green-600" />
              <span className="text-sm font-medium text-green-900">Today's Total</span>
            </div>
            <div className="text-3xl font-bold text-green-900">{stats.totalSessionsToday}</div>
          </div>

          <div className="bg-gradient-to-br from-purple-50 to-purple-100 rounded-lg p-6">
            <div className="flex items-center gap-3 mb-2">
              <Shield className="w-6 h-6 text-purple-600" />
              <span className="text-sm font-medium text-purple-900">Branches</span>
            </div>
            <div className="text-3xl font-bold text-purple-900">
              {Object.keys(stats.sessionsByBranch).length}
            </div>
          </div>

          {Object.entries(stats.sessionsByBranch).length > 0 && (
            <div className="col-span-full">
              <h3 className="text-sm font-medium text-gray-700 mb-3">Sessions by Branch</h3>
              <div className="space-y-2">
                {Object.entries(stats.sessionsByBranch).map(([branchId, count]) => (
                  <div key={branchId} className="flex items-center justify-between p-3 bg-gray-50 rounded">
                    <span className="text-sm text-gray-700">{branchId}</span>
                    <span className="font-semibold text-gray-900">{count}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
