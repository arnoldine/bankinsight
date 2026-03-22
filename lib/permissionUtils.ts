import { AuthUser } from "../types/auth";

export const hasPermission = (user: AuthUser | null | undefined, requiredPermission: string): boolean => {
  if (!user || !user.permissions) return false;
  return user.permissions.includes(requiredPermission);
};

export const hasAnyPermission = (user: AuthUser | null | undefined, requiredPermissions: string[]): boolean => {
  if (!user || !user.permissions) return false;
  return requiredPermissions.some(perm => user.permissions.includes(perm));
};

export const hasAllPermissions = (user: AuthUser | null | undefined, requiredPermissions: string[]): boolean => {
  if (!user || !user.permissions) return false;
  return requiredPermissions.every(perm => user.permissions.includes(perm));
};

export const getDefaultRoute = (permissions: string[] | undefined): string => {
  if (!permissions) return 'dashboard';
  
  const landingPriority = [
    { path: "dashboard", permission: "dashboard.view" },
    { path: "clients", permission: "customers.view" },
    { path: "accounts-list", permission: "accounts.view" },
    { path: "loans", permission: "loans.view" },
    { path: "transactions", permission: "transactions.view" },
    { path: "reporting", permission: "reports.view" },
    { path: "users", permission: "users.view" }
  ];

  for (const route of landingPriority) {
    if (permissions.includes(route.permission)) {
      return route.path;
    }
  }
  
  return 'dashboard';
};

