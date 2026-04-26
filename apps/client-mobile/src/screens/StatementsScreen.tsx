import { Ionicons } from "@expo/vector-icons";
import { useState } from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { useSession } from "../context/SessionContext";
import { useClientData } from "../hooks/useClientData";
import { exportClientStatement, type ClientStatementExport } from "../services/clientChannelApi";
import { colors, spacing, typography } from "../theme";

export function StatementsScreen() {
  const { user } = useSession();
  const { statements, isLoading, permissionWarnings } = useClientData(user);
  const [exportingId, setExportingId] = useState<string | null>(null);
  const [latestExport, setLatestExport] = useState<ClientStatementExport | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleExport(statementId: string, accountId: string, year: number, month: number) {
    setExportingId(statementId);
    setErrorMessage(null);
    try {
      const exported = await exportClientStatement(accountId, year, month);
      setLatestExport(exported);
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Unable to export statement.");
    } finally {
      setExportingId(null);
    }
  }

  return (
    <Screen>
      <Card title="Statements" description="Archive and export">
        <View style={styles.hero}>
          <View style={styles.heroPanel}>
            <Text style={styles.heroLabel}>Periods</Text>
            <Text style={styles.heroValue}>{statements.length}</Text>
          </View>
          <View style={styles.heroPanel}>
            <Text style={styles.heroLabel}>Latest export</Text>
            <Text style={styles.heroValueSmall}>{latestExport ? latestExport.fileName : "None"}</Text>
          </View>
        </View>

        {latestExport ? (
          <View style={styles.exportPanel}>
            <View style={styles.exportHeader}>
              <Ionicons name="download-outline" size={18} color={colors.inkInverse} />
              <Text style={styles.exportTitle}>{latestExport.fileName}</Text>
            </View>
            <Text style={styles.exportMeta}>
              {latestExport.lineCount} lines • {new Date(latestExport.exportedAt).toLocaleString()}
            </Text>
          </View>
        ) : null}

        {isLoading ? <Text style={styles.meta}>Loading statements...</Text> : null}

        {!isLoading && statements.length === 0 ? (
          <Text style={styles.meta}>No statement periods are available for this customer yet.</Text>
        ) : null}

        {statements.map((statement) => (
          <View key={statement.statementId} style={styles.item}>
            <View style={styles.itemTop}>
              <View style={styles.periodBlock}>
                <View style={styles.periodIconWrap}>
                  <Ionicons name="document-text-outline" size={18} color={colors.forest} />
                </View>
                <View style={styles.periodCopy}>
                  <Text style={styles.period}>{statement.periodLabel}</Text>
                  <Text style={styles.meta}>Account •••• {statement.accountId.slice(-4)}</Text>
                </View>
              </View>
              <View style={styles.countBadge}>
                <Text style={styles.countBadgeLabel}>{statement.entryCount}</Text>
              </View>
            </View>

            <View style={styles.amountRow}>
              <View style={styles.amountTile}>
                <Text style={styles.amountLabel}>Credits</Text>
                <Text style={styles.creditValue}>{statement.totalCredits.toLocaleString()}</Text>
              </View>
              <View style={styles.amountTile}>
                <Text style={styles.amountLabel}>Debits</Text>
                <Text style={styles.debitValue}>{statement.totalDebits.toLocaleString()}</Text>
              </View>
            </View>

            <Pressable
              style={[styles.exportAction, exportingId === statement.statementId && styles.exportActionDisabled]}
              disabled={exportingId === statement.statementId}
              onPress={() => {
                void handleExport(statement.statementId, statement.accountId, statement.year, statement.month);
              }}
            >
              <Text style={styles.exportActionLabel}>
                {exportingId === statement.statementId ? "Preparing..." : "Export CSV"}
              </Text>
            </Pressable>
          </View>
        ))}

        {errorMessage ? <Text style={styles.error}>{errorMessage}</Text> : null}
        {permissionWarnings.map((warning) => (
          <Text key={warning} style={styles.meta}>
            {warning}
          </Text>
        ))}
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  hero: {
    flexDirection: "row",
    gap: spacing.sm
  },
  heroPanel: {
    flex: 1,
    borderRadius: 16,
    backgroundColor: colors.surfaceStrong,
    padding: spacing.md,
    gap: 4
  },
  heroLabel: {
    color: "rgba(245, 248, 252, 0.68)",
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 1,
    fontFamily: typography.body
  },
  heroValue: {
    color: colors.inkInverse,
    fontSize: 24,
    fontWeight: "800",
    letterSpacing: -0.8,
    fontFamily: typography.display
  },
  heroValueSmall: {
    color: colors.inkInverse,
    fontSize: 14,
    fontWeight: "700",
    fontFamily: typography.display
  },
  exportPanel: {
    borderRadius: 16,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: 4
  },
  exportHeader: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm
  },
  exportTitle: {
    color: colors.text,
    fontWeight: "700",
    fontSize: 15,
    fontFamily: typography.display
  },
  exportMeta: {
    color: colors.textSoft,
    fontSize: 12,
    fontFamily: typography.body
  },
  item: {
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: spacing.md,
    gap: spacing.sm
  },
  itemTop: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    gap: spacing.md
  },
  periodBlock: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm,
    flex: 1
  },
  periodIconWrap: {
    width: 40,
    height: 40,
    borderRadius: 12,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  periodCopy: {
    flex: 1
  },
  period: {
    color: colors.text,
    fontSize: 16,
    fontWeight: "700",
    fontFamily: typography.display
  },
  meta: {
    color: colors.muted,
    marginTop: 2,
    fontFamily: typography.body
  },
  countBadge: {
    minWidth: 36,
    borderRadius: 999,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    paddingHorizontal: 10,
    paddingVertical: 6,
    alignItems: "center"
  },
  countBadgeLabel: {
    color: colors.textSoft,
    fontSize: 11,
    fontWeight: "700",
    fontFamily: typography.body
  },
  amountRow: {
    flexDirection: "row",
    gap: spacing.sm
  },
  amountTile: {
    flex: 1,
    borderRadius: 14,
    backgroundColor: colors.surfaceSoft,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: 4
  },
  amountLabel: {
    color: colors.muted,
    textTransform: "uppercase",
    letterSpacing: 0.8,
    fontSize: 11,
    fontFamily: typography.body
  },
  creditValue: {
    color: colors.stable,
    fontWeight: "800",
    fontSize: 18,
    fontFamily: typography.display
  },
  debitValue: {
    color: colors.copper,
    fontWeight: "800",
    fontSize: 18,
    fontFamily: typography.display
  },
  exportAction: {
    borderRadius: 999,
    backgroundColor: colors.forest,
    paddingVertical: 12,
    paddingHorizontal: spacing.md
  },
  exportActionDisabled: {
    opacity: 0.65
  },
  exportActionLabel: {
    color: colors.white,
    fontWeight: "700",
    textAlign: "center",
    fontFamily: typography.body
  },
  error: {
    color: colors.critical,
    fontFamily: typography.body
  }
});
