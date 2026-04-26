import { Ionicons } from "@expo/vector-icons";
import { useMemo, useState } from "react";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { useSession } from "../context/SessionContext";
import { useClientData } from "../hooks/useClientData";
import { setTransactionPin } from "../services/authApi";
import { colors, spacing, typography } from "../theme";

export function SecurityScreen() {
  const { user, refreshCurrentUser } = useSession();
  const { sessions, isLoading, permissionWarnings } = useClientData(user);
  const [password, setPassword] = useState("");
  const [pin, setPin] = useState("");
  const [feedback, setFeedback] = useState<string | null>(null);
  const [isSavingPin, setIsSavingPin] = useState(false);

  const sortedSessions = useMemo(
    () =>
      [...sessions].sort((left, right) => {
        const leftDate = new Date(left.lastActivity ?? left.createdAt).getTime();
        const rightDate = new Date(right.lastActivity ?? right.createdAt).getTime();
        return rightDate - leftDate;
      }),
    [sessions]
  );

  const activeSessionCount = sortedSessions.filter((session) => session.isActive).length;
  const staleSessionCount = sortedSessions.length - activeSessionCount;

  async function savePin() {
    setIsSavingPin(true);
    setFeedback(null);
    try {
      const result = await setTransactionPin({ password, pin });
      setFeedback(result.message);
      setPassword("");
      setPin("");
      await refreshCurrentUser();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to save transaction PIN.");
    } finally {
      setIsSavingPin(false);
    }
  }

  return (
    <Screen>
      <Card title="Security" description="Session visibility and approval controls">
        <View style={styles.hero}>
          <View style={styles.heroIconWrap}>
            <Ionicons name="shield-checkmark-outline" size={28} color={colors.goldSoft} />
          </View>
          <Text style={styles.eyebrow}>Protection</Text>
          <Text style={styles.title}>See active access instantly.</Text>
          <Text style={styles.copy}>Sessions and transaction approval.</Text>
          <View style={styles.metrics}>
            <View style={styles.metricTile}>
              <Ionicons name="radio-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.metricValue}>{activeSessionCount}</Text>
              <Text style={styles.metricLabel}>active sessions</Text>
            </View>
            <View style={styles.metricTile}>
              <Ionicons name="time-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.metricValue}>{staleSessionCount}</Text>
              <Text style={styles.metricLabel}>inactive sessions</Text>
            </View>
          </View>
        </View>
      </Card>

      <Card title="Transaction PIN" description="Approval factor">
        <Text style={styles.meta}>{user?.hasTransactionPin ? "A transaction PIN is already active on this profile." : "Set a transaction PIN so money movement can be approved without waiting for an OTP."}</Text>
        <TextInput
          value={password}
          onChangeText={setPassword}
          placeholder="Current password"
          placeholderTextColor={colors.muted}
          secureTextEntry
          style={styles.input}
        />
        <TextInput
          value={pin}
          onChangeText={setPin}
          placeholder="4-digit transaction PIN"
          placeholderTextColor={colors.muted}
          keyboardType="number-pad"
          secureTextEntry
          maxLength={4}
          style={styles.input}
        />
        <Pressable style={[styles.action, isSavingPin && styles.disabled]} disabled={isSavingPin} onPress={() => void savePin()}>
          <Text style={styles.actionLabel}>{user?.hasTransactionPin ? "Update transaction PIN" : "Set transaction PIN"}</Text>
        </Pressable>
        {feedback ? <Text style={styles.meta}>{feedback}</Text> : null}
      </Card>

      <Card title="Sessions" description="Live client-channel activity">
        {isLoading ? <Text style={styles.meta}>Loading session visibility...</Text> : null}
        {!isLoading && sortedSessions.length === 0 ? (
          <Text style={styles.meta}>No live sessions were returned for this identity.</Text>
        ) : null}
        {sortedSessions.map((session, index) => (
          <View key={`${session.id ?? "session"}-${index}`} style={styles.item}>
            <View style={styles.itemTop}>
              <View style={styles.sessionIconWrap}>
                <Ionicons
                  name={session.isActive ? "phone-portrait-outline" : "desktop-outline"}
                  size={20}
                  color={colors.forest}
                />
              </View>
              <View style={styles.copyBlock}>
                <Text style={styles.name}>{session.userAgent ?? "Unknown device"}</Text>
                <Text style={styles.meta}>{session.ipAddress ?? "Unknown IP"}</Text>
              </View>
              <View style={[styles.statusPill, session.isActive ? styles.statusPositive : styles.statusMuted]}>
                <Text style={[styles.statusLabel, session.isActive ? styles.statusLabelPositive : styles.statusLabelMuted]}>
                  {session.isActive ? "Active" : "Ended"}
                </Text>
              </View>
            </View>
            <Text style={styles.meta}>Last activity: {formatTimestamp(session.lastActivity ?? session.createdAt)}</Text>
            <Text style={styles.meta}>Expires: {formatTimestamp(session.expiresAt)}</Text>
          </View>
        ))}
        {permissionWarnings.map((warning) => (
          <Text key={warning} style={styles.warning}>
            {warning}
          </Text>
        ))}
      </Card>
    </Screen>
  );
}

function formatTimestamp(value?: string | null) {
  if (!value) {
    return "Unknown";
  }

  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString();
}

const styles = StyleSheet.create({
  hero: {
    backgroundColor: colors.surfaceStrong,
    borderRadius: 24,
    padding: spacing.lg,
    gap: spacing.sm
  },
  heroIconWrap: {
    width: 52,
    height: 52,
    borderRadius: 18,
    backgroundColor: "rgba(245, 238, 229, 0.10)",
    borderWidth: 1,
    borderColor: "rgba(245, 238, 229, 0.16)",
    alignItems: "center",
    justifyContent: "center"
  },
  eyebrow: {
    color: colors.copperSoft,
    fontSize: 11,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 1,
    fontFamily: typography.body
  },
  title: {
    color: colors.inkInverse,
    fontSize: 26,
    lineHeight: 30,
    fontWeight: "800",
    letterSpacing: -0.8,
    fontFamily: typography.display
  },
  copy: {
    color: "rgba(245, 248, 252, 0.76)",
    lineHeight: 18,
    fontSize: 13,
    fontFamily: typography.body
  },
  metrics: {
    flexDirection: "row",
    gap: spacing.sm
  },
  metricTile: {
    flex: 1,
    borderRadius: 18,
    padding: spacing.md,
    backgroundColor: "rgba(245, 238, 229, 0.08)",
    borderWidth: 1,
    borderColor: "rgba(245, 238, 229, 0.12)",
    gap: 4
  },
  metricValue: {
    color: colors.inkInverse,
    fontSize: 20,
    fontWeight: "800",
    fontFamily: typography.display
  },
  metricLabel: {
    color: "rgba(245, 238, 229, 0.72)",
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 0.8,
    fontFamily: typography.body
  },
  input: {
    backgroundColor: colors.surfaceSoft,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: 14,
    color: colors.text
  },
  action: {
    backgroundColor: colors.forest,
    borderRadius: 999,
    paddingVertical: 14,
    paddingHorizontal: spacing.lg
  },
  actionLabel: {
    color: colors.white,
    fontWeight: "700",
    textAlign: "center",
    fontFamily: typography.body
  },
  disabled: {
    opacity: 0.6
  },
  item: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 18,
    padding: spacing.md,
    backgroundColor: colors.surfaceSoft,
    gap: 6
  },
  itemTop: {
    flexDirection: "row",
    gap: spacing.md,
    alignItems: "flex-start"
  },
  sessionIconWrap: {
    width: 42,
    height: 42,
    borderRadius: 14,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  copyBlock: {
    flex: 1,
    gap: 4
  },
  name: {
    color: colors.text,
    fontWeight: "700",
    fontFamily: typography.display
  },
  meta: {
    color: colors.muted,
    fontFamily: typography.body
  },
  warning: {
    color: colors.warning,
    fontWeight: "700",
    fontFamily: typography.body
  },
  statusPill: {
    borderRadius: 999,
    borderWidth: 1,
    paddingHorizontal: spacing.sm,
    paddingVertical: 8
  },
  statusPositive: {
    backgroundColor: "rgba(46, 106, 74, 0.08)",
    borderColor: "rgba(46, 106, 74, 0.20)"
  },
  statusMuted: {
    backgroundColor: colors.surfaceMuted,
    borderColor: colors.border
  },
  statusLabel: {
    fontSize: 11,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.8,
    fontFamily: typography.body
  },
  statusLabelPositive: {
    color: colors.stable
  },
  statusLabelMuted: {
    color: colors.textSoft
  }
});
