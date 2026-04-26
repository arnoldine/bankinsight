import { Ionicons } from "@expo/vector-icons";
import { useNavigation } from "@react-navigation/native";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { appConfig } from "../config";
import { colors, spacing, typography } from "../theme";
import { Badge } from "../components/Badge";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { useSession } from "../context/SessionContext";
import { useClientData } from "../hooks/useClientData";
import type { MetricCard } from "../types";

const metricIcons: Record<string, keyof typeof Ionicons.glyphMap> = {
  Protection: "shield-checkmark-outline",
  Sessions: "phone-portrait-outline",
  Cases: "chatbubbles-outline",
  KYC: "id-card-outline"
};

export function HomeScreen() {
  const navigation = useNavigation<any>();
  const { user } = useSession();
  const { bootstrap, accounts, sessions, complaints, statements, fixedDeposits, bankingOverview, kycOverview, isLoading, permissionWarnings, errorMessage } = useClientData(user);
  const totalBalance = accounts.reduce((sum, account) => sum + account.balance, 0);
  const activeSessions = sessions.filter((session) => session.isActive).length;
  const openComplaints = complaints.filter((complaint) => !["RESOLVED", "CLOSED"].includes(complaint.status.toUpperCase())).length;
  const latestKycCase = kycOverview?.cases?.[0];
  const statementPeriods = statements.length;
  const liveMetrics: MetricCard[] = [
    {
      label: "Protection",
      value: activeSessions > 0 ? "On" : "Review",
      tone: activeSessions > 0 ? "stable" : "warning"
    },
    {
      label: "Sessions",
      value: `${activeSessions}`,
      tone: activeSessions > 1 ? "warning" : "stable"
    },
    {
      label: "Cases",
      value: `${openComplaints}`,
      tone: openComplaints > 0 ? "warning" : "stable"
    },
    {
      label: "KYC",
      value: latestKycCase?.status?.replace(/_/g, " ") ?? "Ready",
      tone: latestKycCase && !["APPROVED", "COMPLETED"].includes(latestKycCase.status.toUpperCase()) ? "warning" : "stable"
    }
  ];

  return (
    <Screen>
      <Card title="Overview" description="Today at a glance">
        <View style={styles.hero}>
          <View style={styles.heroHeader}>
            <View style={styles.heroText}>
              <Text style={styles.eyebrow}>BankInsight</Text>
              <Text style={styles.heroTitle}>Good morning, {user?.name?.split(" ")[0] ?? "Customer"}</Text>
              <Text style={styles.heroCopy}>Balances, payments, investments, and controls.</Text>
            </View>
            <View style={styles.heroStatus}>
              <Text style={styles.heroStatusValue}>GHS {(bankingOverview?.totalVisibleBalance ?? totalBalance).toFixed(0)}</Text>
              <Text style={styles.heroStatusLabel}>available</Text>
            </View>
          </View>
          <View style={styles.heroMetrics}>
            <View style={styles.heroMetricCard}>
              <Ionicons name="wallet-outline" size={18} color={colors.goldSoft} />
              <Text style={styles.heroMetricValue}>{accounts.length}</Text>
              <Text style={styles.heroMetricLabel}>accounts</Text>
            </View>
            <View style={styles.heroMetricCard}>
              <Ionicons name="cash-outline" size={18} color={colors.goldSoft} />
              <Text style={styles.heroMetricValue}>GHS {totalBalance.toFixed(0)}</Text>
              <Text style={styles.heroMetricLabel}>portfolio</Text>
            </View>
            <View style={styles.heroMetricCard}>
              <Ionicons name="trending-up-outline" size={18} color={colors.goldSoft} />
              <Text style={styles.heroMetricValue}>{bankingOverview?.activeInvestmentCount ?? fixedDeposits.length}</Text>
              <Text style={styles.heroMetricLabel}>investments</Text>
            </View>
          </View>
          <View style={styles.actionRow}>
            <Pressable style={styles.primaryAction} onPress={() => navigation.navigate("MainTabs", { screen: "Banking" })}>
              <View style={styles.actionContent}>
                <Ionicons name="swap-horizontal-outline" size={18} color={colors.forest} />
                <Text style={styles.primaryActionLabel}>Banking</Text>
              </View>
            </Pressable>
            <Pressable style={styles.secondaryAction} onPress={() => navigation.navigate("SecurityHub")}>
              <View style={styles.actionContent}>
                <Ionicons name="shield-half-outline" size={18} color="#fbf7ef" />
                <Text style={styles.secondaryActionLabel}>Security</Text>
              </View>
            </Pressable>
          </View>
        </View>
      </Card>

      <View style={styles.metricGrid}>
        {liveMetrics.map((metric) => (
          <View key={metric.label} style={styles.metricTile}>
            <View style={styles.metricTop}>
              <View style={styles.metricIconWrap}>
                <Ionicons name={metricIcons[metric.label] ?? "ellipse-outline"} size={18} color={colors.forest} />
              </View>
              <Badge tone={metric.tone}>{metric.tone}</Badge>
            </View>
            <Text style={styles.metricLabel}>{metric.label}</Text>
            <Text style={styles.metricValue}>{metric.value}</Text>
          </View>
        ))}
      </View>

      <Card title="Positions" description="Accounts, lending, and records">
        <View style={styles.positionsGrid}>
          <View style={styles.positionItem}>
            <Text style={styles.positionLabel}>Standing orders</Text>
            <Text style={styles.positionValue}>{bankingOverview?.activeStandingOrderCount ?? 0}</Text>
          </View>
          <View style={styles.positionItem}>
            <Text style={styles.positionLabel}>Loan exposure</Text>
            <Text style={styles.positionValue}>GHS {(bankingOverview?.totalLoanExposure ?? 0).toFixed(0)}</Text>
          </View>
          <View style={styles.positionItem}>
            <Text style={styles.positionLabel}>Statements</Text>
            <Text style={styles.positionValue}>{statementPeriods}</Text>
          </View>
          <View style={styles.positionItem}>
            <Text style={styles.positionLabel}>Client</Text>
            <Text style={styles.positionValueSmall}>{bootstrap?.linkedCustomer?.name ?? user?.name ?? "Unlinked"}</Text>
          </View>
        </View>
      </Card>

      {permissionWarnings.length > 0 || errorMessage ? (
        <Card title="Attention" description="Items requiring review">
          {permissionWarnings.map((warning) => (
            <Text key={warning} style={styles.warningText}>
              {warning}
            </Text>
          ))}
          {errorMessage ? <Text style={styles.errorText}>{errorMessage}</Text> : null}
        </Card>
      ) : (
        <Card title="Session" description="Current policy">
          <View style={styles.detailRow}>
            <Text style={styles.detailLabel}>Notice</Text>
            <Text style={styles.detailValue}>{appConfig.privacyNoticeVersion}</Text>
          </View>
          <View style={styles.detailRow}>
            <Text style={styles.detailLabel}>Timeout</Text>
            <Text style={styles.detailValue}>{appConfig.sessionTimeoutMinutes} min</Text>
          </View>
          <View style={styles.detailRow}>
            <Text style={styles.detailLabel}>Status</Text>
            <Text style={styles.detailValue}>{isLoading ? "Syncing" : "Live"}</Text>
          </View>
        </Card>
      )}
    </Screen>
  );
}

const styles = StyleSheet.create({
  hero: {
    backgroundColor: colors.surfaceStrong,
    borderRadius: 20,
    padding: spacing.lg,
    gap: spacing.md
  },
  heroHeader: {
    flexDirection: "row",
    gap: spacing.md,
    alignItems: "flex-start"
  },
  heroText: {
    flex: 1,
    gap: 6
  },
  heroStatus: {
    minWidth: 108,
    borderRadius: 18,
    backgroundColor: "rgba(249, 251, 254, 0.08)",
    borderWidth: 1,
    borderColor: "rgba(249, 251, 254, 0.12)",
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.sm,
    alignItems: "center"
  },
  heroStatusValue: {
    color: colors.white,
    fontSize: 28,
    fontWeight: "800",
    fontFamily: typography.display,
    letterSpacing: -0.8
  },
  heroStatusLabel: {
    color: "rgba(249, 251, 254, 0.68)",
    fontSize: 11,
    marginTop: 4,
    textTransform: "uppercase",
    letterSpacing: 1
  },
  eyebrow: {
    textTransform: "uppercase",
    letterSpacing: 1.7,
    color: colors.copperSoft,
    fontSize: 11,
    fontWeight: "700"
  },
  heroTitle: {
    color: colors.inkInverse,
    fontSize: 28,
    lineHeight: 32,
    fontWeight: "800",
    letterSpacing: -0.9,
    fontFamily: typography.display
  },
  heroCopy: {
    color: "rgba(245, 248, 252, 0.76)",
    fontSize: 13,
    lineHeight: 18,
    fontFamily: typography.body
  },
  heroMetrics: {
    flexDirection: "row",
    gap: spacing.sm
  },
  heroMetricCard: {
    flex: 1,
    borderRadius: 16,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.md,
    backgroundColor: "rgba(249, 251, 254, 0.06)",
    borderWidth: 1,
    borderColor: "rgba(249, 251, 254, 0.12)"
  },
  heroMetricValue: {
    color: colors.white,
    fontSize: 18,
    fontWeight: "800",
    fontFamily: typography.display,
    letterSpacing: -0.5
  },
  heroMetricLabel: {
    color: "rgba(249, 251, 254, 0.62)",
    fontSize: 11,
    marginTop: 4
  },
  actionRow: {
    flexDirection: "row",
    gap: spacing.sm
  },
  actionContent: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: spacing.sm
  },
  primaryAction: {
    flex: 1,
    backgroundColor: colors.white,
    borderRadius: 999,
    paddingVertical: 14,
    paddingHorizontal: spacing.lg
  },
  primaryActionLabel: {
    color: colors.forest,
    fontWeight: "700",
    textAlign: "center",
    fontFamily: typography.body
  },
  secondaryAction: {
    flex: 1,
    borderRadius: 999,
    borderWidth: 1,
    borderColor: "rgba(255, 255, 255, 0.22)",
    paddingVertical: 14,
    paddingHorizontal: spacing.lg
  },
  secondaryActionLabel: {
    color: colors.white,
    fontWeight: "700",
    textAlign: "center",
    fontFamily: typography.body
  },
  metricGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: spacing.sm
  },
  metricValue: {
    fontSize: 28,
    fontWeight: "800",
    color: colors.text,
    fontFamily: typography.display,
    letterSpacing: -0.8
  },
  metricTile: {
    width: "48%",
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: spacing.md,
    gap: spacing.xs
  },
  metricTop: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center"
  },
  metricIconWrap: {
    width: 36,
    height: 36,
    borderRadius: 12,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  metricLabel: {
    color: colors.muted,
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 1,
    fontFamily: typography.body
  },
  positionsGrid: {
    gap: spacing.sm
  },
  positionItem: {
    borderRadius: 16,
    backgroundColor: colors.surfaceSoft,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: 4
  },
  positionLabel: {
    color: colors.muted,
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 1,
    fontFamily: typography.body
  },
  positionValue: {
    color: colors.text,
    fontSize: 24,
    fontWeight: "800",
    letterSpacing: -0.7,
    fontFamily: typography.display
  },
  positionValueSmall: {
    color: colors.textSoft,
    fontSize: 16,
    fontWeight: "700",
    fontFamily: typography.display
  },
  detailRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    gap: spacing.md,
    paddingVertical: 4
  },
  detailLabel: {
    color: colors.muted,
    fontSize: 13,
    fontFamily: typography.body
  },
  detailValue: {
    color: colors.textSoft,
    fontSize: 13,
    fontWeight: "600",
    flexShrink: 1,
    textAlign: "right",
    fontFamily: typography.body
  },
  warningText: {
    fontSize: 14,
    color: colors.warning,
    fontFamily: typography.body
  },
  errorText: {
    fontSize: 14,
    color: colors.critical,
    fontFamily: typography.body
  }
});
