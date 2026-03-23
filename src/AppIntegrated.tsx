/// <reference types="vite/client" />
import React, { useEffect, useState } from 'react';
import { ClerkProvider, useAuth as useClerkAuth, useUser as useClerkUser } from '@clerk/react';
import { useAuth } from './hooks/useApi';
import LoginScreen from './components/LoginScreen';
import EnhancedDashboardLayout from './components/EnhancedDashboardLayout';
import ErrorBoundary from './components/ErrorBoundary';
import { API_CONFIG } from './services/apiConfig';
import { setClerkGetToken } from './services/httpClient';
import { authService, LoginResponse } from './services/authService';

// Clerk publishable key from environment
const clerkPublishableKey = import.meta.env.VITE_CLERK_PUBLISHABLE_KEY || '';
const enableClerkAuth = (import.meta.env.VITE_ENABLE_CLERK_AUTH || 'false') === 'true' && !!clerkPublishableKey;

if (!clerkPublishableKey) {
  if ((import.meta.env.VITE_ENABLE_CLERK_AUTH || 'false') === 'true') {
    console.warn('VITE_CLERK_PUBLISHABLE_KEY environment variable is not set');
  }
}

/**
 * Inner app content component that uses Clerk authentication
 */
function AppContent() {
  // Only use Clerk hooks if Clerk is enabled and we have a publishable key
  const clerkAuth = enableClerkAuth ? useClerkAuth() : { isLoaded: true, isSignedIn: false, getToken: null };
  const clerkUserHook = enableClerkAuth ? useClerkUser() : { user: null };

  const { isLoaded, isSignedIn, getToken } = clerkAuth;
  const { user: clerkUser } = clerkUserHook;
  const { user, isAuthenticated, isLoading, isAuthenticating, login, verifyMfa, logout } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [syncing, setSyncing] = useState(false);
  const [authMode, setAuthMode] = useState<'legacy' | 'clerk'>('legacy');

  // Only let httpClient use Clerk tokens while the app is actively in Clerk mode.
  useEffect(() => {
    if (enableClerkAuth && authMode === 'clerk' && isLoaded && isSignedIn && getToken) {
      setClerkGetToken(getToken);
      return;
    }

    setClerkGetToken(null);
  }, [authMode, isLoaded, isSignedIn, getToken]);

  // Sync Clerk user with BankInsight backend when signed in
  useEffect(() => {
    if (enableClerkAuth && authMode === 'clerk' && isSignedIn && clerkUser && !syncing) {
      syncClerkUserWithBackend();
    }
  }, [authMode, isSignedIn, clerkUser, syncing]);

  useEffect(() => {
    if (authMode === 'clerk' && enableClerkAuth && isLoaded && !isSignedIn) {
      // Just fall through to rendering the sign in state
    }
  }, [authMode, isLoaded, isSignedIn]);

  const handleLogin = async (email: string, password: string): Promise<LoginResponse> => {
    try {
      setError(null);
      return await login(email, password);
    } catch (err) {
      setError((err as Error).message);
      throw err;
    }
  };

  const handleVerifyMfa = async (mfaToken: string, code: string): Promise<LoginResponse> => {
    try {
      setError(null);
      return await verifyMfa(mfaToken, code);
    } catch (err) {
      setError((err as Error).message);
      throw err;
    }
  };

  const syncClerkUserWithBackend = async () => {
    if (!getToken || !clerkUser) {
      return;
    }

    setSyncing(true);
    try {
      const response = await fetch(
        `${API_CONFIG.baseUrl}/clerk/sync`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${await getToken()}`
          },
          body: JSON.stringify({
            clerkUserId: clerkUser.id,
            email: clerkUser.emailAddresses[0]?.emailAddress,
            firstName: clerkUser.firstName,
            lastName: clerkUser.lastName
          })
        }
      );

      if (!response.ok) {
        throw new Error('Failed to sync user with backend');
      }
    } catch (err) {
      console.error('User sync error:', err);
      setError((err as Error).message);
    } finally {
      setSyncing(false);
    }
  };

  const handleLogout = async () => {
    try {
      setError(null);
      await logout();
      setAuthMode('legacy');
      setClerkGetToken(null);
    } catch (err) {
      setError((err as Error).message);
    }
  };

  // Legacy auth flow (default)
  if (!enableClerkAuth || authMode === 'legacy') {
    if (isLoading) {
      return (
        <div className="flex items-center justify-center min-h-screen bg-slate-100">
          <div className="text-white">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white mx-auto mb-4"></div>
            <p>Loading...</p>
          </div>
        </div>
      );
    }

    if (!isAuthenticated) {
      return (
        <LoginScreen
          onLogin={handleLogin}
          onVerifyMfa={handleVerifyMfa}
          loading={isAuthenticating}
          error={error}
          onUseClerk={enableClerkAuth ? () => setAuthMode('clerk') : undefined}
        />
      );
    }

    return (
      <EnhancedDashboardLayout
        user={user!}
        onLogout={handleLogout}
        error={error}
        onErrorDismiss={() => setError(null)}
      />
    );
  }

  // Clerk auth flow (opt-in)
  if (!isLoaded || isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-slate-100">
        <div className="text-white">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white mx-auto mb-4"></div>
          <p>Loading...</p>
        </div>
      </div>
    );
  }

  // Show Clerk sign-in if not authenticated with Clerk
  if (!isSignedIn) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-slate-100">
        <div className="text-white text-center">
          <h2 className="text-2xl font-bold mb-4">Sign In Required</h2>
          <p className="mb-4">Please sign in with your Clerk account to continue.</p>
          <button
            type="button"
            onClick={() => setAuthMode('legacy')}
            className="px-4 py-2 bg-slate-700 hover:bg-slate-600 rounded"
          >
            Back to password login
          </button>
        </div>
      </div>
    );
  }

  // Show dashboard if authenticated
  if (isAuthenticated && user) {
    return (
      <EnhancedDashboardLayout
        user={user}
        onLogout={handleLogout}
        error={error}
        onErrorDismiss={() => setError(null)}
      />
    );
  }

  // Show error state
  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(23,150,128,0.28),transparent_24%),radial-gradient(circle_at_top_right,rgba(245,158,11,0.22),transparent_18%),linear-gradient(180deg,#07131a,#0b1b22)]">
        <div className="text-white text-center">
          <h2 className="text-xl font-bold mb-4">Error</h2>
          <p className="mb-4">{error}</p>
          <button
            onClick={() => setError(null)}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded"
          >
            Dismiss
          </button>
        </div>
      </div>
    );
  }

  // While syncing
  if (syncing) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(23,150,128,0.28),transparent_24%),radial-gradient(circle_at_top_right,rgba(245,158,11,0.22),transparent_18%),linear-gradient(180deg,#07131a,#0b1b22)]">
        <div className="text-white">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white mx-auto mb-4"></div>
          <p>Initializing...</p>
        </div>
      </div>
    );
  }

  return null;
}

/**
 * Clerk-wrapped App component
 */
export default function App() {
  // Only enable Clerk when it is explicitly turned on.
  if (enableClerkAuth) {
    return (
      <ClerkProvider publishableKey={clerkPublishableKey}>
        <ErrorBoundary>
          <AppContent />
        </ErrorBoundary>
      </ClerkProvider>
    );
  }

  // Fallback if no Clerk key is configured (Clerk disabled)
  return (
    <ErrorBoundary>
      <AppContent />
    </ErrorBoundary>
  );
}
