import type { PropsWithChildren } from "react";
import { Platform, StyleSheet, Text, View } from "react-native";
import { colors, spacing, typography } from "../theme";

interface CardProps extends PropsWithChildren {
  title: string;
  description?: string;
}

export function Card({ title, description, children }: CardProps) {
  return (
    <View style={styles.card}>
      <View style={styles.cardGlow} />
      <View style={styles.header}>
        <View style={styles.accentWrap}>
          <View style={styles.accentRail}>
            <View style={styles.accent} />
          </View>
        </View>
        <Text style={styles.title}>{title}</Text>
        {description ? <Text style={styles.description}>{description}</Text> : null}
      </View>
      <View style={styles.body}>{children}</View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    overflow: "hidden",
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 22,
    padding: spacing.lg,
    ...(Platform.OS === "web"
      ? {
          boxShadow: "0px 20px 40px rgba(17, 35, 58, 0.08)"
        }
      : {
          shadowColor: "#10233a",
          shadowOpacity: 0.08,
          shadowRadius: 22,
          shadowOffset: { width: 0, height: 12 },
          elevation: 4
        })
  },
  cardGlow: {
    position: "absolute",
    top: -24,
    right: -18,
    width: 180,
    height: 180,
    borderRadius: 999,
    backgroundColor: "rgba(158, 194, 232, 0.14)"
  },
  header: {
    gap: spacing.xs
  },
  accentWrap: {
    marginBottom: spacing.xs
  },
  accentRail: {
    width: 72,
    height: 6,
    borderRadius: 999,
    backgroundColor: colors.surfaceMuted,
    overflow: "hidden"
  },
  accent: {
    width: 40,
    height: 6,
    borderRadius: 999,
    backgroundColor: colors.copper
  },
  title: {
    fontSize: 18,
    fontWeight: "700",
    color: colors.text,
    letterSpacing: -0.3,
    fontFamily: typography.display
  },
  description: {
    fontSize: 13,
    lineHeight: 18,
    color: colors.muted,
    fontFamily: typography.body
  },
  body: {
    marginTop: spacing.md,
    gap: spacing.sm,
    position: "relative",
    zIndex: 1
  }
});
