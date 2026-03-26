import { API_CONFIG } from './apiConfig';

export class ApiError extends Error {
  private static extractValidationMessage(data?: any): string | null {
    const errors = data?.errors;
    if (!errors || typeof errors !== 'object') {
      return null;
    }

    const messages = Object.values(errors)
      .flatMap((value) => Array.isArray(value) ? value : [value])
      .filter((value): value is string => typeof value === 'string' && value.trim().length > 0);

    return messages[0] || null;
  }

  private static resolveMessage(status: number, data?: any): string {
    const explicitMessage =
      (typeof data?.message === 'string' && data.message.trim()) ||
      (typeof data?.error === 'string' && data.error.trim()) ||
      ApiError.extractValidationMessage(data);

    if (explicitMessage) {
      return explicitMessage;
    }

    switch (status) {
      case 0:
        return 'Network error. Please check your connection and try again.';
      case 400:
        return 'The request could not be processed. Please review your input and try again.';
      case 401:
        return 'Your credentials or verification code are invalid or expired.';
      case 403:
        return 'You do not have permission to perform this action.';
      case 404:
        return 'The requested resource could not be found.';
      case 408:
        return 'The request timed out. Please try again.';
      case 500:
        return 'The server encountered an unexpected error. Please try again shortly.';
      default:
        return `Request failed (${status}). Please try again.`;
    }
  }

  constructor(
    public status: number,
    public data?: any
  ) {
    super(ApiError.resolveMessage(status, data));
    this.name = 'ApiError';
  }
}

export interface ApiResponse<T> {
  data?: T;
  message?: string;
  error?: string;
  errorCode?: string;
  traceId?: string;
  timestamp?: string;
}

export interface BlobDownloadResult {
  blob: Blob;
  fileName?: string | null;
  contentType?: string | null;
}

// Store Clerk getToken function globally so it can be used in httpClient
let clerkGetToken: (() => Promise<string | null>) | null = null;

export function setClerkGetToken(getTokenFn: (() => Promise<string | null>) | null) {
  clerkGetToken = getTokenFn;
}

const getFileNameFromDisposition = (contentDisposition: string | null): string | null => {
  if (!contentDisposition) {
    return null;
  }

  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]).replace(/"/g, '');
  }

  const asciiMatch = contentDisposition.match(/filename="?([^";]+)"?/i);
  return asciiMatch?.[1] ?? null;
};

class HttpClient {
  private baseUrl: string;
  private timeout: number;

  constructor() {
    this.baseUrl = API_CONFIG.baseUrl;
    this.timeout = API_CONFIG.timeout;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;

    // Use the active auth mode's token source. Clerk is opt-in, so legacy JWT remains the default.
    let token = localStorage.getItem('auth_token');

    if (!token && clerkGetToken) {
      try {
        token = await clerkGetToken();
      } catch (error) {
        console.warn('Failed to get Clerk token:', error);
      }
    }

    const defaultHeaders = { ...API_CONFIG.headers } as Record<string, string>;
    if (options.body instanceof FormData) {
      delete defaultHeaders['Content-Type'];
    }

    const headers: HeadersInit = {
      ...defaultHeaders,
      ...options.headers,
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(url, {
        ...options,
        headers,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new ApiError(response.status, errorData);
      }

      if (response.headers.get('content-length') === '0' || response.status === 204) {
        return {} as T;
      }

      const contentType = response.headers.get('content-type');
      if (contentType?.includes('application/json')) {
        return await response.json() as T;
      }

      return await response.text() as unknown as T;
    } catch (error) {
      clearTimeout(timeoutId);

      if (error instanceof TypeError && error.message === 'Failed to fetch') {
        throw new ApiError(0, { message: 'Network error - unable to connect to server' });
      }

      if (error instanceof DOMException && error.name === 'AbortError') {
        throw new ApiError(408, { message: 'Request timeout' });
      }

      throw error;
    }
  }

  async get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' });
  }

  async post<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async postForm<T>(endpoint: string, data: FormData): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data,
    });
  }

  async put<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }

  async patch<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PATCH',
      body: data ? JSON.stringify(data) : undefined,
    });
  }
  async downloadBlob(endpoint: string, method: 'GET' | 'POST' = 'GET', data?: any): Promise<BlobDownloadResult> {
    const url = `${this.baseUrl}${endpoint}`;
    let token = localStorage.getItem('auth_token');
    if (!token && clerkGetToken) {
      try {
        token = await clerkGetToken();
      } catch (error) {
        console.warn('Failed to get Clerk token:', error);
      }
    }

    const headers: HeadersInit = {
      ...API_CONFIG.headers,
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(url, {
        method,
        headers,
        body: data ? JSON.stringify(data) : undefined,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new ApiError(response.status, errorData);
      }

      return {
        blob: await response.blob(),
        fileName: getFileNameFromDisposition(response.headers.get('content-disposition')),
        contentType: response.headers.get('content-type'),
      };
    } catch (error) {
      clearTimeout(timeoutId);

      if (error instanceof TypeError && error.message === 'Failed to fetch') {
        throw new ApiError(0, { message: 'Network error - unable to connect to server' });
      }

      if (error instanceof DOMException && error.name === 'AbortError') {
        throw new ApiError(408, { message: 'Request timeout' });
      }

      throw error;
    }
  }
}

export const httpClient = new HttpClient();



