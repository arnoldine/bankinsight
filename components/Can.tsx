import React, { ReactNode } from 'react';
import { useBankingSystem } from '../hooks/useBankingSystem';
import { hasPermission, hasAnyPermission, hasAllPermissions } from '../lib/permissionUtils';

interface CanProps {
  permission?: string;
  anyPermission?: string[];
  allPermissions?: string[];
  children: ReactNode;
  fallback?: ReactNode;
}

export const Can: React.FC<CanProps> = ({
  permission,
  anyPermission,
  allPermissions,
  children,
  fallback = null
}) => {
  const { currentUser } = useBankingSystem();

  let hasAccess = true;

  if (permission && !hasPermission(currentUser, permission)) {
    hasAccess = false;
  }
  
  if (anyPermission && anyPermission.length > 0 && !hasAnyPermission(currentUser, anyPermission)) {
    hasAccess = false;
  }

  if (allPermissions && allPermissions.length > 0 && !hasAllPermissions(currentUser, allPermissions)) {
    hasAccess = false;
  }

  return hasAccess ? <>{children}</> : <>{fallback}</>;
};
