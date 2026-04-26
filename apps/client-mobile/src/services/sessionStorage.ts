import { Platform } from "react-native";
import * as SecureStore from "expo-secure-store";

const SESSION_KEY = "bankinsight-client-session";
const ACCESS_TOKEN_KEY = "bankinsight-client-access-token";
const REFRESH_TOKEN_KEY = "bankinsight-client-refresh-token";
const memoryStorage = new Map<string, string>();

export interface SessionUser {
  id: string;
  customerId?: string;
  name: string;
  email: string;
  role?: string;
  permissions?: string[];
  hasTransactionPin?: boolean;
}

export interface SessionState {
  isHydrating: boolean;
  isAuthenticated: boolean;
  displayName: string;
  user: SessionUser | null;
  accessToken: string | null;
  refreshToken: string | null;
}

const guestState: SessionState = {
  isHydrating: false,
  isAuthenticated: false,
  displayName: "Guest",
  user: null,
  accessToken: null,
  refreshToken: null
};

function isWebStorageAvailable() {
  return typeof window !== "undefined" && typeof window.localStorage !== "undefined";
}

function readWebStorage(key: string): string | null {
  if (!isWebStorageAvailable()) {
    return memoryStorage.get(key) ?? null;
  }

  try {
    return window.localStorage.getItem(key);
  } catch {
    return memoryStorage.get(key) ?? null;
  }
}

function writeWebStorage(key: string, value: string): void {
  memoryStorage.set(key, value);

  if (!isWebStorageAvailable()) {
    return;
  }

  try {
    window.localStorage.setItem(key, value);
  } catch {
    // Keep the in-memory fallback even when persistent storage is blocked.
  }
}

function removeWebStorage(key: string): void {
  memoryStorage.delete(key);

  if (!isWebStorageAvailable()) {
    return;
  }

  try {
    window.localStorage.removeItem(key);
  } catch {
    // Ignore persistent storage failures and keep the in-memory state clean.
  }
}

async function getItem(key: string): Promise<string | null> {
  if (Platform.OS === "web") {
    return readWebStorage(key);
  }

  if (typeof SecureStore.getItemAsync !== "function") {
    return null;
  }

  try {
    return await SecureStore.getItemAsync(key);
  } catch {
    return null;
  }
}

async function setItem(key: string, value: string): Promise<void> {
  if (Platform.OS === "web") {
    writeWebStorage(key, value);
    return;
  }

  if (typeof SecureStore.setItemAsync !== "function") {
    return;
  }

  try {
    await SecureStore.setItemAsync(key, value);
  } catch {
    // Ignore storage failures and let the session continue in memory.
  }
}

async function removeItem(key: string): Promise<void> {
  if (Platform.OS === "web") {
    removeWebStorage(key);
    return;
  }

  if (typeof SecureStore.deleteItemAsync !== "function") {
    return;
  }

  try {
    await SecureStore.deleteItemAsync(key);
  } catch {
    // Ignore storage failures and keep runtime usable.
  }
}

export async function hydrateSession(): Promise<SessionState> {
  try {
    const [storedUser, accessToken, refreshToken] = await Promise.all([
      getItem(SESSION_KEY),
      getItem(ACCESS_TOKEN_KEY),
      getItem(REFRESH_TOKEN_KEY)
    ]);

    if (!storedUser || !accessToken) {
      return guestState;
    }

    const user = JSON.parse(storedUser) as SessionUser;
    if (!user?.id || !user?.name || !user?.email) {
      await clearSession();
      return guestState;
    }

    return {
      isHydrating: false,
      isAuthenticated: true,
      displayName: user.name,
      user,
      accessToken,
      refreshToken
    };
  } catch {
    await clearSession();
    return guestState;
  }
}

export async function persistSession(
  user: SessionUser,
  accessToken: string,
  refreshToken?: string | null
): Promise<SessionState> {
  await Promise.all([
    setItem(SESSION_KEY, JSON.stringify(user)),
    setItem(ACCESS_TOKEN_KEY, accessToken),
    refreshToken ? setItem(REFRESH_TOKEN_KEY, refreshToken) : removeItem(REFRESH_TOKEN_KEY)
  ]);

  return {
    isHydrating: false,
    isAuthenticated: true,
    displayName: user.name,
    user,
    accessToken,
    refreshToken: refreshToken ?? null
  };
}

export async function persistTokens(accessToken: string, refreshToken?: string | null): Promise<void> {
  await setItem(ACCESS_TOKEN_KEY, accessToken);
  if (refreshToken) {
    await setItem(REFRESH_TOKEN_KEY, refreshToken);
  }
}

export async function clearSession(): Promise<SessionState> {
  await Promise.all([
    removeItem(SESSION_KEY),
    removeItem(ACCESS_TOKEN_KEY),
    removeItem(REFRESH_TOKEN_KEY)
  ]);
  return guestState;
}

export async function getStoredAccessToken(): Promise<string | null> {
  return getItem(ACCESS_TOKEN_KEY);
}

export async function getStoredRefreshToken(): Promise<string | null> {
  return getItem(REFRESH_TOKEN_KEY);
}
