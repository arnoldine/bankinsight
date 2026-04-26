import { useState } from "react";
import { Ionicons } from "@expo/vector-icons";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { appConfig } from "../config";
import { useSession } from "../context/SessionContext";
import { completePasswordReset, register as registerClient, startPasswordReset, verifyRegistration } from "../services/authApi";
import { colors, spacing, typography } from "../theme";

interface SignInScreenProps {
  mode: "idle" | "loading";
}

export function SignInScreen({ mode }: SignInScreenProps) {
  const { signIn, verifyMfa, resendMfa, isSubmitting, errorMessage, mfaChallenge } = useSession();
  const [view, setView] = useState<"signin" | "register" | "reset">("signin");
  const [email, setEmail] = useState("akosua.mensah@bankinsight.local");
  const [password, setPassword] = useState("ClientPass123!");
  const [code, setCode] = useState("");
  const [name, setName] = useState("Kwame Client");
  const [phone, setPhone] = useState("+233240000099");
  const [digitalAddress, setDigitalAddress] = useState("GA-999-0001");
  const [registrationToken, setRegistrationToken] = useState<string | null>(null);
  const [resetToken, setResetToken] = useState<string | null>(null);
  const [newPassword, setNewPassword] = useState("ClientPass456!");
  const [localFeedback, setLocalFeedback] = useState<string | null>(null);
  const [localSubmitting, setLocalSubmitting] = useState(false);

  const isLoading = mode === "loading" || isSubmitting || localSubmitting;
  const isMfaStep = Boolean(mfaChallenge);

  async function submitRegistration() {
    setLocalSubmitting(true);
    setLocalFeedback(null);
    try {
      const challenge = await registerClient({
        name: name.trim(),
        email: email.trim(),
        phone: phone.trim(),
        digitalAddress: digitalAddress.trim(),
        password
      });
      setRegistrationToken(challenge.challengeToken);
      setLocalFeedback(
        appConfig.showDevOtp && challenge.debugCode
          ? `Registration code sent to ${challenge.deliveryHint}. Dev code: ${challenge.debugCode}`
          : `Registration code sent to ${challenge.deliveryHint}.`
      );
    } catch (error) {
      setLocalFeedback(error instanceof Error ? error.message : "Unable to start registration.");
    } finally {
      setLocalSubmitting(false);
    }
  }

  async function submitRegistrationVerification() {
    if (!registrationToken) {
      setLocalFeedback("Start registration first.");
      return;
    }

    setLocalSubmitting(true);
    setLocalFeedback(null);
    try {
      const response = await verifyRegistration({ registrationToken, code: code.trim() });
      if (response.token) {
        setView("signin");
        setLocalFeedback("Registration verified. You can now continue in the signed-in experience.");
      }
    } catch (error) {
      setLocalFeedback(error instanceof Error ? error.message : "Unable to verify registration.");
    } finally {
      setLocalSubmitting(false);
    }
  }

  async function submitPasswordResetStart() {
    setLocalSubmitting(true);
    setLocalFeedback(null);
    try {
      const response = await startPasswordReset({ email: email.trim() });
      setResetToken(response.resetToken ?? null);
      setLocalFeedback(
        appConfig.showDevOtp && response.debugCode
          ? `${response.deliveryMessage ?? "Password reset initiated."} Dev code: ${response.debugCode}`
          : response.deliveryMessage ?? "Password reset initiated."
      );
    } catch (error) {
      setLocalFeedback(error instanceof Error ? error.message : "Unable to start password reset.");
    } finally {
      setLocalSubmitting(false);
    }
  }

  async function submitPasswordResetComplete() {
    if (!resetToken) {
      setLocalFeedback("Start password reset first.");
      return;
    }

    setLocalSubmitting(true);
    setLocalFeedback(null);
    try {
      const response = await completePasswordReset({ resetToken, code: code.trim(), newPassword });
      setPassword(newPassword);
      setView("signin");
      setResetToken(null);
      setCode("");
      setLocalFeedback(response.message);
    } catch (error) {
      setLocalFeedback(error instanceof Error ? error.message : "Unable to complete password reset.");
    } finally {
      setLocalSubmitting(false);
    }
  }

  return (
    <Screen>
      <Card title={isMfaStep ? "Verify sign-in" : "Client access"} description={isMfaStep ? "Confirm identity to continue." : "Secure sign-in, onboarding, and recovery."}>
        <View style={styles.hero}>
          <View style={styles.heroTopRow}>
            <View>
              <Text style={styles.eyebrow}>BankInsight client</Text>
              <Text style={styles.heroTitle}>{isMfaStep ? "Confirm this session" : "Secure digital banking"}</Text>
            </View>
            <View style={styles.heroIconWrap}>
              <Ionicons name={isMfaStep ? "shield-checkmark-outline" : "lock-closed-outline"} size={24} color={colors.inkInverse} />
            </View>
          </View>
          <Text style={styles.heroCopy}>{isMfaStep ? "Use the code sent to your registered factor." : "Balances, payments, statements, support, and approvals in one controlled workspace."}</Text>
          <View style={styles.heroStats}>
            <View style={styles.heroStatTile}>
              <Ionicons name="shield-outline" size={16} color={colors.copperSoft} />
              <Text style={styles.heroStatValue}>MFA</Text>
              <Text style={styles.heroStatLabel}>session gate</Text>
            </View>
            <View style={styles.heroStatTile}>
              <Ionicons name="time-outline" size={16} color={colors.copperSoft} />
              <Text style={styles.heroStatValue}>2 min</Text>
              <Text style={styles.heroStatLabel}>code window</Text>
            </View>
            <View style={styles.heroStatTile}>
              <Ionicons name="checkmark-done-outline" size={16} color={colors.copperSoft} />
              <Text style={styles.heroStatValue}>Client</Text>
              <Text style={styles.heroStatLabel}>verified flow</Text>
            </View>
          </View>
        </View>

        {!isMfaStep ? (
          <View style={styles.modeTabs}>
            <Pressable style={[styles.modeTab, view === "signin" && styles.modeTabActive]} onPress={() => setView("signin")}>
              <Text style={[styles.modeTabLabel, view === "signin" && styles.modeTabLabelActive]}>Sign in</Text>
            </Pressable>
            <Pressable style={[styles.modeTab, view === "register" && styles.modeTabActive]} onPress={() => setView("register")}>
              <Text style={[styles.modeTabLabel, view === "register" && styles.modeTabLabelActive]}>Register</Text>
            </Pressable>
            <Pressable style={[styles.modeTab, view === "reset" && styles.modeTabActive]} onPress={() => setView("reset")}>
              <Text style={[styles.modeTabLabel, view === "reset" && styles.modeTabLabelActive]}>Reset</Text>
            </Pressable>
          </View>
        ) : null}

        {isMfaStep ? (
          <View style={styles.stack}>
            <View style={styles.infoPanel}>
              <Text style={styles.infoEyebrow}>Verification</Text>
              <Text style={styles.infoTitle}>Enter the code sent to {mfaChallenge?.deliveryHint ?? "your registered factor"}</Text>
            </View>
            {appConfig.showDevOtp && mfaChallenge?.debugCode ? (
              <View style={styles.devCodePanel}>
                <Text style={styles.devCodeLabel}>Development OTP</Text>
                <Text style={styles.devCodeValue}>{mfaChallenge.debugCode}</Text>
                <Text style={styles.devCodeHint}>This appears only in the local development preview.</Text>
              </View>
            ) : null}
            <TextInput value={code} onChangeText={setCode} placeholder="Verification code" placeholderTextColor={colors.muted} keyboardType="number-pad" autoCapitalize="none" style={styles.input} />
            {errorMessage ? <Text style={styles.error}>{errorMessage}</Text> : null}
            <Pressable style={[styles.action, isLoading && styles.actionDisabled]} disabled={isLoading} onPress={() => void verifyMfa(code.trim())}>
              <Text style={styles.actionLabel}>{isLoading ? "Verifying..." : "Verify code"}</Text>
            </Pressable>
            <Pressable style={styles.linkAction} disabled={isLoading} onPress={() => void resendMfa()}>
              <Text style={styles.linkLabel}>Resend code</Text>
            </Pressable>
          </View>
        ) : view === "register" ? (
          <View style={styles.stack}>
            <View style={styles.flowHeader}>
              <Text style={styles.flowTitle}>Register client access</Text>
              <Text style={styles.sectionIntro}>Create the profile, verify the code, then continue into the signed-in workspace.</Text>
            </View>
            <View style={styles.fieldGroup}>
              <Text style={styles.fieldGroupLabel}>Identity</Text>
              <TextInput value={name} onChangeText={setName} placeholder="Full name" placeholderTextColor={colors.muted} style={styles.input} />
              <TextInput value={email} onChangeText={setEmail} placeholder="Email" placeholderTextColor={colors.muted} autoCapitalize="none" style={styles.input} />
            </View>
            <View style={styles.fieldGroup}>
              <Text style={styles.fieldGroupLabel}>Contact</Text>
              <TextInput value={phone} onChangeText={setPhone} placeholder="Phone" placeholderTextColor={colors.muted} style={styles.input} />
              <TextInput value={digitalAddress} onChangeText={setDigitalAddress} placeholder="Digital address" placeholderTextColor={colors.muted} style={styles.input} />
              <TextInput value={password} onChangeText={setPassword} placeholder="Password" placeholderTextColor={colors.muted} secureTextEntry style={styles.input} />
            </View>
            {registrationToken ? <TextInput value={code} onChangeText={setCode} placeholder="Registration code" placeholderTextColor={colors.muted} keyboardType="number-pad" style={styles.input} /> : null}
            {localFeedback ? <Text style={styles.meta}>{localFeedback}</Text> : null}
            <Pressable style={[styles.action, isLoading && styles.actionDisabled]} disabled={isLoading} onPress={() => void submitRegistration()}>
              <Text style={styles.actionLabel}>{isLoading ? "Starting..." : "Start registration"}</Text>
            </Pressable>
            {registrationToken ? (
              <Pressable style={[styles.secondaryAction, isLoading && styles.actionDisabled]} disabled={isLoading} onPress={() => void submitRegistrationVerification()}>
                <Text style={styles.secondaryActionLabel}>{isLoading ? "Verifying..." : "Verify registration"}</Text>
              </Pressable>
            ) : null}
          </View>
        ) : view === "reset" ? (
          <View style={styles.stack}>
            <View style={styles.flowHeader}>
              <Text style={styles.flowTitle}>Reset credentials</Text>
              <Text style={styles.sectionIntro}>Request a code, confirm it, and set a new password without losing the customer link.</Text>
            </View>
            <View style={styles.fieldGroup}>
              <Text style={styles.fieldGroupLabel}>Recovery</Text>
              <TextInput value={email} onChangeText={setEmail} placeholder="Email" placeholderTextColor={colors.muted} autoCapitalize="none" style={styles.input} />
              <TextInput value={newPassword} onChangeText={setNewPassword} placeholder="New password" placeholderTextColor={colors.muted} secureTextEntry style={styles.input} />
            </View>
            {resetToken ? <TextInput value={code} onChangeText={setCode} placeholder="Reset code" placeholderTextColor={colors.muted} keyboardType="number-pad" style={styles.input} /> : null}
            {localFeedback ? <Text style={styles.meta}>{localFeedback}</Text> : null}
            <Pressable style={[styles.action, isLoading && styles.actionDisabled]} disabled={isLoading} onPress={() => void submitPasswordResetStart()}>
              <Text style={styles.actionLabel}>{isLoading ? "Sending..." : "Send reset code"}</Text>
            </Pressable>
            {resetToken ? (
              <Pressable style={[styles.secondaryAction, isLoading && styles.actionDisabled]} disabled={isLoading} onPress={() => void submitPasswordResetComplete()}>
                <Text style={styles.secondaryActionLabel}>{isLoading ? "Resetting..." : "Complete reset"}</Text>
              </Pressable>
            ) : null}
          </View>
        ) : (
          <View style={styles.stack}>
            <View style={styles.infoPanel}>
              <Text style={styles.infoEyebrow}>Access</Text>
              <Text style={styles.infoTitle}>Open balances, payments, statements, support, and secured actions.</Text>
            </View>
            <View style={styles.fieldGroup}>
              <Text style={styles.fieldGroupLabel}>Credentials</Text>
              <TextInput value={email} onChangeText={setEmail} placeholder="Email" placeholderTextColor={colors.muted} keyboardType="email-address" autoCapitalize="none" style={styles.input} />
              <TextInput value={password} onChangeText={setPassword} placeholder="Password" placeholderTextColor={colors.muted} secureTextEntry style={styles.input} />
            </View>
            {errorMessage ? <Text style={styles.error}>{errorMessage}</Text> : null}
            {localFeedback ? <Text style={styles.meta}>{localFeedback}</Text> : null}
            <Pressable style={[styles.action, isLoading && styles.actionDisabled]} disabled={isLoading} onPress={() => void signIn({ email: email.trim(), password })}>
              <Text style={styles.actionLabel}>{isLoading ? "Preparing secure session..." : "Continue"}</Text>
            </Pressable>
          </View>
        )}
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  hero: {
    backgroundColor: colors.surfaceStrong,
    borderRadius: 24,
    padding: spacing.lg,
    gap: spacing.md
  },
  heroTopRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
    gap: spacing.sm
  },
  heroIconWrap: {
    width: 48,
    height: 48,
    borderRadius: 16,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "rgba(245, 248, 252, 0.1)",
    borderWidth: 1,
    borderColor: "rgba(245, 248, 252, 0.12)"
  },
  eyebrow: {
    textTransform: "uppercase",
    letterSpacing: 1.4,
    color: colors.copperSoft,
    fontSize: 12,
    fontWeight: "700",
    fontFamily: typography.body
  },
  heroTitle: {
    color: colors.inkInverse,
    fontSize: 28,
    lineHeight: 34,
    fontWeight: "800",
    fontFamily: typography.display,
    letterSpacing: -0.5,
    marginTop: 4
  },
  heroCopy: {
    color: "rgba(245, 238, 229, 0.84)",
    fontSize: 15,
    lineHeight: 22,
    fontFamily: typography.body
  },
  heroStats: {
    flexDirection: "row",
    gap: spacing.sm
  },
  heroStatTile: {
    flex: 1,
    borderRadius: 18,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.sm,
    backgroundColor: "rgba(245, 238, 229, 0.08)",
    borderWidth: 1,
    borderColor: "rgba(245, 238, 229, 0.12)",
    gap: 6
  },
  heroStatValue: {
    color: colors.inkInverse,
    fontSize: 16,
    fontWeight: "800",
    fontFamily: typography.display
  },
  heroStatLabel: {
    color: "rgba(245, 238, 229, 0.72)",
    fontSize: 12,
    fontFamily: typography.body
  },
  stack: {
    gap: spacing.sm
  },
  modeTabs: {
    flexDirection: "row",
    gap: spacing.sm
  },
  modeTab: {
    flex: 1,
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.border,
    paddingVertical: 12,
    backgroundColor: colors.surfaceSoft
  },
  modeTabActive: {
    borderColor: colors.copper,
    backgroundColor: "#e2edf9"
  },
  modeTabLabel: {
    textAlign: "center",
    color: colors.muted,
    fontWeight: "700",
    fontFamily: typography.body
  },
  modeTabLabelActive: {
    color: colors.forest
  },
  flowHeader: {
    gap: 4
  },
  flowTitle: {
    color: colors.text,
    fontSize: 20,
    fontWeight: "800",
    fontFamily: typography.display,
    letterSpacing: -0.3
  },
  fieldGroup: {
    gap: spacing.sm,
    padding: spacing.md,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: "rgba(230, 237, 245, 0.45)"
  },
  fieldGroupLabel: {
    color: colors.forestSoft,
    fontSize: 12,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.9,
    fontFamily: typography.body
  },
  infoPanel: {
    borderRadius: 18,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: 4
  },
  infoEyebrow: {
    color: colors.copper,
    fontSize: 11,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.9,
    fontFamily: typography.body
  },
  infoTitle: {
    color: colors.textSoft,
    lineHeight: 21,
    fontWeight: "700",
    fontFamily: typography.body
  },
  meta: {
    color: colors.muted,
    fontSize: 14,
    lineHeight: 20,
    fontFamily: typography.body
  },
  sectionIntro: {
    color: colors.textSoft,
    fontSize: 14,
    lineHeight: 21,
    fontFamily: typography.body
  },
  devCodePanel: {
    borderRadius: 18,
    padding: spacing.md,
    backgroundColor: "#f4ecd2",
    borderWidth: 1,
    borderColor: "#e2cf88",
    gap: 4
  },
  devCodeLabel: {
    color: colors.forest,
    fontSize: 12,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 1,
    fontFamily: typography.body
  },
  devCodeValue: {
    color: colors.forestDeep,
    fontSize: 28,
    fontWeight: "700",
    letterSpacing: 2,
    fontFamily: typography.mono
  },
  devCodeHint: {
    color: colors.textSoft,
    fontSize: 13,
    fontFamily: typography.body
  },
  input: {
    backgroundColor: colors.surfaceSoft,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: 14,
    color: colors.text,
    fontFamily: typography.body,
    fontSize: 15
  },
  error: {
    color: colors.critical,
    fontSize: 14,
    fontFamily: typography.body
  },
  action: {
    backgroundColor: colors.forest,
    borderRadius: 999,
    paddingVertical: 15,
    paddingHorizontal: spacing.lg
  },
  actionDisabled: {
    opacity: 0.6
  },
  actionLabel: {
    color: colors.white,
    textAlign: "center",
    fontWeight: "800",
    fontFamily: typography.body
  },
  secondaryAction: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.forest,
    paddingVertical: 15,
    paddingHorizontal: spacing.lg
  },
  secondaryActionLabel: {
    color: colors.forest,
    textAlign: "center",
    fontWeight: "800",
    fontFamily: typography.body
  },
  linkAction: {
    paddingVertical: 8
  },
  linkLabel: {
    color: colors.forestSoft,
    textAlign: "center",
    fontWeight: "700",
    fontFamily: typography.body
  }
});
