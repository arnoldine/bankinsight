import React, { useState, useEffect } from 'react';
import { Server, Save, CheckCircle, AlertCircle, Key, Shield, Database } from 'lucide-react';
import {
  getApiConfig,
  setApiConfig,
  setSystemAdminPassword,
  buildApiBaseUrl,
  type ApiConfig,
} from '../lib/configStore';

type SetupTab = 'api' | 'admin';

interface SetupProps {
  /** When system admin is viewing setup */
  isSystemAdmin: boolean;
  /** Optional: initial tab to show (e.g. from URL or post-login redirect) */
  initialTab?: SetupTab;
}

const Setup: React.FC<SetupProps> = ({ isSystemAdmin, initialTab = 'api' }) => {
  const [activeTab, setActiveTab] = useState<SetupTab>(initialTab);
  const [apiConfig, setApiConfigState] = useState<ApiConfig>(getApiConfig());
  const [adminPassword, setAdminPassword] = useState('');
  const [adminConfirm, setAdminConfirm] = useState('');
  const [savedApi, setSavedApi] = useState(false);
  const [savedAdmin, setSavedAdmin] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setApiConfigState(getApiConfig());
  }, [activeTab]);

  const handleSaveApi = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSavedApi(false);
    try {
      new URL(apiConfig.baseURL);
      setApiConfig(apiConfig);
      setSavedApi(true);
      setTimeout(() => setSavedApi(false), 3000);
    } catch (err) {
      setError('Invalid base URL format. Use http://host:port or https://host:port');
    }
  };

  const handleSaveAdmin = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSavedAdmin(false);
    if (adminPassword && adminPassword !== adminConfirm) {
      setError('Passwords do not match.');
      return;
    }
    if (adminPassword) {
      setSystemAdminPassword(adminPassword);
      setAdminPassword('');
      setAdminConfirm('');
    }
    setSavedAdmin(true);
    setTimeout(() => setSavedAdmin(false), 3000);
  };

  const previewUrl = buildApiBaseUrl();

  const tabs: { id: SetupTab; label: string; icon: React.ReactNode }[] = [
    { id: 'api', label: 'MySQL API Config', icon: <Database size={18} /> },
    { id: 'admin', label: 'System Admin Password', icon: <Key size={18} /> },
  ];

  return (
    <div className="max-w-4xl mx-auto">
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div className="border-b border-gray-200 bg-gray-50 px-6 py-4">
          <h2 className="text-xl font-bold text-gray-800 flex items-center gap-2">
            <Shield size={24} className="text-blue-600" />
            Setup
          </h2>
          <p className="text-sm text-gray-500 mt-1">
            Configure system admin password and MySQL API connection. Stored locally in this browser.
          </p>
        </div>

        <div className="flex border-b border-gray-200">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => { setActiveTab(tab.id); setError(null); }}
              className={`flex items-center gap-2 px-6 py-4 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.id
                  ? 'border-blue-600 text-blue-600 bg-blue-50/50'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:bg-gray-50'
              }`}
            >
              {tab.icon}
              {tab.label}
            </button>
          ))}
        </div>

        <div className="p-6">
          {activeTab === 'api' && (
            <form onSubmit={handleSaveApi} className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="md:col-span-2">
                  <label className="block text-xs font-bold text-gray-500 uppercase mb-2">
                    API Base URL <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    value={apiConfig.baseURL}
                    onChange={(e) => setApiConfigState((c) => ({ ...c, baseURL: e.target.value }))}
                    className="w-full p-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 focus:border-blue-500 font-mono"
                    placeholder="http://localhost:3001/api"
                    required
                  />
                  <p className="text-xs text-gray-500 mt-1">Full base URL of your MySQL API service (e.g. http://localhost:3001/api)</p>
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-500 uppercase mb-2">API Version Prefix (optional)</label>
                  <input
                    type="text"
                    value={apiConfig.apiVersion || ''}
                    onChange={(e) => setApiConfigState((c) => ({ ...c, apiVersion: e.target.value || undefined }))}
                    className="w-full p-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    placeholder="/v1"
                  />
                  <p className="text-xs text-gray-500 mt-1">API version prefix (e.g. /v1). Leave empty if not used.</p>
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-500 uppercase mb-2">Request timeout (ms)</label>
                  <input
                    type="number"
                    value={apiConfig.timeoutMs}
                    onChange={(e) => setApiConfigState((c) => ({ ...c, timeoutMs: parseInt(e.target.value, 10) || 30000 }))}
                    className="w-full p-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    min={5000}
                    max={120000}
                  />
                </div>
                <div className="md:col-span-2">
                  <label className="block text-xs font-bold text-gray-500 uppercase mb-2">API Key (optional)</label>
                  <input
                    type="password"
                    value={apiConfig.apiKey || ''}
                    onChange={(e) => setApiConfigState((c) => ({ ...c, apiKey: e.target.value || undefined }))}
                    className="w-full p-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 focus:border-blue-500 font-mono"
                    placeholder="Leave empty if API doesn't require key"
                  />
                  <p className="text-xs text-gray-500 mt-1">API key for authentication (sent as X-API-Key header)</p>
                </div>
              </div>
              <div className="bg-gray-50 border border-gray-200 rounded p-4">
                <div className="text-xs font-bold text-gray-500 uppercase mb-1">Preview API Base URL</div>
                <code className="text-sm text-blue-700 break-all">{previewUrl}</code>
                <p className="text-xs text-gray-500 mt-2">Example endpoints: {previewUrl}/customers, {previewUrl}/accounts, {previewUrl}/transactions</p>
              </div>
              {error && (
                <div className="flex items-center gap-2 text-red-600 text-sm">
                  <AlertCircle size={18} />
                  {error}
                </div>
              )}
              <div className="flex items-center gap-3">
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 flex items-center gap-2"
                >
                  <Save size={18} />
                  Save API Config
                </button>
                {savedApi && (
                  <span className="flex items-center gap-2 text-green-600 text-sm">
                    <CheckCircle size={18} />
                    Saved to localStorage
                  </span>
                )}
              </div>
            </form>
          )}

          {activeTab === 'admin' && (
            <form onSubmit={handleSaveAdmin} className="space-y-6 max-w-md">
              <p className="text-sm text-gray-600">
                Set or change the system admin password used to access Setup and sensitive configuration. Stored in this browser only.
              </p>
              <div>
                <label className="block text-xs font-bold text-gray-500 uppercase mb-2">New system admin password</label>
                <input
                  type="password"
                  value={adminPassword}
                  onChange={(e) => setAdminPassword(e.target.value)}
                  className="w-full p-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Leave blank to keep current"
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-gray-500 uppercase mb-2">Confirm password</label>
                <input
                  type="password"
                  value={adminConfirm}
                  onChange={(e) => setAdminConfirm(e.target.value)}
                  className="w-full p-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="••••••••"
                />
              </div>
              {error && (
                <div className="flex items-center gap-2 text-red-600 text-sm">
                  <AlertCircle size={18} />
                  {error}
                </div>
              )}
              <div className="flex items-center gap-3">
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 flex items-center gap-2"
                >
                  <Save size={18} />
                  Save admin password
                </button>
                {savedAdmin && (
                  <span className="flex items-center gap-2 text-green-600 text-sm">
                    <CheckCircle size={18} />
                    Saved
                  </span>
                )}
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
};

export default Setup;
