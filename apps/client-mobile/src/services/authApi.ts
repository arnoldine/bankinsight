import { apiClient } from "./apiClient";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserProfile {
  id: string;
  customerId?: string;
  name: string;
  email: string;
  role?: string;
  permissions?: string[];
  hasTransactionPin?: boolean;
}

export interface LoginResponse {
  token?: string | null;
  refreshToken?: string | null;
  user?: UserProfile;
  mfaRequired?: boolean;
  mfaToken?: string | null;
  deliveryChannel?: string | null;
  deliveryHint?: string | null;
  deliveryStatus?: string | null;
  deliveryMessage?: string | null;
  mfaExpiresAtUtc?: string | null;
  allowedFactors?: string[];
  debugCode?: string | null;
}

export interface VerifyMfaRequest {
  mfaToken: string;
  code: string;
}

export interface VerificationChallengeResponse {
  challengeRequired: boolean;
  challengeToken: string;
  deliveryChannel: string;
  deliveryHint: string;
  deliveryStatus: string;
  deliveryMessage: string;
  expiresAtUtc: string;
  debugCode?: string | null;
  factor: "otp" | "pin";
  allowedFactors?: string[];
}

export interface RegisterRequest {
  name: string;
  email: string;
  phone: string;
  digitalAddress?: string;
  ghanaCard?: string;
  password: string;
}

export interface VerifyRegistrationRequest {
  registrationToken: string;
  code: string;
}

export interface PasswordResetStartRequest {
  email: string;
}

export interface PasswordResetStartResponse {
  accepted: boolean;
  resetToken?: string | null;
  deliveryHint?: string | null;
  deliveryChannel?: string | null;
  deliveryStatus?: string | null;
  deliveryMessage?: string | null;
  expiresAtUtc?: string | null;
  debugCode?: string | null;
}

export interface PasswordResetCompleteRequest {
  resetToken: string;
  code: string;
  newPassword: string;
}

export interface StepUpStartRequest {
  purpose: string;
  factor?: "otp" | "pin";
}

export interface StepUpVerifyRequest {
  challengeToken: string;
  code: string;
}

export interface StepUpVerifiedResponse {
  stepUpToken: string;
  purpose: string;
  expiresAtUtc: string;
  factor: "otp" | "pin";
}

export interface SetTransactionPinRequest {
  password: string;
  pin: string;
}

export async function login(payload: LoginRequest): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>("/client-auth/login", payload);
}

export async function verifyMfa(payload: VerifyMfaRequest): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>("/client-auth/mfa/verify", payload);
}

export async function resendMfa(mfaToken: string): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>("/client-auth/mfa/resend", { mfaToken });
}

export async function register(payload: RegisterRequest): Promise<VerificationChallengeResponse> {
  return apiClient.post<VerificationChallengeResponse>("/client-auth/register", payload);
}

export async function verifyRegistration(payload: VerifyRegistrationRequest): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>("/client-auth/register/verify", payload);
}

export async function startPasswordReset(payload: PasswordResetStartRequest): Promise<PasswordResetStartResponse> {
  return apiClient.post<PasswordResetStartResponse>("/client-auth/password/forgot", payload);
}

export async function completePasswordReset(payload: PasswordResetCompleteRequest): Promise<{ success: boolean; message: string }> {
  return apiClient.post<{ success: boolean; message: string }>("/client-auth/password/reset", payload);
}

export async function initiateStepUp(payload: StepUpStartRequest): Promise<VerificationChallengeResponse> {
  return apiClient.post<VerificationChallengeResponse>("/client-auth/step-up/initiate", payload);
}

export async function verifyStepUp(payload: StepUpVerifyRequest): Promise<StepUpVerifiedResponse> {
  return apiClient.post<StepUpVerifiedResponse>("/client-auth/step-up/verify", payload);
}

export async function setTransactionPin(payload: SetTransactionPinRequest): Promise<{ success: boolean; message: string }> {
  return apiClient.post<{ success: boolean; message: string }>("/client-auth/transaction-pin", payload);
}

export async function getCurrentUser(): Promise<UserProfile> {
  return apiClient.get<UserProfile>("/client-auth/me");
}

export async function logout(): Promise<void> {
  await apiClient.post("/client-auth/logout");
}
