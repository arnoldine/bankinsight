import type { PropsWithChildren } from "react";
import { Platform, StyleSheet, Text, View } from "react-native";
import { colors, typography } from "../theme";

export function DevicePreviewFrame({ children }: PropsWithChildren) {
  if (Platform.OS !== "web") {
    return <>{children}</>;
  }

  return (
    <View style={styles.page}>
      <View style={styles.device}>
        <View style={styles.notch} />
        <View style={styles.screen}>
          {children}
        </View>
      </View>
      <Text style={styles.caption}>iPhone 14 Pro Max preview</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  page: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    paddingVertical: 16,
    paddingHorizontal: 16
  },
  device: {
    width: "100%",
    maxWidth: 430,
    aspectRatio: 430 / 932,
    maxHeight: 932,
    borderRadius: 42,
    backgroundColor: "#0a0f16",
    padding: 10,
    boxShadow: "0px 30px 80px rgba(15, 24, 36, 0.22)",
    position: "relative"
  },
  notch: {
    position: "absolute",
    top: 14,
    left: "50%",
    marginLeft: -70,
    width: 140,
    height: 30,
    borderRadius: 18,
    backgroundColor: "#0a0f16",
    zIndex: 2
  },
  screen: {
    flex: 1,
    overflow: "hidden",
    borderRadius: 34,
    backgroundColor: colors.backgroundTop
  },
  caption: {
    marginTop: 12,
    color: colors.muted,
    fontSize: 12,
    letterSpacing: 0.8,
    textTransform: "uppercase",
    fontFamily: typography.body
  }
});
