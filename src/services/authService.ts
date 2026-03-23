import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token?: string | null;
  refreshToken?: string | null;
  user?: {
    id: string;
    name: string;
    email: string;
    role?: string;
    roleName?: string;
    permissions?: string[];
    status?: string;
  };
  mfaRequired?: boolean;
  mfaToken?: string | null;
  deliveryChannel?: string | null;
  deliveryHint?: string | null;
  deliveryStatus?: string | null;
  deliveryMessage?: string | null;
  mfaExpiresAtUtc?: string | null;
  allowedFactors?: string[];
  debugCode?: string | null;
}

export interface VerifyMfaRequest {
  mfaToken: string;
  code: string;
}

export interface User {
  id: string;
  name: string;
  email: string;
  role: string;
  permissions?: string[];
  status?: string;
}

class AuthService {
  private tokenKey = 'auth_token';
  private userKey = 'auth_user';
  private refreshTokenKey = 'refresh_token';
  private isDevMode = Boolean((import.meta as any).env?.DEV);

  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await httpClient.post<LoginResponse>(
      API_ENDPOINTS.auth.login,
      credentials
    );

    if (response.token) {
      this.setToken(response.token);
      if (response.user) {
        this.setUser(this.normalizeUser(response.user));
      }
      if (response.refreshToken) {
        this.setRefreshToken(response.refreshToken);
      }
    }

    return response;
  }

  async verifyMfa(payload: VerifyMfaRequest): Promise<LoginResponse> {
    const response = await httpClient.post<LoginResponse>(
      API_ENDPOINTS.auth.verifyMfa,
      payload
    );

    if (response.token) {
      this.setToken(response.token);
      if (response.user) {
        this.setUser(this.normalizeUser(response.user));
      }
      if (response.refreshToken) {
        this.setRefreshToken(response.refreshToken);
      }
    }

    return response;
  }

  async logout(): Promise<void> {
    try {
      await httpClient.post(API_ENDPOINTS.auth.logout);
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      this.clearAuth();
    }
  }

  async validateToken(): Promise<boolean> {
    try {
      const token = this.getToken();
      if (!token) return false;

      if (this.isTokenExpired(token)) {
        this.clearAuth();
        return false;
      }

      // In development, trust non-expired local token and avoid startup validate call noise.
      if (this.isDevMode) {
        const currentUser = this.getUser();
        return !!currentUser;
      }

      await httpClient.get(API_ENDPOINTS.auth.validate);
      try {
        const currentUser = await httpClient.get<User & { roleName?: string }>(API_ENDPOINTS.auth.me);
        this.setUser(this.normalizeUser(currentUser));
      } catch {
      }
      return true;
    } catch (error) {
      this.clearAuth();
      return false;
    }
  }

  async refreshToken(): Promise<string | null> {
    try {
      const refreshToken = this.getRefreshToken();
      if (!refreshToken) return null;

      const response = await httpClient.post<{ token: string; refreshToken?: string | null }>(
        API_ENDPOINTS.auth.refresh,
        { refreshToken }
      );

      if (response.token) {
        this.setToken(response.token);
        if (response.refreshToken) {
          this.setRefreshToken(response.refreshToken);
        }
        return response.token;
      }

      return null;
    } catch (error) {
      console.error('Token refresh error:', error);
      this.clearAuth();
      return null;
    }
  }

  setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  setRefreshToken(token: string): void {
    localStorage.setItem(this.refreshTokenKey, token);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  setUser(user: User): void {
    localStorage.setItem(this.userKey, JSON.stringify(user));
  }

  getUser(): User | null {
    const user = localStorage.getItem(this.userKey);
    return user ? JSON.parse(user) : null;
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  clearAuth(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    localStorage.removeItem(this.refreshTokenKey);
  }

  getAuthHeader(): { Authorization: string } | {} {
    const token = this.getToken();
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  private normalizeUser(user: { id: string; name: string; email: string; permissions?: string[]; status?: string; roleName?: string; role?: string }): User {
    return {
      ...user,
      role: user.role || user.roleName || 'Unknown',
    };
  }

  private isTokenExpired(token: string): boolean {
    try {
      const [, payloadBase64] = token.split('.');
      if (!payloadBase64) {
        return false;
      }

      const normalized = payloadBase64.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
      const payloadJson = atob(padded);
      const payload = JSON.parse(payloadJson) as { exp?: number };

      if (!payload.exp) {
        return false;
      }

      const nowInSeconds = Math.floor(Date.now() / 1000);
      return payload.exp <= nowInSeconds;
    } catch {
      return false;
    }
  }
}

export const authService = new AuthService();
