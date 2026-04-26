import { StyleSheet, Text, View } from "react-native";
import { appConfig } from "../config";
import { colors, spacing } from "../theme";

export function DevModeBanner() {
  if (!appConfig.showDevOtp) {
    return null;
  }

  return (
    <View style={styles.container}>
      <Text style={styles.label}>Development mode</Text>
      <Text style={styles.copy}>
        Debug OTP display is enabled for this preview. API target: {appConfig.apiBaseUrl}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: "#f4ecd2",
    borderBottomWidth: 1,
    borderBottomColor: "#e2cf88",
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm
  },
  label: {
    color: colors.forest,
    fontSize: 12,
    fontWeight: "700",
    letterSpacing: 1,
    textTransform: "uppercase"
  },
  copy: {
    color: colors.textSoft,
    fontSize: 13,
    lineHeight: 18,
    marginTop: 2
  }
});
