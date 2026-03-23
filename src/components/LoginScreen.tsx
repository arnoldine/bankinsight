import React, { useState } from 'react';
import { AlertCircle, ArrowRight, CheckCircle2, Loader2, Lock, Mail, ShieldCheck } from 'lucide-react';
import { LoginResponse } from '../services/authService';

export interface LoginProps {
  onLogin: (email: string, password: string) => Promise<LoginResponse>;
  onVerifyMfa?: (mfaToken: string, code: string) => Promise<LoginResponse>;
  onResendMfa?: (mfaToken: string) => Promise<LoginResponse>;
  loading?: boolean;
  error?: string | null;
  onErrorDismiss?: () => void;
  onUseClerk?: () => void;
}

export default function LoginScreen({
  onLogin,
  onVerifyMfa,
  onResendMfa,
  loading = false,
  error,
  onErrorDismiss,
  onUseClerk,
}: LoginProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [mfaCode, setMfaCode] = useState('');
  const [mfaToken, setMfaToken] = useState<string | null>(null);
  const [mfaHint, setMfaHint] = useState<string | null>(null);
  const [mfaExpiry, setMfaExpiry] = useState<string | null>(null);
  const [mfaDebugCode, setMfaDebugCode] = useState<string | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [mfaMessage, setMfaMessage] = useState<string | null>(null);
  const [resendMessage, setResendMessage] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setValidationError(null);
    setMfaMessage(null);
    setResendMessage(null);

    if (mfaToken) {
      if (!mfaCode || mfaCode.trim().length < 4) {
        setValidationError('Enter the one-time verification code.');
        return;
      }

      if (!onVerifyMfa) {
        setValidationError('MFA verification is not available.');
        return;
      }

      try {
        await onVerifyMfa(mfaToken, mfaCode.trim());
      } catch {
        // Parent handles API errors.
      }
      return;
    }

    if (!email) {
      setValidationError('Email is required.');
      return;
    }

    if (!password) {
      setValidationError('Password is required.');
      return;
    }

    if (!email.includes('@')) {
      setValidationError('Use a valid work email address.');
      return;
    }

    try {
      const response = await onLogin(email, password);
      if (response.mfaRequired && response.mfaToken) {
        setMfaToken(response.mfaToken);
        setMfaHint(response.deliveryHint || null);
        setMfaExpiry(response.mfaExpiresAtUtc || null);
        setMfaDebugCode(response.debugCode || null);
        setMfaCode('');
        setMfaMessage(
          response.deliveryMessage ||
          `Enter the one-time code sent to ${response.deliveryHint || 'your registered channel'}.`
        );
      }
    } catch {
      // Parent handles API errors.
    }
  };

  const errorMessage = error || validationError;
  const mfaExpiryLabel = mfaExpiry
    ? new Date(mfaExpiry).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    : null;

  return (
    <div className="simple-screen min-h-screen bg-slate-100 px-4 py-8 text-slate-900">
      <div className="mx-auto flex min-h-[calc(100vh-4rem)] max-w-md items-center justify-center">
        <section className="w-full rounded-lg border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
          <div className="text-center">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-[rgba(25,57,79,.08)] text-[#19394f]">
              {mfaToken ? <ShieldCheck className="h-6 w-6" /> : <Lock className="h-6 w-6" />}
            </div>
            <p className="mt-4 text-xs font-semibold text-slate-500">
              BankInsight
            </p>
            <h1 className="mt-3 text-[1.85rem] font-semibold text-slate-900">
              {mfaToken ? 'Verify your sign-in' : 'Admin sign in'}
            </h1>
            <p className="mt-3 text-sm leading-6 text-slate-600">
              {mfaToken
                ? `Complete the sign-in with the code sent to ${mfaHint || 'your registered contact point'}.`
                : 'Access the core banking system for operations, compliance, treasury, lending, and reporting.'}
            </p>
          </div>

          {errorMessage ? (
            <div className="mt-6 flex items-start gap-3 rounded-xl border border-[#f2dad6] bg-[#fbf0ef] px-4 py-3 text-sm text-[#8f4439]">
              <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0" />
              <div className="flex-1 leading-6">{errorMessage}</div>
              <button
                type="button"
                onClick={onErrorDismiss || (() => setValidationError(null))}
                className="text-xs font-semibold uppercase tracking-[0.14em] text-[#8f4439]/80"
              >
                Close
              </button>
            </div>
          ) : null}

          {mfaToken && (mfaMessage || resendMessage) ? (
            <div className="mt-6 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-4 text-sm text-emerald-900">
              <div className="flex items-start gap-3">
                <CheckCircle2 className="mt-0.5 h-4 w-4 flex-shrink-0 text-emerald-600" />
                <div className="space-y-2 leading-6">
                  <p className="font-medium">{resendMessage || mfaMessage}</p>
                  <ul className="list-disc space-y-1 pl-5 text-emerald-800/90">
                    <li>Check your inbox, spam, and promotions folders.</li>
                    <li>Use the most recent 6-digit code only.</li>
                    {mfaExpiryLabel ? <li>The current code expires around {mfaExpiryLabel}.</li> : null}
                  </ul>
                </div>
              </div>
            </div>
          ) : null}

          <form onSubmit={handleSubmit} className="mt-7 space-y-4">
            {!mfaToken ? (
              <>
                <label className="block text-sm font-semibold text-[#19394f]">
                  Work email
                  <div className="mt-2 flex items-center gap-3 rounded-xl border border-[rgba(15,46,67,.21)] bg-white px-4 py-3">
                    <Mail className="h-4 w-4 text-[#2b808c]" />
                    <input
                      type="email"
                      value={email}
                      onChange={(event) => setEmail(event.target.value)}
                      placeholder="admin@bankinsight.local"
                      disabled={loading}
                      className="w-full bg-transparent text-[15px] text-slate-900 placeholder:text-slate-400 focus:outline-none"
                    />
                  </div>
                </label>

                <label className="block text-sm font-semibold text-[#19394f]">
                  Password
                  <div className="mt-2 flex items-center gap-3 rounded-xl border border-[rgba(15,46,67,.21)] bg-white px-4 py-3">
                    <Lock className="h-4 w-4 text-[#2b808c]" />
                    <input
                      type="password"
                      value={password}
                      onChange={(event) => setPassword(event.target.value)}
                      placeholder="Enter your password"
                      disabled={loading}
                      className="w-full bg-transparent text-[15px] text-slate-900 placeholder:text-slate-400 focus:outline-none"
                    />
                  </div>
                </label>
              </>
            ) : (
              <label className="block text-sm font-semibold text-[#19394f]">
                One-time passcode
                <div className="mt-2 flex items-center gap-3 rounded-xl border border-[rgba(15,46,67,.21)] bg-white px-4 py-3">
                  <ShieldCheck className="h-4 w-4 text-[#2b808c]" />
                  <input
                    type="text"
                    inputMode="numeric"
                    value={mfaCode}
                    onChange={(event) => setMfaCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
                    placeholder="Enter verification code"
                    disabled={loading}
                    className="w-full bg-transparent text-[15px] text-slate-900 placeholder:text-slate-400 focus:outline-none"
                  />
                </div>
                {mfaDebugCode ? (
                  <p className="mt-2 text-xs font-semibold uppercase tracking-[0.14em] text-[#b54024]">
                    Local dev code: {mfaDebugCode}
                  </p>
                ) : null}
              </label>
            )}

            <button
              type="submit"
              disabled={loading}
              className="screen-button-primary inline-flex w-full items-center justify-center gap-2 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <ArrowRight className="h-4 w-4" />}
              {loading ? 'Processing...' : mfaToken ? 'Verify and continue' : 'Login'}
            </button>

            {mfaToken ? (
              <div className="space-y-3">
                <button
                  type="button"
                  onClick={async () => {
                    if (!mfaToken || !onResendMfa || loading) {
                      return;
                    }

                    setValidationError(null);
                    try {
                      const response = await onResendMfa(mfaToken);
                      setMfaHint(response.deliveryHint || mfaHint);
                      setMfaExpiry(response.mfaExpiresAtUtc || null);
                      setMfaDebugCode(response.debugCode || null);
                      setMfaCode('');
                      setResendMessage(
                        response.deliveryMessage ||
                        `A fresh verification code was sent to ${response.deliveryHint || mfaHint || 'your registered channel'}.`
                      );
                    } catch {
                      // Parent handles API errors.
                    }
                  }}
                  className="screen-button-secondary w-full"
                >
                  Resend code
                </button>

                <button
                  type="button"
                  onClick={() => {
                    setMfaToken(null);
                    setMfaHint(null);
                    setMfaExpiry(null);
                    setMfaDebugCode(null);
                    setMfaCode('');
                    setMfaMessage(null);
                    setResendMessage(null);
                    setValidationError(null);
                  }}
                  className="screen-button-secondary w-full"
                >
                  Back to credentials
                </button>
              </div>
            ) : null}

            {onUseClerk ? (
              <button
                type="button"
                onClick={onUseClerk}
                className="screen-button-secondary w-full"
              >
                Use Clerk sign-in
              </button>
            ) : null}
          </form>

          <div className="mt-6 border-t border-[rgba(15,46,67,.08)] pt-4 text-center text-xs uppercase tracking-[0.18em] text-slate-500">
            Secure access for regulated banking operations
          </div>
        </section>
      </div>
    </div>
  );
}
