export type AuthScope = 'BranchOnly' | 'Regional' | 'All';

export interface AuthSession {
  token: string;
  expiresAt: string;
}

export interface AuthUser {
  id: string;
  email: string;
  name: string;
  branchId: string;
  scopeType?: AuthScope;
  roles?: string[];
  permissions?: string[];
}
