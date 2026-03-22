import React from 'react';
import { useBankingSystem } from '../../hooks/useBankingSystem';
import { ShieldAlert } from 'lucide-react';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPermission: string;
  userPermissions?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredPermission, userPermissions }) => {
  const { hasPermission } = useBankingSystem();
  const isAllowed = userPermissions
    ? userPermissions.includes(requiredPermission)
    : hasPermission(requiredPermission);

  if (!isAllowed) {
    return (
      <div className="flex flex-col items-center justify-center p-8 h-full bg-red-50/50 rounded-xl border border-red-100">
        <div className="w-16 h-16 bg-red-100 text-red-600 rounded-full flex items-center justify-center mb-4 shadow-sm">
          <ShieldAlert size={32} />
        </div>
        <h2 className="text-xl font-bold text-gray-800 mb-2">Access Denied</h2>
        <p className="text-gray-500 text-center max-w-md">
          You do not have the required clearance (<span className="font-mono text-red-600 text-xs bg-red-50 px-1 rounded">{requiredPermission}</span>) to view this screen. Please contact your system administrator if you believe this is an error.
        </p>
      </div>
    );
  }

  return <>{children}</>;
};
