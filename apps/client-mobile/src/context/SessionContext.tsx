import { createContext, useContext, useEffect, useState } from "react";
import type { PropsWithChildren } from "react";
import {
  clearSession,
  hydrateSession,
  persistSession,
  type SessionState
} from "../services/sessionStorage";
import {
  getCurrentUser,
  login as loginRequest,
  logout as logoutRequest,
  resendMfa as resendMfaRequest,
  verifyMfa as verifyMfaRequest
} from "../services/authApi";
import { ApiError } from "../services/apiClient";
import { appConfig } from "../config";

interface SessionContextValue extends SessionState {
  isSubmitting: boolean;
  errorMessage: string | null;
  mfaChallenge: {
    token: string;
    deliveryHint?: string | null;
    expiresAtUtc?: string | null;
    debugCode?: string | null;
  } | null;
  signIn: (payload: { email: string; password: string }) => Promise<void>;
  verifyMfa: (code: string) => Promise<void>;
  resendMfa: () => Promise<void>;
  signOut: () => Promise<void>;
  refreshCurrentUser: () => Promise<void>;
  clearError: () => void;
}

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

export function SessionProvider({ children }: PropsWithChildren) {
  const [state, setState] = useState<SessionState>({
    isHydrating: true,
    isAuthenticated: false,
    displayName: "Guest",
    user: null,
    accessToken: null,
    refreshToken: null
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [mfaChallenge, setMfaChallenge] = useState<SessionContextValue["mfaChallenge"]>(null);

  useEffect(() => {
    let cancelled = false;

    hydrateSession().then((nextState) => {
      if (!cancelled) {
        setState(nextState);
      }
    });

    return () => {
      cancelled = true;
    };
  }, []);

  async function signIn(payload: { email: string; password: string }) {
    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const response = await loginRequest(payload);

      if (response.mfaRequired && response.mfaToken) {
        setMfaChallenge({
          token: response.mfaToken,
          deliveryHint: response.deliveryHint,
          expiresAtUtc: response.mfaExpiresAtUtc,
          debugCode: appConfig.showDevOtp ? response.debugCode ?? null : null
        });
        return;
      }

      if (!response.token) {
        throw new ApiError(401, "The login response did not include a usable session token.");
      }

      const user = response.user ?? (await getCurrentUser());
      const nextState = await persistSession(user, response.token, response.refreshToken);
      setMfaChallenge(null);
      setState(nextState);
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Unable to sign in.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function verifyMfa(code: string) {
    if (!mfaChallenge?.token) {
      setErrorMessage("Your verification session has expired. Please sign in again.");
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const response = await verifyMfaRequest({ mfaToken: mfaChallenge.token, code });
      if (!response.token) {
        throw new ApiError(401, "The verification response did not include a usable session token.");
      }

      const user = response.user ?? (await getCurrentUser());
      const nextState = await persistSession(user, response.token, response.refreshToken);
      setMfaChallenge(null);
      setState(nextState);
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Unable to verify the code.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function resendMfa() {
    if (!mfaChallenge?.token) {
      setErrorMessage("Your verification session has expired. Please sign in again.");
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const response = await resendMfaRequest(mfaChallenge.token);
      setMfaChallenge((current) =>
        current
          ? {
              ...current,
              deliveryHint: response.deliveryHint ?? current.deliveryHint,
              expiresAtUtc: response.mfaExpiresAtUtc ?? current.expiresAtUtc,
              debugCode: appConfig.showDevOtp
                ? response.debugCode ?? current.debugCode
                : null
            }
          : current
      );
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Unable to resend the code.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function signOut() {
    setIsSubmitting(true);
    try {
      await logoutRequest();
    } catch {
      // Clear local session even if the API logout fails.
    } finally {
      const nextState = await clearSession();
      setMfaChallenge(null);
      setErrorMessage(null);
      setState(nextState);
      setIsSubmitting(false);
    }
  }

  async function refreshCurrentUser() {
    if (!state.accessToken || !state.user) {
      return;
    }

    try {
      const user = await getCurrentUser();
      const nextState = await persistSession(user, state.accessToken, state.refreshToken);
      setState(nextState);
    } catch {
      // Keep the current session if profile refresh fails.
    }
  }

  return (
    <SessionContext.Provider
      value={{
        ...state,
        isSubmitting,
        errorMessage,
        mfaChallenge,
        signIn,
        verifyMfa,
        resendMfa,
        signOut,
        refreshCurrentUser,
        clearError: () => setErrorMessage(null)
      }}
    >
      {children}
    </SessionContext.Provider>
  );
}

export function useSession() {
  const context = useContext(SessionContext);

  if (!context) {
    throw new Error("useSession must be used inside SessionProvider");
  }

  return context;
}
