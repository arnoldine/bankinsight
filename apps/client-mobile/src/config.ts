function resolveDefaultApiBaseUrl(): string {
  const location = globalThis.location;

  if (location?.protocol && location?.hostname) {
    return `${location.protocol}//${location.hostname}:5176/api`;
  }

  return "http://localhost:5176/api";
}

const defaultApiBaseUrl = resolveDefaultApiBaseUrl();
const rawApiBaseUrl = process.env.EXPO_PUBLIC_API_BASE_URL?.trim();

function normalizeApiBaseUrl(value: string | undefined): string {
  if (!value) {
    return defaultApiBaseUrl;
  }

  return value.endsWith("/") ? value.slice(0, -1) : value;
}

function readBooleanFlag(value: string | undefined, defaultValue = false): boolean {
  if (!value) {
    return defaultValue;
  }

  return value.trim().toLowerCase() === "true";
}

function shouldShowDevOtpByDefault(): boolean {
  const location = globalThis.location;
  const isLocalHost =
    location?.hostname === "localhost" ||
    location?.hostname === "127.0.0.1" ||
    location?.hostname === "::1";

  return process.env.NODE_ENV !== "production" && isLocalHost;
}

export const appConfig = {
  appName: "BankInsight Client",
  complianceTag: "BoG-aligned",
  privacyNoticeVersion: "draft-2026-04",
  sessionTimeoutMinutes: 15,
  requestTimeoutMs: 30000,
  apiBaseUrl: normalizeApiBaseUrl(rawApiBaseUrl),
  showDevOtp: readBooleanFlag(process.env.EXPO_PUBLIC_SHOW_DEV_OTP, shouldShowDevOtpByDefault())
} as const;
