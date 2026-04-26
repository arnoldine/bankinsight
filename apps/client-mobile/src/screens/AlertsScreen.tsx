import { Ionicons } from "@expo/vector-icons";
import { useMemo } from "react";
import { StyleSheet, Text, View } from "react-native";
import { Badge } from "../components/Badge";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { useSession } from "../context/SessionContext";
import { useClientData } from "../hooks/useClientData";
import { colors, spacing } from "../theme";

export function AlertsScreen() {
  const { user } = useSession();
  const { sessions, complaints, statements, kycOverview } = useClientData(user);

  const alertItems = useMemo(() => {
    const items: Array<{
      id: string;
      title: string;
      copy: string;
      status: string;
      tone: "warning" | "stable";
      timestamp: string;
      category: string;
    }> = [];

    const recentActiveSession = [...sessions]
      .filter((session) => session.isActive)
      .sort((left, right) => new Date(right.lastActivity ?? right.createdAt).getTime() - new Date(left.lastActivity ?? left.createdAt).getTime())[0];

    if (recentActiveSession) {
      items.push({
        id: `session-${recentActiveSession.id}`,
        title: "Active session detected",
        copy: `${recentActiveSession.userAgent ?? "A device"} last showed activity from ${recentActiveSession.ipAddress ?? "an unknown IP"}.`,
        status: "review",
        tone: "warning",
        timestamp: formatTimestamp(recentActiveSession.lastActivity ?? recentActiveSession.createdAt),
        category: "Security"
      });
    }

    complaints
      .slice(0, 2)
      .forEach((complaint) => {
        items.push({
          id: `complaint-${complaint.id}`,
          title: "Complaint update available",
          copy: `${complaint.reference} is currently ${complaint.status.toLowerCase()} under ${complaint.ownerTeam}.`,
          status: complaint.status === "CLOSED" ? "closed" : "updated",
          tone: complaint.status === "CLOSED" ? "stable" : "warning",
          timestamp: formatTimestamp(complaint.updatedAt),
          category: "Support"
        });
      });

    if (statements[0]) {
      items.push({
        id: `statement-${statements[0].statementId}`,
        title: "Statement archive ready",
        copy: `${statements[0].periodLabel} statement is available with ${statements[0].entryCount} posted entries.`,
        status: "ready",
        tone: "stable",
        timestamp: formatTimestamp(statements[0].generatedAt),
        category: "Statements"
      });
    }

    if (kycOverview?.cases[0]) {
      items.push({
        id: `kyc-${kycOverview.cases[0].id}`,
        title: "KYC case activity",
        copy: `${kycOverview.cases[0].reference} is currently ${kycOverview.cases[0].status.toLowerCase()}.`,
        status: "tracked",
        tone: kycOverview.cases[0].status === "APPROVED" ? "stable" : "warning",
        timestamp: formatTimestamp(kycOverview.cases[0].submittedAt),
        category: "KYC"
      });
    }

    return items.sort((left, right) => new Date(right.timestamp).getTime() - new Date(left.timestamp).getTime());
  }, [complaints, kycOverview, sessions, statements]);

  const actionRequiredCount = alertItems.filter((item) => item.tone === "warning").length;
  const categoryIcons: Record<string, keyof typeof Ionicons.glyphMap> = {
    Security: "shield-checkmark-outline",
    Support: "chatbubble-ellipses-outline",
    Statements: "document-text-outline",
    KYC: "id-card-outline"
  };

  return (
    <Screen>
      <Card title="Alerts" description="Customer notifications now reflect live and derived client-channel activity instead of static sample rows.">
        <View style={styles.hero}>
          <View style={styles.heroIconWrap}>
            <Ionicons name="notifications-outline" size={28} color={colors.goldSoft} />
          </View>
          <Text style={styles.eyebrow}>Awareness</Text>
          <Text style={styles.heroTitle}>A calmer place to review what changed across security, support, and records</Text>
          <Text style={styles.heroCopy}>Alerts are built from recent session activity, complaint movement, statement readiness, and KYC case progress.</Text>
          <View style={styles.metricRow}>
            <View style={styles.metricTile}>
              <Ionicons name="time-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.metricValue}>{alertItems.length}</Text>
              <Text style={styles.metricLabel}>recent alerts</Text>
            </View>
            <View style={styles.metricTile}>
              <Ionicons name="alert-circle-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.metricValue}>{actionRequiredCount}</Text>
              <Text style={styles.metricLabel}>need review</Text>
            </View>
          </View>
        </View>
      </Card>

      <Card title="Recent activity" description="Alerts are organized as concise, timestamped cards that make next steps obvious.">
        <View style={styles.alertList}>
          {alertItems.length === 0 ? <Text style={styles.empty}>No recent alerts are available for this client record.</Text> : null}
          {alertItems.map((item) => (
            <View key={item.id} style={styles.notice}>
              <View style={styles.noticeTop}>
                <View style={styles.noticeIconWrap}>
                  <Ionicons name={categoryIcons[item.category] ?? "ellipse-outline"} size={20} color={colors.forest} />
                </View>
                <View style={styles.noticeCopy}>
                  <Text style={styles.category}>{item.category}</Text>
                  <Text style={styles.title}>{item.title}</Text>
                </View>
                <Badge tone={item.tone}>{item.status}</Badge>
              </View>
              <Text style={styles.copy}>{item.copy}</Text>
              <Text style={styles.timestamp}>{item.timestamp}</Text>
            </View>
          ))}
        </View>
      </Card>
    </Screen>
  );
}

function formatTimestamp(value?: string | null) {
  if (!value) {
    return "Unknown time";
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
    letterSpacing: 1
  },
  heroTitle: {
    color: colors.inkInverse,
    fontSize: 24,
    lineHeight: 30,
    fontWeight: "700"
  },
  heroCopy: {
    color: "rgba(245, 238, 229, 0.82)",
    lineHeight: 21
  },
  metricRow: {
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
    fontWeight: "700"
  },
  metricLabel: {
    color: "rgba(245, 238, 229, 0.72)",
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 0.8
  },
  alertList: {
    gap: spacing.md
  },
  empty: {
    color: colors.muted
  },
  notice: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 18,
    padding: spacing.md,
    backgroundColor: colors.surfaceSoft,
    gap: spacing.sm
  },
  noticeTop: {
    flexDirection: "row",
    alignItems: "flex-start",
    gap: spacing.md
  },
  noticeIconWrap: {
    width: 42,
    height: 42,
    borderRadius: 14,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  noticeCopy: {
    flex: 1,
    gap: 4
  },
  category: {
    color: colors.copper,
    fontSize: 11,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.8
  },
  title: {
    fontWeight: "700",
    color: colors.text
  },
  copy: {
    color: colors.muted,
    lineHeight: 20
  },
  timestamp: {
    color: colors.textSoft,
    fontSize: 12,
    fontWeight: "600"
  }
});
