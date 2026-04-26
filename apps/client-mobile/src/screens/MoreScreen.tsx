import { Ionicons } from "@expo/vector-icons";
import { useNavigation } from "@react-navigation/native";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { colors, spacing } from "../theme";

const shortcuts = [
  {
    route: "StatementsHub",
    icon: "archive-outline",
    eyebrow: "Archive",
    title: "Statements",
    copy: "Review exported periods, balances, and account history in one place."
  },
  {
    route: "AlertsHub",
    icon: "notifications-outline",
    eyebrow: "Awareness",
    title: "Alerts",
    copy: "Track customer notices, risk nudges, and important account events."
  },
  {
    route: "ProfileHub",
    icon: "person-circle-outline",
    eyebrow: "Identity",
    title: "Profile and KYC",
    copy: "Manage contact details, upload evidence, and request KYC review."
  },
  {
    route: "SecurityHub",
    icon: "shield-checkmark-outline",
    eyebrow: "Protection",
    title: "Security center",
    copy: "Review device trust, sessions, and protective controls."
  }
] as const;

export function MoreScreen() {
  const navigation = useNavigation<any>();

  return (
    <Screen>
      <Card title="More" description="Secondary tools are grouped here so the main navigation stays focused on the banking journey.">
        <View style={styles.hero}>
          <View style={styles.heroIconWrap}>
            <Ionicons name="grid-outline" size={28} color={colors.goldSoft} />
          </View>
          <Text style={styles.eyebrow}>Client workspace</Text>
          <Text style={styles.title}>Everything important is still here, just arranged more calmly</Text>
          <Text style={styles.copy}>
            Statements, alerts, security, and profile controls are still part of the product, but no longer crowd the primary navigation.
          </Text>
        </View>
      </Card>

      {shortcuts.map((shortcut) => (
        <Pressable key={shortcut.route} onPress={() => navigation.navigate(shortcut.route)}>
          <Card title={shortcut.title} description={shortcut.copy}>
            <View style={styles.row}>
              <View style={styles.iconWrap}>
                <Ionicons name={shortcut.icon} size={22} color={colors.forest} />
              </View>
              <View style={styles.copyBlock}>
                <Text style={styles.shortcutEyebrow}>{shortcut.eyebrow}</Text>
                <Text style={styles.shortcutTitle}>{shortcut.title}</Text>
              </View>
              <View style={styles.arrowWrap}>
                <Text style={styles.arrow}>Open</Text>
                <Ionicons name="chevron-forward-outline" size={18} color={colors.forest} />
              </View>
            </View>
          </Card>
        </Pressable>
      ))}
    </Screen>
  );
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
  title: {
    color: colors.inkInverse,
    fontSize: 24,
    lineHeight: 30,
    fontWeight: "700"
  },
  copy: {
    color: "rgba(245, 238, 229, 0.82)",
    lineHeight: 21
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md
  },
  iconWrap: {
    width: 44,
    height: 44,
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
  shortcutEyebrow: {
    color: colors.copper,
    fontSize: 11,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.9
  },
  shortcutTitle: {
    color: colors.text,
    fontSize: 18,
    fontWeight: "700"
  },
  arrow: {
    color: colors.forest,
    fontWeight: "700"
  },
  arrowWrap: {
    flexDirection: "row",
    alignItems: "center",
    gap: 4
  }
});
