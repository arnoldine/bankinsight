import { appConfig } from "../config";
import { getStoredAccessToken, getStoredRefreshToken, persistTokens, clearSession } from "./sessionStorage";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public data?: unknown
  ) {
    super(message);
    this.name = "ApiError";
  }
}

function resolveErrorMessage(status: number, data: any): string {
  if (typeof data?.message === "string" && data.message.trim()) {
    return data.message;
  }

  switch (status) {
    case 0:
      return "Unable to reach the BankInsight API. Check the API URL and network access.";
    case 401:
      return "Your session is invalid or has expired.";
    case 403:
      return "This account does not currently have permission to access this feature.";
    case 404:
      return "The requested resource was not found.";
    case 408:
      return "The request timed out. Please try again.";
    default:
      return `Request failed with status ${status}.`;
  }
}

class ApiClient {
  private async performRequest<T>(
    path: string,
    init: RequestInit,
    allowRefresh: boolean
  ): Promise<T> {
    const accessToken = await getStoredAccessToken();
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
      ...(init.headers as Record<string, string> | undefined)
    };

    if (accessToken) {
      headers.Authorization = `Bearer ${accessToken}`;
    }

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), appConfig.requestTimeoutMs);

    try {
      const response = await fetch(`${appConfig.apiBaseUrl}${path}`, {
        ...init,
        headers,
        signal: controller.signal
      });

      if (response.status === 401 && allowRefresh && path !== "/auth/refresh") {
        const refreshed = await this.tryRefresh();
        if (refreshed) {
          return this.performRequest<T>(path, init, false);
        }
      }

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new ApiError(response.status, resolveErrorMessage(response.status, errorData), errorData);
      }

      if (response.status === 204) {
        return {} as T;
      }

      const contentType = response.headers.get("content-type") ?? "";
      if (contentType.includes("application/json")) {
        return (await response.json()) as T;
      }

      return (await response.text()) as unknown as T;
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }

      if (error instanceof DOMException && error.name === "AbortError") {
        throw new ApiError(408, resolveErrorMessage(408, null));
      }

      throw new ApiError(0, resolveErrorMessage(0, null), error);
    } finally {
      clearTimeout(timeoutId);
    }
  }

  private async tryRefresh(): Promise<boolean> {
    const refreshToken = await getStoredRefreshToken();
    if (!refreshToken) {
      await clearSession();
      return false;
    }

    try {
      const refreshed = await this.performRequest<{ token: string; refreshToken?: string | null }>(
        "/client-auth/refresh",
        {
          method: "POST",
          body: JSON.stringify({ refreshToken })
        },
        false
      );

      await persistTokens(refreshed.token, refreshed.refreshToken ?? refreshToken);
      return true;
    } catch {
      await clearSession();
      return false;
    }
  }

  async get<T>(path: string): Promise<T> {
    return this.performRequest<T>(path, { method: "GET" }, true);
  }

  async post<T>(path: string, body?: unknown): Promise<T> {
    return this.performRequest<T>(
      path,
      { method: "POST", body: body ? JSON.stringify(body) : undefined },
      true
    );
  }

  async put<T>(path: string, body?: unknown): Promise<T> {
    return this.performRequest<T>(
      path,
      { method: "PUT", body: body ? JSON.stringify(body) : undefined },
      true
    );
  }
}

export const apiClient = new ApiClient();

export function resolveApiUrl(path: string | null | undefined): string | undefined {
  if (!path) {
    return undefined;
  }

  if (/^https?:\/\//i.test(path) || path.startsWith("data:")) {
    return path;
  }

  try {
    return new URL(path, `${appConfig.apiBaseUrl}/`).toString();
  } catch {
    return path;
  }
}
