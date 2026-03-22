// components/PermissionGuard.tsx
// Component to conditionally render content based on user permissions

import React from 'react';
import { hasPermission, hasAnyPermission, hasAllPermissions } from '../lib/jwtUtils';
import { Permission } from '../types';

interface PermissionGuardProps {
  permission?: Permission | Permission[];
  requireAll?: boolean;
  fallback?: React.ReactNode;
  children: React.ReactNode;
  token?: string | null;
}

/**
 * Conditionally render content based on user permissions
 * 
 * Usage:
 * <PermissionGuard permission="MANAGE_USERS">
 *   <button>Manage Users</button>
 * </PermissionGuard>
 * 
 * Multiple permissions (OR logic - has any):
 * <PermissionGuard permission={["MANAGE_USERS", "SYSTEM_ADMIN"]}>
 *   <button>Manage Users or Admin</button>
 * </PermissionGuard>
 * 
 * Multiple permissions (AND logic - has all):
 * <PermissionGuard permission={["MANAGE_USERS", "MANAGE_ROLES"]} requireAll>
 *   <button>Manage Users AND Roles</button>
 * </PermissionGuard>
 */
export const PermissionGuard: React.FC<PermissionGuardProps> = ({
  permission,
  requireAll = false,
  fallback = null,
  children,
  token
}) => {
  // Get token from props or localStorage
  const authToken = token || (typeof window !== 'undefined' ? localStorage.getItem('bankinsight_token') : null);
  
  if (!authToken) {
    return <>{fallback}</>;
  }

  // Handle no permission specified
  if (!permission) {
    return <>{children}</>;
  }

  let hasAccess = false;

  if (Array.isArray(permission)) {
    if (requireAll) {
      // Check if user has ALL permissions
      hasAccess = permission.every(p => hasPermission(authToken, p));
    } else {
      // Check if user has ANY permission
      hasAccess = hasAnyPermission(authToken, permission);
    }
  } else {
    // Single permission
    hasAccess = hasPermission(authToken, permission);
  }

  return <>{hasAccess ? children : fallback}</>;
};

export default PermissionGuard;
