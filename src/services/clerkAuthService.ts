import { useAuth as useClerkAuth, useUser as useClerkUser } from '@clerk/react';
import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface User {
  id: string;
  name: string;
  email: string;
  role?: string;
  roleId?: string;
  branchId?: string;
  status?: string;
  permissions?: string[];
}

/**
 * Service to integrate Clerk authentication with BankInsight backend
 * Bridges Clerk's auth with our Staff/Role/Permission system
 */
class ClerkAuthService {
  async getCurrentUser(): Promise<User | null> {
    try {
      const response = await httpClient.get<any>(API_ENDPOINTS.clerk.me);
      return {
        id: response.id,
        name: response.name,
        email: response.email,
        role: response.role,
        roleId: response.roleId,
        branchId: response.branchId,
        status: response.status,
        permissions: response.permissions || []
      };
    } catch (error) {
      console.error('Failed to get current user:', error);
      return null;
    }
  }

  async syncClerkUser(clerkUserId: string, email: string): Promise<User | null> {
    try {
      const response = await httpClient.post<any>(`${API_ENDPOINTS.clerk.sync}`, {
        clerkUserId,
        email
      });
      return response;
    } catch (error) {
      console.error('Failed to sync Clerk user:', error);
      return null;
    }
  }

  async getAuthToken(): Promise<string | null> {
    try {
      const { getToken } = useClerkAuth();
      return await getToken();
    } catch (error) {
      console.error('Failed to get auth token:', error);
      return null;
    }
  }
}

export const clerkAuthService = new ClerkAuthService();

/**
 * React hook for Clerk authentication integration
 */
export function useClerkBankInsightAuth() {
  const { isSignedIn, getToken, signOut } = useClerkAuth();
  const { user: clerkUser } = useClerkUser();

  return {
    isSignedIn,
    clerkUser,
    getToken,
    signOut,
    getCurrentUser: clerkAuthService.getCurrentUser,
    syncUser: clerkAuthService.syncClerkUser
  };
}
