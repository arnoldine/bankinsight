import { appConfig } from "../config";

export interface ApiRequestOptions extends RequestInit {
  requiresStepUp?: boolean;
}

export async function apiRequest<T>(
  path: string,
  options: ApiRequestOptions = {}
): Promise<T> {
  const response = await fetch(`${appConfig.apiBaseUrl}${path}`, {
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...options.headers
    },
    ...options
  });

  if (!response.ok) {
    throw new Error(`Client channel request failed for ${path}`);
  }

  return (await response.json()) as T;
}
