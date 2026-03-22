import React, { useState } from 'react';
import ErrorBoundary from '@shared/components/ErrorBoundary';
import LoginScreen from '@shared/components/LoginScreen';
import BankingOSControlCenter from '@shared/components/BankingOSControlCenter';
import { useAuth } from '@shared/hooks/useApi';

function BankingOSShell({
  user,
  onLogout
}: {
  user: { name: string; email: string; role: string };
  onLogout: () => Promise<void>;
}) {
  return (
    <div className="min-h-screen bg-[linear-gradient(180deg,#eef4fb,#f8fbff_42%,#ffffff)] text-slate-950">
      <header className="border-b border-slate-200/80 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-6 py-5">
          <div>
            <div className="text-[11px] font-semibold uppercase tracking-[0.28em] text-sky-700">BankingOS</div>
            <h1 className="mt-1 text-2xl font-bold tracking-[-0.03em] text-slate-950">
              Standalone Core Banking Platform
            </h1>
            <p className="mt-1 text-sm text-slate-600">
              Platform shell focused on BankingOS process packs, low-code forms, themes, and governed releases.
            </p>
          </div>
          <div className="flex items-center gap-4 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
            <div className="text-right">
              <div className="text-sm font-semibold text-slate-900">{user.name}</div>
              <div className="text-xs uppercase tracking-[0.14em] text-slate-500">
                {user.role} | {user.email}
              </div>
            </div>
            <button
              type="button"
              onClick={() => void onLogout()}
              className="rounded-xl bg-slate-950 px-4 py-2 text-sm font-medium text-white transition hover:bg-slate-800"
            >
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-7xl px-4 py-6 sm:px-6">
        <div className="mb-6 grid gap-4 md:grid-cols-3">
          <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Platform</div>
            <div className="mt-3 text-lg font-semibold text-slate-950">Metadata-driven banking operations</div>
            <p className="mt-2 text-sm text-slate-600">
              BankingOS runs as its own shell instead of appearing as a submenu inside BankInsight.
            </p>
          </div>
          <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Config Studio</div>
            <div className="mt-3 text-lg font-semibold text-slate-950">Forms, themes, and releases</div>
            <p className="mt-2 text-sm text-slate-600">
              Low-code form versions, BankingOS themes, and publish bundles stay grouped under one shell.
            </p>
          </div>
          <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Runtime</div>
            <div className="mt-3 text-lg font-semibold text-slate-950">Executable banking process workspace</div>
            <p className="mt-2 text-sm text-slate-600">
              Launch, claim, approve, and complete BankingOS processes without leaving the platform.
            </p>
          </div>
        </div>

        <BankingOSControlCenter />
      </main>
    </div>
  );
}

function BankingOSAppContent() {
  const { user, isAuthenticated, isLoading, isAuthenticating, login, verifyMfa, logout } = useAuth();
  const [error, setError] = useState<string | null>(null);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-950 text-white">
        <div className="text-center">
          <div className="mx-auto mb-4 h-12 w-12 animate-spin rounded-full border-b-2 border-white" />
          <p>Loading BankingOS...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return (
      <LoginScreen
        onLogin={async (email, password) => {
          try {
            setError(null);
            return await login(email, password);
          } catch (loginError) {
            setError((loginError as Error).message);
            throw loginError;
          }
        }}
        onVerifyMfa={async (mfaToken, code) => {
          try {
            setError(null);
            return await verifyMfa(mfaToken, code);
          } catch (mfaError) {
            setError((mfaError as Error).message);
            throw mfaError;
          }
        }}
        loading={isAuthenticating}
        error={error}
      />
    );
  }

  return <BankingOSShell user={user} onLogout={logout} />;
}

export default function BankingOSApp() {
  return (
    <ErrorBoundary>
      <BankingOSAppContent />
    </ErrorBoundary>
  );
}
