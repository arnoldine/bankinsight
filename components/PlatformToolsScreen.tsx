import React from 'react';
import { Shield, RefreshCw, Zap, Settings2 } from 'lucide-react';

const tools = [
  {
    name: 'Security Audit',
    description: 'Run security checks and view audit logs.',
    icon: <Shield className="h-8 w-8 text-blue-600" />,
    action: () => window.open('/audit', '_blank'),
  },
  {
    name: 'System Health',
    description: 'Monitor system health and uptime.',
    icon: <RefreshCw className="h-8 w-8 text-green-600" />,
    action: () => window.open('/health', '_blank'),
  },
  {
    name: 'Performance Tools',
    description: 'Analyze and optimize performance.',
    icon: <Zap className="h-8 w-8 text-yellow-500" />,
    action: () => window.open('/performance', '_blank'),
  },
  {
    name: 'Configuration',
    description: 'Manage environment and platform settings.',
    icon: <Settings2 className="h-8 w-8 text-gray-700" />,
    action: () => window.open('/settings', '_blank'),
  },
];

const PlatformToolsScreen: React.FC = () => {
  return (
    <div className="p-8 max-w-4xl mx-auto">
      <h1 className="text-3xl font-bold mb-6">Platform Tools</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {tools.map((tool) => (
          <div
            key={tool.name}
            className="bg-white dark:bg-slate-900 rounded-xl shadow p-6 flex items-center space-x-4 hover:shadow-lg transition cursor-pointer border border-slate-200 dark:border-slate-700"
            onClick={tool.action}
            tabIndex={0}
            role="button"
            aria-label={tool.name}
          >
            <div>{tool.icon}</div>
            <div>
              <div className="text-lg font-semibold">{tool.name}</div>
              <div className="text-slate-600 dark:text-slate-300 text-sm">{tool.description}</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default PlatformToolsScreen;
