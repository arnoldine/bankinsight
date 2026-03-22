// lib/jwtUtils.ts
// Utilities for parsing and working with JWT tokens

export interface JWTPayload {
  sub?: string;
  email?: string;
  role_id?: string;
  branch_id?: string;
  permissions?: string[];
  exp?: number;
  iat?: number;
  iss?: string;
  aud?: string;
  [key: string]: any;
}

/**
 * Decode JWT token without verification (client-side only)
 * WARNING: Never trust decoded token claims - always validate on server
 */
export function decodeJWT(token: string): JWTPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    let payload = parts[1];
    // Add padding if necessary
    while (payload.length % 4 !== 0) {
      payload += '=';
    }

    const decoded = JSON.parse(atob(payload));
    return decoded;
  } catch (error) {
    console.error('Failed to decode JWT:', error);
    return null;
  }
}

/**
 * Get permissions from JWT token
 */
export function getPermissionsFromToken(token: string): string[] {
  const payload = decodeJWT(token);
  if (!payload) return [];
  return payload.permissions || [];
}

/**
 * Check if token is expired
 */
export function isTokenExpired(token: string): boolean {
  const payload = decodeJWT(token);
  if (!payload || !payload.exp) return true;

  const expirationTime = payload.exp * 1000; // Convert to milliseconds
  return Date.now() >= expirationTime;
}

/**
 * Get user role from token
 */
export function getRoleFromToken(token: string): string | null {
  const payload = decodeJWT(token);
  return payload?.role_id || null;
}

/**
 * Get user email from token
 */
export function getEmailFromToken(token: string): string | null {
  const payload = decodeJWT(token);
  return payload?.email || null;
}

/**
 * Get branch ID from token
 */
export function getBranchFromToken(token: string): string | null {
  const payload = decodeJWT(token);
  return payload?.branch_id || null;
}

/**
 * Check if user has specific permission
 */
export function hasPermission(token: string, permission: string): boolean {
  const permissions = getPermissionsFromToken(token);
  // Check for specific permission or SYSTEM_ADMIN bypass
  return permissions.includes(permission) || permissions.includes('SYSTEM_ADMIN');
}

/**
 * Check if user has any of the specified permissions
 */
export function hasAnyPermission(token: string, permissions: string[]): boolean {
  const userPermissions = getPermissionsFromToken(token);
  if (userPermissions.includes('SYSTEM_ADMIN')) return true;
  return permissions.some(p => userPermissions.includes(p));
}

/**
 * Check if user has all of the specified permissions
 */
export function hasAllPermissions(token: string, permissions: string[]): boolean {
  const userPermissions = getPermissionsFromToken(token);
  if (userPermissions.includes('SYSTEM_ADMIN')) return true;
  return permissions.every(p => userPermissions.includes(p));
}
