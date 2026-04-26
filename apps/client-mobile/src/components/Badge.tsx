import type { PropsWithChildren } from "react";
import { StyleSheet, Text, View } from "react-native";
import { colors } from "../theme";

interface BadgeProps extends PropsWithChildren {
  tone: "stable" | "warning" | "critical";
}

export function Badge({ tone, children }: BadgeProps) {
  return (
    <View
      style={[
        styles.badge,
        tone === "stable" && styles.stable,
        tone === "warning" && styles.warning,
        tone === "critical" && styles.critical
      ]}
    >
      <Text
        style={[
          styles.label,
          tone === "stable" && styles.stableText,
          tone === "warning" && styles.warningText,
          tone === "critical" && styles.criticalText
        ]}
      >
        {children}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    alignSelf: "flex-start",
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 999
  },
  label: {
    fontSize: 12,
    fontWeight: "700",
    textTransform: "capitalize"
  },
  stable: {
    backgroundColor: "rgba(34, 102, 70, 0.12)"
  },
  warning: {
    backgroundColor: "rgba(145, 95, 0, 0.12)"
  },
  critical: {
    backgroundColor: "rgba(142, 47, 39, 0.12)"
  },
  stableText: {
    color: colors.stable
  },
  warningText: {
    color: colors.warning
  },
  criticalText: {
    color: colors.critical
  }
});
