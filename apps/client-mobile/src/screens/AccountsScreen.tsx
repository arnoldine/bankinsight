import { Ionicons } from "@expo/vector-icons";
import { StyleSheet, Text, View } from "react-native";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { useSession } from "../context/SessionContext";
import { useClientData } from "../hooks/useClientData";
import { colors, spacing, typography } from "../theme";

export function AccountsScreen() {
  const { user } = useSession();
  const { accounts, isLoading, errorMessage } = useClientData(user);
  const totalAvailable = accounts.reduce((sum, account) => sum + Number(account.balance ?? 0), 0);
  const activeCount = accounts.filter((account) => account.status === "ACTIVE").length;

  return (
    <Screen>
      <Card title="Accounts" description="Balances and positions">
        <View style={styles.hero}>
          <View style={styles.heroPanel}>
            <Text style={styles.heroLabel}>Available</Text>
            <Text style={styles.heroValue}>GHS {totalAvailable.toFixed(0)}</Text>
          </View>
          <View style={styles.heroPanel}>
            <Text style={styles.heroLabel}>Active</Text>
            <Text style={styles.heroValue}>{activeCount}</Text>
          </View>
        </View>

        {isLoading ? <Text style={styles.accountMeta}>Loading accounts...</Text> : null}

        {!isLoading && accounts.length === 0 ? (
          <Text style={styles.accountMeta}>No account data is currently available for this login.</Text>
        ) : null}

        {accounts.map((account) => (
          <View key={account.id} style={styles.row}>
            <View style={styles.rowTop}>
              <View style={styles.accountIconWrap}>
                <Ionicons
                  name={account.type === "CURRENT" ? "card-outline" : "wallet-outline"}
                  size={18}
                  color={colors.forest}
                />
              </View>
              <View style={styles.identityBlock}>
                <Text style={styles.accountEyebrow}>{account.type ?? "Account"}</Text>
                <Text style={styles.accountName}>Account •••• {account.id.slice(-4)}</Text>
              </View>
              <View style={[styles.statusPill, account.status === "ACTIVE" && styles.statusPillActive]}>
                <Text style={[styles.statusPillLabel, account.status === "ACTIVE" && styles.statusPillLabelActive]}>
                  {account.status}
                </Text>
              </View>
            </View>

            <Text style={styles.balance}>
              {account.currency.toUpperCase()} {Number(account.balance ?? 0).toLocaleString()}
            </Text>

            <View style={styles.metaRow}>
              <Text style={styles.accountMeta}>Product {account.productCode ?? "Client account"}</Text>
              <Text style={styles.accountMeta}>Lien {Number(account.lienAmount ?? 0).toLocaleString()}</Text>
            </View>
          </View>
        ))}

        {errorMessage ? <Text style={styles.error}>{errorMessage}</Text> : null}
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
  row: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 18,
    padding: spacing.md,
    gap: spacing.sm,
    backgroundColor: colors.surface
  },
  rowTop: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md
  },
  accountIconWrap: {
    width: 40,
    height: 40,
    borderRadius: 12,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  identityBlock: {
    flex: 1,
    gap: 4
  },
  accountEyebrow: {
    color: colors.copper,
    textTransform: "uppercase",
    letterSpacing: 1,
    fontSize: 11,
    fontWeight: "700",
    fontFamily: typography.body
  },
  accountName: {
    fontSize: 16,
    fontWeight: "700",
    color: colors.text,
    fontFamily: typography.display
  },
  metaRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    gap: spacing.md
  },
  accountMeta: {
    color: colors.muted,
    fontFamily: typography.body
  },
  balance: {
    color: colors.forest,
    fontWeight: "800",
    fontSize: 24,
    letterSpacing: -0.7,
    fontFamily: typography.display
  },
  statusPill: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    backgroundColor: colors.surfaceMuted,
    paddingHorizontal: 10,
    paddingVertical: 6
  },
  statusPillActive: {
    borderColor: colors.forestSoft,
    backgroundColor: "#e7f0f9"
  },
  statusPillLabel: {
    color: colors.textSoft,
    fontSize: 11,
    fontWeight: "700",
    fontFamily: typography.body
  },
  statusPillLabelActive: {
    color: colors.forestSoft
  },
  error: {
    color: colors.critical,
    fontFamily: typography.body
  }
});
