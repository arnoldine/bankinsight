import { Ionicons } from "@expo/vector-icons";
import { useMemo, useState } from "react";
import { Linking, Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { useSession } from "../context/SessionContext";
import { useClientData } from "../hooks/useClientData";
import { createClientComplaint, getClientComplaint, reopenClientComplaint } from "../services/clientChannelApi";
import { colors, spacing, typography } from "../theme";

export function ComplaintsScreen() {
  const { user } = useSession();
  const { complaints, isLoading, permissionWarnings, reload } = useClientData(user);
  const [category, setCategory] = useState("Unauthorized access concern");
  const [summary, setSummary] = useState("I need help reviewing recent account access.");
  const [details, setDetails] = useState("Please review recent device activity and confirm whether my account was accessed by an unrecognized session.");
  const [selectedComplaintId, setSelectedComplaintId] = useState<string | null>(null);
  const [selectedComplaintDetail, setSelectedComplaintDetail] = useState<any | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const openCount = useMemo(() => complaints.filter((item) => item.status !== "CLOSED").length, [complaints]);

  async function submitComplaint() {
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const complaint = await createClientComplaint({ category: category.trim(), summary: summary.trim(), details: details.trim() });
      setFeedback(`Complaint ${complaint.reference} was created successfully.`);
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to create complaint.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function viewComplaint(complaintId: string) {
    setSelectedComplaintId(complaintId);
    try {
      const detail = await getClientComplaint(complaintId);
      setSelectedComplaintDetail(detail);
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to load complaint details.");
    }
  }

  async function reopenComplaint() {
    if (!selectedComplaintId) return;
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const reopened = await reopenClientComplaint(selectedComplaintId, {
        reason: "I still need support on this case and require another review."
      });
      setSelectedComplaintDetail(reopened);
      setFeedback(`Complaint ${reopened.reference} was reopened.`);
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to reopen complaint.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function openAttachment(url?: string | null) {
    if (!url) {
      setFeedback("This attachment is not available for download yet.");
      return;
    }
    try {
      await Linking.openURL(url);
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to open this attachment.");
    }
  }

  return (
    <Screen>
      <Card title="Complaints" description="Recourse and case tracking">
        <View style={styles.hero}>
          <View style={styles.heroIconWrap}>
            <Ionicons name="chatbubbles-outline" size={28} color={colors.goldSoft} />
          </View>
          <Text style={styles.eyebrow}>Recourse</Text>
          <Text style={styles.heroTitle}>Track support cases clearly.</Text>
          <Text style={styles.heroCopy}>Submit, review, reopen.</Text>
          <View style={styles.metrics}>
            <View style={styles.metricTile}>
              <Ionicons name="documents-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.metricValue}>{complaints.length}</Text>
              <Text style={styles.metricLabel}>total cases</Text>
            </View>
            <View style={styles.metricTile}>
              <Ionicons name="time-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.metricValue}>{openCount}</Text>
              <Text style={styles.metricLabel}>still active</Text>
            </View>
          </View>
        </View>
      </Card>

      <Card title="Case queue" description="Recent complaints">
        {isLoading ? <Text style={styles.meta}>Loading complaints...</Text> : null}
        {!isLoading && complaints.length === 0 ? <Text style={styles.meta}>No complaints are available for the linked customer record yet.</Text> : null}
        {complaints.map((item) => (
          <Pressable key={item.reference} style={styles.item} onPress={() => void viewComplaint(item.id)}>
            <View style={styles.itemTop}>
              <View style={styles.itemIconWrap}>
                <Ionicons name="chatbox-ellipses-outline" size={20} color={colors.forest} />
              </View>
              <View style={styles.itemCopy}>
                <Text style={styles.reference}>{item.reference}</Text>
                <Text style={styles.category}>{item.summary}</Text>
              </View>
              <View style={[styles.statusPill, item.status === "CLOSED" ? styles.statusClosed : styles.statusOpen]}>
                <Text style={[styles.statusLabel, item.status === "CLOSED" ? styles.statusLabelClosed : styles.statusLabelOpen]}>{item.status}</Text>
              </View>
            </View>
            <Text style={styles.updated}>Updated {new Date(item.updatedAt).toLocaleString()}</Text>
            <Text style={styles.link}>Open timeline</Text>
          </Pressable>
        ))}
        {permissionWarnings.map((warning) => (
          <Text key={warning} style={styles.warning}>
            {warning}
          </Text>
        ))}
      </Card>

      {selectedComplaintDetail ? (
        <Card title={selectedComplaintDetail.reference} description={`${selectedComplaintDetail.status} • ${selectedComplaintDetail.events?.length ?? 0} events`}>
          {(selectedComplaintDetail.events ?? []).map((event: any) => (
            <View key={event.id} style={styles.timelineItem}>
              <View style={styles.timelineDot} />
              <View style={styles.timelineCopy}>
                <Text style={styles.reference}>{event.title}</Text>
                <Text style={styles.category}>{event.description}</Text>
                <Text style={styles.updated}>{new Date(event.createdAt).toLocaleString()}</Text>
              </View>
            </View>
          ))}
          {(selectedComplaintDetail.attachments ?? []).length > 0 ? (
            <View style={styles.attachmentsSection}>
              <Text style={styles.reference}>Evidence</Text>
              {(selectedComplaintDetail.attachments ?? []).map((attachment: any) => (
                <View key={attachment.id} style={styles.attachmentItem}>
                  <View style={styles.attachmentIconWrap}>
                    <Ionicons
                      name={attachment.fileName?.toLowerCase().endsWith(".pdf") ? "document-outline" : "image-outline"}
                      size={18}
                      color={colors.forest}
                    />
                  </View>
                  <View style={styles.attachmentCopy}>
                    <Text style={styles.category}>{attachment.fileName}</Text>
                    <Text style={styles.updated}>{attachment.status} | {new Date(attachment.uploadedAt).toLocaleString()}</Text>
                  </View>
                  <Pressable onPress={() => void openAttachment(attachment.contentUrl)}>
                    <Text style={styles.link}>Open file</Text>
                  </Pressable>
                </View>
              ))}
            </View>
          ) : null}
          {selectedComplaintDetail.status === "CLOSED" ? (
            <Pressable style={[styles.secondaryAction, isSubmitting && styles.actionDisabled]} onPress={() => void reopenComplaint()} disabled={isSubmitting}>
              <Text style={styles.secondaryActionLabel}>{isSubmitting ? "Reopening..." : "Reopen complaint"}</Text>
            </Pressable>
          ) : null}
        </Card>
      ) : null}

      <Card title="Create complaint" description="Submit a new case">
        <TextInput value={category} onChangeText={setCategory} placeholder="Category" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={summary} onChangeText={setSummary} placeholder="Summary" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={details} onChangeText={setDetails} placeholder="Details" placeholderTextColor={colors.muted} multiline style={[styles.input, styles.textArea]} />
        {feedback ? <Text style={styles.note}>{feedback}</Text> : null}
        <Pressable style={[styles.action, isSubmitting && styles.actionDisabled]} onPress={() => void submitComplaint()} disabled={isSubmitting}>
          <Text style={styles.actionLabel}>{isSubmitting ? "Submitting..." : "Submit complaint"}</Text>
        </Pressable>
      </Card>
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
    letterSpacing: 1,
    fontFamily: typography.body
  },
  heroTitle: {
    color: colors.inkInverse,
    fontSize: 26,
    lineHeight: 30,
    fontWeight: "800",
    letterSpacing: -0.8,
    fontFamily: typography.display
  },
  heroCopy: {
    color: "rgba(245, 248, 252, 0.76)",
    lineHeight: 18,
    fontSize: 13,
    fontFamily: typography.body
  },
  metrics: {
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
    fontWeight: "800",
    fontFamily: typography.display
  },
  metricLabel: {
    color: "rgba(245, 238, 229, 0.72)",
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 0.8,
    fontFamily: typography.body
  },
  item: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 18,
    padding: spacing.md,
    backgroundColor: colors.surfaceSoft,
    gap: 6
  },
  itemTop: {
    flexDirection: "row",
    gap: spacing.md,
    alignItems: "flex-start"
  },
  itemIconWrap: {
    width: 42,
    height: 42,
    borderRadius: 14,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  itemCopy: {
    flex: 1,
    gap: 4
  },
  reference: {
    fontWeight: "700",
    color: colors.text,
    fontFamily: typography.display
  },
  category: {
    color: colors.text,
    lineHeight: 19,
    fontFamily: typography.body
  },
  statusPill: {
    borderRadius: 999,
    borderWidth: 1,
    paddingHorizontal: spacing.sm,
    paddingVertical: 8
  },
  statusOpen: {
    backgroundColor: "#f3e4d7",
    borderColor: colors.copperSoft
  },
  statusClosed: {
    backgroundColor: "rgba(46, 106, 74, 0.08)",
    borderColor: "rgba(46, 106, 74, 0.20)"
  },
  statusLabel: {
    fontSize: 11,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.8
  },
  statusLabelOpen: {
    color: colors.copper
  },
  statusLabelClosed: {
    color: colors.stable
  },
  updated: {
    color: colors.muted,
    fontFamily: typography.body
  },
  note: {
    color: colors.muted,
    lineHeight: 20,
    fontFamily: typography.body
  },
  warning: {
    color: colors.warning,
    fontFamily: typography.body
  },
  link: {
    color: colors.forest,
    fontWeight: "700",
    fontFamily: typography.body
  },
  timelineItem: {
    flexDirection: "row",
    gap: spacing.md,
    alignItems: "flex-start"
  },
  timelineDot: {
    width: 12,
    height: 12,
    borderRadius: 999,
    marginTop: 6,
    backgroundColor: colors.gold
  },
  timelineCopy: {
    flex: 1,
    gap: 4
  },
  attachmentsSection: {
    gap: spacing.sm
  },
  attachmentItem: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceSoft,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm
  },
  attachmentIconWrap: {
    width: 36,
    height: 36,
    borderRadius: 12,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  attachmentCopy: {
    flex: 1,
    gap: 4
  },
  input: {
    backgroundColor: colors.surfaceSoft,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: 14,
    color: colors.text
  },
  textArea: {
    minHeight: 120,
    textAlignVertical: "top"
  },
  action: {
    backgroundColor: colors.forest,
    borderRadius: 999,
    paddingVertical: 15,
    paddingHorizontal: spacing.lg
  },
  actionDisabled: {
    opacity: 0.65
  },
  actionLabel: {
    color: colors.white,
    textAlign: "center",
    fontWeight: "700",
    fontFamily: typography.body
  },
  secondaryAction: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.forest,
    paddingVertical: 15,
    paddingHorizontal: spacing.lg
  },
  secondaryActionLabel: {
    color: colors.forest,
    textAlign: "center",
    fontWeight: "700",
    fontFamily: typography.body
  },
  meta: {
    color: colors.muted,
    fontFamily: typography.body
  }
});
