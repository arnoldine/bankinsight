import type { PropsWithChildren } from "react";
import { Platform, ScrollView, StyleSheet, View } from "react-native";
import { LinearGradient } from "expo-linear-gradient";
import { SafeAreaView } from "react-native-safe-area-context";
import { colors, spacing } from "../theme";

export function Screen({ children }: PropsWithChildren) {
  return (
    <LinearGradient colors={[colors.backgroundTop, colors.backgroundBottom]} style={styles.gradient}>
      <View style={styles.orbPrimary} />
      <View style={styles.orbSecondary} />
      <View style={styles.orbTertiary} />
      <View style={styles.veil} />
      <SafeAreaView style={styles.safeArea}>
        <ScrollView
          contentContainerStyle={styles.content}
          showsVerticalScrollIndicator={false}
          contentInsetAdjustmentBehavior="automatic"
        >
          <View style={styles.stack}>{children}</View>
        </ScrollView>
      </SafeAreaView>
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  gradient: {
    flex: 1
  },
  orbPrimary: {
    position: "absolute",
    top: -88,
    right: -42,
    width: 300,
    height: 300,
    borderRadius: 999,
    backgroundColor: "rgba(46, 111, 179, 0.14)"
  },
  orbSecondary: {
    position: "absolute",
    bottom: 72,
    left: -70,
    width: 260,
    height: 260,
    borderRadius: 999,
    backgroundColor: "rgba(19, 49, 79, 0.10)"
  },
  orbTertiary: {
    position: "absolute",
    top: "34%",
    left: "45%",
    width: 220,
    height: 220,
    borderRadius: 999,
    backgroundColor: "rgba(78, 134, 197, 0.06)"
  },
  veil: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: colors.backgroundVeil
  },
  safeArea: {
    flex: 1
  },
  content: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
    paddingBottom: spacing.xxl,
    ...(Platform.OS === "web"
      ? {
          maxWidth: 920,
          width: "100%",
          alignSelf: "center"
        }
      : null)
  },
  stack: {
    gap: spacing.md
  }
});
