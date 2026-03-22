/**
 * BankInsight localStorage config store.
 * Stores system admin password and MySQL API connection settings.
 */

const STORAGE_KEY_ADMIN = 'bankinsight_admin';
const STORAGE_KEY_API = 'bankinsight_api_config';

/** Default system admin account for initial setup when API is unavailable. Change in Setup → System Admin Password. */
export const DEFAULT_SYSTEM_ADMIN_ID = 'admin';
export const DEFAULT_SYSTEM_ADMIN_PASSWORD = 'admin123';

export interface ApiConfig {
  baseURL: string;
  apiKey?: string;
  timeoutMs: number;
  /** Optional: API version prefix (e.g. '/api/v1') */
  apiVersion?: string;
}

export interface AdminConfig {
  /** Hashed or plain system admin password (use hash in production) */
  systemAdminPassword: string;
}

const DEFAULT_API: ApiConfig = {
  baseURL: 'http://localhost:5176/api',
  timeoutMs: 30000,
  apiVersion: '',
};

const DEFAULT_ADMIN: AdminConfig = {
  systemAdminPassword: DEFAULT_SYSTEM_ADMIN_PASSWORD,
};

function getItem<T>(key: string, defaultValue: T): T {
  try {
    const raw = localStorage.getItem(key);
    if (raw == null) return defaultValue;
    return JSON.parse(raw) as T;
  } catch {
    return defaultValue;
  }
}

function setItem(key: string, value: unknown): void {
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch (e) {
    console.warn('configStore setItem failed', e);
  }
}

export function getApiConfig(): ApiConfig {
  return getItem<ApiConfig>(STORAGE_KEY_API, DEFAULT_API);
}

export function setApiConfig(config: Partial<ApiConfig>): void {
  const current = getApiConfig();
  setItem(STORAGE_KEY_API, { ...current, ...config });
}

export function getAdminConfig(): AdminConfig {
  return getItem<AdminConfig>(STORAGE_KEY_ADMIN, DEFAULT_ADMIN);
}

export function setAdminConfig(config: Partial<AdminConfig>): void {
  const current = getAdminConfig();
  setItem(STORAGE_KEY_ADMIN, { ...current, ...config });
}

/** System admin password (stored separately for clarity) */
export function getSystemAdminPassword(): string {
  return getAdminConfig().systemAdminPassword;
}

export function setSystemAdminPassword(password: string): void {
  setAdminConfig({ systemAdminPassword: password });
}

/**
 * Build the full API base URL from stored config.
 * Returns the configured baseURL with optional version prefix.
 */
export function buildApiBaseUrl(): string {
  const c = getApiConfig();
  const base = c.baseURL.replace(/\/$/, '');
  const version = c.apiVersion ? c.apiVersion.replace(/^\//, '') : '';
  return version ? `${base}/${version}` : base;
}
