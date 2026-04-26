import { apiClient } from "./apiClient";

export interface UserSession {
  id?: string;
  userId?: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  createdAt?: string;
  expiresAt?: string;
  lastActivityAt?: string;
  isActive?: boolean;
}

export async function getUserSessions(userId: string): Promise<UserSession[]> {
  return apiClient.get<UserSession[]>(`/Session/user/${encodeURIComponent(userId)}`);
}

export async function invalidateAllSessions(): Promise<void> {
  await apiClient.post("/Session/invalidate-all");
}
