import { Ionicons } from "@expo/vector-icons";
import { useMemo, useState } from "react";
import { Image, Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import * as ImagePicker from "expo-image-picker";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { appConfig } from "../config";
import { useSession } from "../context/SessionContext";
import { useClientData } from "../hooks/useClientData";
import { initiateStepUp, verifyStepUp } from "../services/authApi";
import {
  submitClientKycRefresh,
  updateClientProfile,
  uploadClientProfileMedia,
  type ClientMediaAsset
} from "../services/clientChannelApi";
import { colors, spacing, typography } from "../theme";

type VerificationPurpose = "PROFILE_UPDATE" | "KYC_REFRESH" | "PROFILE_MEDIA_UPLOAD";
type MediaType = "PROFILE_PHOTO" | "SIGNATURE" | "ID_CARD";
type MediaSide = "FRONT" | "BACK";

export function ProfileScreen() {
  const { user } = useSession();
  const { profile, kycOverview, isLoading, permissionWarnings, reload } = useClientData(user);

  const [name, setName] = useState(profile?.name ?? "");
  const [email, setEmail] = useState(profile?.email ?? "");
  const [phone, setPhone] = useState(profile?.phone ?? "");
  const [digitalAddress, setDigitalAddress] = useState(profile?.digitalAddress ?? "");
  const [stepUpCode, setStepUpCode] = useState("");
  const [stepUpChallengeToken, setStepUpChallengeToken] = useState<string | null>(null);
  const [stepUpToken, setStepUpToken] = useState<string | null>(null);
  const [verificationPurpose, setVerificationPurpose] = useState<VerificationPurpose | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [kycReason, setKycReason] = useState("Identity documents need review");
  const [kycSummary, setKycSummary] = useState("I have updated my identity details and want my KYC record reviewed.");

  const mediaSlots: Array<{ label: string; asset?: ClientMediaAsset | null; mediaType: MediaType; mediaSide?: MediaSide }> = [
    { label: "Profile photo", asset: profile?.profilePhoto, mediaType: "PROFILE_PHOTO" },
    { label: "Signature", asset: profile?.signature, mediaType: "SIGNATURE" },
    { label: "ID front", asset: profile?.idCardFront, mediaType: "ID_CARD", mediaSide: "FRONT" },
    { label: "ID back", asset: profile?.idCardBack, mediaType: "ID_CARD", mediaSide: "BACK" }
  ];

  const readinessChecklist = kycOverview?.readiness.checklist ?? [];
  const satisfiedCount = readinessChecklist.filter((item) => item.isSatisfied).length;
  const latestCase = kycOverview?.cases[0] ?? null;

  const activePurposeLabel = verificationPurpose
    ? {
        PROFILE_UPDATE: "Profile update",
        KYC_REFRESH: "KYC refresh",
        PROFILE_MEDIA_UPLOAD: "Media upload"
      }[verificationPurpose]
    : null;

  const activePurposeDescription = useMemo(() => {
    switch (verificationPurpose) {
      case "PROFILE_UPDATE":
        return "Use this verification to save contact and identity-profile changes.";
      case "KYC_REFRESH":
        return "Use this verification to send a KYC review request.";
      case "PROFILE_MEDIA_UPLOAD":
        return "Use this verification to upload KYC evidence files.";
      default:
        return "Start verification before any sensitive action.";
    }
  }, [verificationPurpose]);

  async function startVerification(purpose: VerificationPurpose) {
    setIsSubmitting(true);
    setFeedback(null);
    setVerificationPurpose(purpose);
    setStepUpToken(null);

    try {
      const challenge = await initiateStepUp({ purpose });
      setStepUpChallengeToken(challenge.challengeToken);
      setFeedback(
        appConfig.showDevOtp && challenge.debugCode
          ? `Verification code sent to ${challenge.deliveryHint}. Dev code: ${challenge.debugCode}`
          : `Verification code sent to ${challenge.deliveryHint}.`
      );
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to start verification.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function completeVerification() {
    if (!stepUpChallengeToken) {
      setFeedback("Start verification first.");
      return;
    }

    setIsSubmitting(true);
    setFeedback(null);
    try {
      const verified = await verifyStepUp({ challengeToken: stepUpChallengeToken, code: stepUpCode.trim() });
      setStepUpToken(verified.stepUpToken);
      setStepUpCode("");
      setVerificationPurpose((verified.purpose as VerificationPurpose) ?? verificationPurpose);
      setFeedback("Verification approved. You can now complete the protected action.");
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to verify the code.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitProfileUpdate() {
    if (!stepUpToken || verificationPurpose !== "PROFILE_UPDATE") {
      setFeedback("Complete profile verification before saving changes.");
      return;
    }

    setIsSubmitting(true);
    setFeedback(null);
    try {
      const updated = await updateClientProfile({ name, email, phone, digitalAddress, stepUpToken });
      setFeedback(`Profile updated for ${updated.name}.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to update profile.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitKycRefresh() {
    if (!stepUpToken || verificationPurpose !== "KYC_REFRESH") {
      setFeedback("Complete KYC verification before submitting the review request.");
      return;
    }

    setIsSubmitting(true);
    setFeedback(null);
    try {
      const submitted = await submitClientKycRefresh({ reason: kycReason, summary: kycSummary, stepUpToken });
      setFeedback(`KYC refresh ${submitted.reference} submitted for review.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to submit KYC refresh.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function uploadEvidence(mediaType: MediaType, mediaSide?: MediaSide) {
    if (!stepUpToken || verificationPurpose !== "PROFILE_MEDIA_UPLOAD") {
      setFeedback("Verify for media upload before sending KYC evidence.");
      return;
    }

    setIsSubmitting(true);
    setFeedback(null);
    try {
      const selected = await pickEvidenceFile(mediaType, mediaSide);
      if (!selected) {
        setFeedback("Evidence selection was cancelled.");
        return;
      }

      const uploaded = await uploadClientProfileMedia({
        mediaType,
        mediaSide,
        fileName: selected.fileName,
        contentType: selected.contentType,
        dataUrl: selected.dataUrl,
        stepUpToken
      });

      setFeedback(`${labelMedia(uploaded)} uploaded and queued for review.`);
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to upload media.");
    } finally {
      setIsSubmitting(false);
    }
  }

  function clearProtectedAction() {
    setStepUpToken(null);
    setStepUpChallengeToken(null);
    setStepUpCode("");
    setVerificationPurpose(null);
  }

  return (
    <Screen>
      <Card title="Profile and KYC" description="Identity, readiness, and review">
        <View style={styles.hero}>
          <View style={styles.heroIconWrap}>
            <Ionicons name="person-circle-outline" size={28} color={colors.goldSoft} />
          </View>
          <Text style={styles.eyebrow}>Identity</Text>
          <Text style={styles.heroTitle}>Manage identity with less friction.</Text>
          <Text style={styles.heroCopy}>Profile, evidence, review.</Text>
          <View style={styles.heroMetrics}>
            <View style={styles.heroMetricTile}>
              <Ionicons name="layers-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.heroMetricValue}>{profile?.kycLevel ?? "Unknown"}</Text>
              <Text style={styles.heroMetricLabel}>kyc level</Text>
            </View>
            <View style={styles.heroMetricTile}>
              <Ionicons name="checkmark-done-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.heroMetricValue}>{satisfiedCount}/{readinessChecklist.length || 0}</Text>
              <Text style={styles.heroMetricLabel}>readiness complete</Text>
            </View>
          </View>
        </View>
      </Card>

      <Card title="Verification" description="Current protected action">
        <View style={styles.statusPanel}>
          <Text style={styles.statusEyebrow}>Protected action</Text>
          <Text style={styles.statusTitle}>{activePurposeLabel ?? "No active verification"}</Text>
          <Text style={styles.meta}>{activePurposeDescription}</Text>
        </View>
        <View style={styles.verificationActions}>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} onPress={() => void startVerification("PROFILE_UPDATE")} disabled={isSubmitting}>
            <Text style={styles.secondaryActionLabel}>Verify profile update</Text>
          </Pressable>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} onPress={() => void startVerification("PROFILE_MEDIA_UPLOAD")} disabled={isSubmitting}>
            <Text style={styles.secondaryActionLabel}>Verify media upload</Text>
          </Pressable>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} onPress={() => void startVerification("KYC_REFRESH")} disabled={isSubmitting}>
            <Text style={styles.secondaryActionLabel}>Verify KYC refresh</Text>
          </Pressable>
        </View>
        {stepUpChallengeToken ? (
          <>
            <TextInput value={stepUpCode} onChangeText={setStepUpCode} placeholder="Verification code" placeholderTextColor={colors.muted} keyboardType="number-pad" style={styles.input} />
            <View style={styles.inlineActions}>
              <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} onPress={() => void completeVerification()} disabled={isSubmitting}>
                <Text style={styles.secondaryActionLabel}>{stepUpToken ? "Verified" : "Confirm verification"}</Text>
              </Pressable>
              <Pressable style={styles.ghostAction} onPress={clearProtectedAction}>
                <Text style={styles.ghostActionLabel}>Clear</Text>
              </Pressable>
            </View>
          </>
        ) : null}
        {feedback ? <Text style={styles.meta}>{feedback}</Text> : null}
      </Card>

      <Card title="Snapshot" description="Current customer record">
        {isLoading ? <Text style={styles.meta}>Loading profile...</Text> : null}
        {profile ? (
          <View style={styles.snapshotGrid}>
            <View style={styles.snapshotTile}>
              <View style={styles.snapshotHeader}>
                <Ionicons name="pulse-outline" size={18} color={colors.copper} />
                <Text style={styles.snapshotLabel}>Risk rating</Text>
              </View>
              <Text style={styles.snapshotValue}>{profile.riskRating ?? "Unknown"}</Text>
            </View>
            <View style={styles.snapshotTile}>
              <View style={styles.snapshotHeader}>
                <Ionicons name="card-outline" size={18} color={colors.copper} />
                <Text style={styles.snapshotLabel}>Ghana Card</Text>
              </View>
              <Text style={styles.snapshotValue}>{profile.ghanaCard ?? "Not provided"}</Text>
            </View>
          </View>
        ) : null}
        {permissionWarnings.map((warning) => (
          <Text key={warning} style={styles.warning}>{warning}</Text>
        ))}
      </Card>

      <Card title="KYC readiness" description="Checklist and case status">
        <Text style={styles.metaStrong}>KYC tier: {kycOverview?.kycLevel ?? profile?.kycLevel ?? "Unknown"}</Text>
        <View style={styles.checklist}>
          {readinessChecklist.length > 0 ? (
            readinessChecklist.map((item) => (
              <View key={item.key} style={styles.checkItem}>
                <View style={[styles.checkIconWrap, item.isSatisfied ? styles.checkIconWrapGood : styles.checkIconWrapPending]}>
                  <Ionicons
                    name={item.isSatisfied ? "checkmark-outline" : "alert-outline"}
                    size={18}
                    color={item.isSatisfied ? colors.stable : colors.warning}
                  />
                </View>
                <View style={styles.checkCopy}>
                  <Text style={styles.checkTitle}>{item.label}</Text>
                  <Text style={item.isSatisfied ? styles.good : styles.warning}>{item.isSatisfied ? "Ready" : "Pending"}</Text>
                  <Text style={styles.meta}>{item.detail}</Text>
                </View>
              </View>
            ))
          ) : (
            <Text style={styles.meta}>KYC readiness is temporarily unavailable in this environment.</Text>
          )}
        </View>
        {latestCase ? (
          <View style={styles.casePanel}>
            <Text style={styles.caseEyebrow}>Latest case</Text>
            <Text style={styles.caseTitle}>{latestCase.reference} ({latestCase.status})</Text>
            {latestCase.decisionNote ? <Text style={styles.meta}>Reviewer note: {latestCase.decisionNote}</Text> : null}
          </View>
        ) : (
          <Text style={styles.meta}>No KYC refresh case is currently in progress.</Text>
        )}
      </Card>

      <Card title="Update details" description="Contact profile changes">
        <TextInput value={name} onChangeText={setName} placeholder="Full name" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={email} onChangeText={setEmail} placeholder="Email" placeholderTextColor={colors.muted} autoCapitalize="none" style={styles.input} />
        <TextInput value={phone} onChangeText={setPhone} placeholder="Phone" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={digitalAddress} onChangeText={setDigitalAddress} placeholder="Digital address" placeholderTextColor={colors.muted} style={styles.input} />
        <Pressable style={[styles.action, isSubmitting && styles.disabled]} onPress={() => void submitProfileUpdate()} disabled={isSubmitting}>
          <Text style={styles.actionLabel}>{isSubmitting ? "Updating..." : "Save profile changes"}</Text>
        </Pressable>
      </Card>

      <Card title="Evidence" description="Upload and review status">
        <View style={styles.guidancePanel}>
          <Text style={styles.metaStrong}>Recommended evidence</Text>
          <Text style={styles.meta}>Use clear, uncropped images with readable text and minimal glare.</Text>
          <Text style={styles.meta}>Complete media verification once, then upload each required item below.</Text>
        </View>
        {mediaSlots.map((slot) => (
          <Pressable key={`${slot.mediaType}-${slot.mediaSide ?? "NONE"}`} style={[styles.mediaRow, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void uploadEvidence(slot.mediaType, slot.mediaSide)}>
            <View style={styles.mediaRowTop}>
              <View style={styles.mediaIconWrap}>
                <Ionicons
                  name={
                    slot.mediaType === "PROFILE_PHOTO"
                      ? "person-outline"
                      : slot.mediaType === "SIGNATURE"
                        ? "create-outline"
                        : "id-card-outline"
                  }
                  size={20}
                  color={colors.forest}
                />
              </View>
              <View style={styles.mediaCopy}>
                <Text style={styles.mediaLabel}>{slot.label}</Text>
                <Text style={slot.asset ? styles.good : styles.warning}>{slot.asset ? `${slot.asset.status} | ${slot.asset.fileName}` : "Missing"}</Text>
                {slot.asset?.uploadedAt ? <Text style={styles.metaSmall}>Updated {new Date(slot.asset.uploadedAt).toLocaleString()}</Text> : null}
              </View>
              {slot.asset?.previewUrl ? (
                <Image source={{ uri: slot.asset.previewUrl }} style={styles.thumbnail} resizeMode="cover" />
              ) : (
                <View style={styles.thumbnailPlaceholder}>
                  <Text style={styles.thumbnailPlaceholderLabel}>No file</Text>
                </View>
              )}
            </View>
          </Pressable>
        ))}
      </Card>

      <Card title="KYC refresh" description="Submit for review">
        <TextInput value={kycReason} onChangeText={setKycReason} placeholder="Reason" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={kycSummary} onChangeText={setKycSummary} placeholder="Summary" placeholderTextColor={colors.muted} multiline style={[styles.input, styles.multilineInput]} />
        <Pressable style={[styles.action, isSubmitting && styles.disabled]} onPress={() => void submitKycRefresh()} disabled={isSubmitting}>
          <Text style={styles.actionLabel}>{isSubmitting ? "Submitting..." : "Submit KYC refresh"}</Text>
        </Pressable>
      </Card>

      {latestCase ? (
        <Card title="Case timeline" description="Latest review milestones">
          {latestCase.events.map((event) => (
            <View key={event.id} style={styles.timelineItem}>
              <View style={styles.timelineDot} />
              <View style={styles.timelineCopy}>
                <Text style={styles.timelineTitle}>{event.title}</Text>
                <Text style={styles.meta}>{event.description}</Text>
                <Text style={styles.metaSmall}>
                  {new Date(event.createdAt).toLocaleString()}
                  {event.actorName ? ` | ${event.actorName}` : ""}
                </Text>
              </View>
            </View>
          ))}
        </Card>
      ) : null}
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
  heroMetrics: {
    flexDirection: "row",
    gap: spacing.sm
  },
  heroMetricTile: {
    flex: 1,
    borderRadius: 18,
    padding: spacing.md,
    backgroundColor: "rgba(245, 238, 229, 0.08)",
    borderWidth: 1,
    borderColor: "rgba(245, 238, 229, 0.12)",
    gap: 4
  },
  heroMetricValue: {
    color: colors.inkInverse,
    fontSize: 20,
    fontWeight: "800",
    fontFamily: typography.display
  },
  heroMetricLabel: {
    color: "rgba(245, 238, 229, 0.72)",
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 0.8,
    fontFamily: typography.body
  },
  statusPanel: {
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceMuted,
    padding: spacing.md,
    gap: 4
  },
  statusEyebrow: {
    color: colors.copper,
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 0.8,
    fontWeight: "700"
  },
  statusTitle: {
    color: colors.text,
    fontSize: 18,
    fontWeight: "700"
  },
  verificationActions: {
    gap: spacing.sm
  },
  inlineActions: {
    gap: spacing.sm
  },
  snapshotGrid: {
    gap: spacing.sm
  },
  snapshotTile: {
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceSoft,
    padding: spacing.md,
    gap: 4
  },
  snapshotHeader: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm
  },
  snapshotLabel: {
    color: colors.muted,
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 0.8
  },
  snapshotValue: {
    color: colors.text,
    fontWeight: "700",
    lineHeight: 21
  },
  checklist: {
    gap: spacing.sm
  },
  checkItem: {
    flexDirection: "row",
    gap: spacing.md,
    alignItems: "flex-start",
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceSoft,
    padding: spacing.md
  },
  checkIconWrap: {
    width: 36,
    height: 36,
    borderRadius: 12,
    alignItems: "center",
    justifyContent: "center",
    borderWidth: 1
  },
  checkIconWrapGood: {
    backgroundColor: "rgba(46, 106, 74, 0.08)",
    borderColor: "rgba(46, 106, 74, 0.20)"
  },
  checkIconWrapPending: {
    backgroundColor: "rgba(179, 124, 38, 0.10)",
    borderColor: "rgba(179, 124, 38, 0.22)"
  },
  checkCopy: {
    flex: 1,
    gap: 4
  },
  checkTitle: {
    color: colors.textSoft,
    fontWeight: "700"
  },
  casePanel: {
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceMuted,
    padding: spacing.md,
    gap: 4
  },
  caseEyebrow: {
    color: colors.copper,
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: 0.8,
    fontWeight: "700"
  },
  caseTitle: {
    color: colors.text,
    fontWeight: "700"
  },
  meta: {
    color: colors.muted,
    lineHeight: 21
  },
  metaStrong: {
    color: colors.textSoft,
    lineHeight: 21,
    fontWeight: "700"
  },
  metaSmall: {
    color: colors.muted,
    fontSize: 12,
    lineHeight: 18
  },
  warning: {
    color: colors.warning
  },
  good: {
    color: colors.stable
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
  multilineInput: {
    minHeight: 88,
    textAlignVertical: "top"
  },
  guidancePanel: {
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceMuted,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    gap: 4
  },
  mediaRow: {
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceSoft,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md
  },
  mediaRowTop: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md
  },
  mediaIconWrap: {
    width: 42,
    height: 42,
    borderRadius: 14,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  mediaCopy: {
    flex: 1,
    gap: 4
  },
  mediaLabel: {
    color: colors.textSoft,
    fontWeight: "700"
  },
  thumbnail: {
    width: 64,
    height: 64,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    backgroundColor: colors.surface
  },
  thumbnailPlaceholder: {
    width: 64,
    height: 64,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceMuted,
    alignItems: "center",
    justifyContent: "center"
  },
  thumbnailPlaceholderLabel: {
    color: colors.muted,
    fontSize: 12,
    fontWeight: "700"
  },
  timelineItem: {
    flexDirection: "row",
    alignItems: "flex-start",
    gap: spacing.md
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
  timelineTitle: {
    color: colors.textSoft,
    fontWeight: "700"
  },
  action: {
    backgroundColor: colors.forest,
    borderRadius: 999,
    paddingVertical: 15,
    paddingHorizontal: spacing.lg
  },
  disabled: {
    opacity: 0.65
  },
  secondaryAction: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.forest,
    paddingVertical: 13,
    paddingHorizontal: spacing.lg
  },
  secondaryActionLabel: {
    color: colors.forest,
    textAlign: "center",
    fontWeight: "700"
  },
  ghostAction: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    paddingVertical: 13,
    paddingHorizontal: spacing.lg
  },
  ghostActionLabel: {
    color: colors.textSoft,
    textAlign: "center",
    fontWeight: "700"
  },
  actionLabel: {
    color: colors.white,
    textAlign: "center",
    fontWeight: "700"
  }
});

function labelMedia(asset: ClientMediaAsset) {
  if (asset.mediaType === "ID_CARD" && asset.mediaSide) {
    return `ID card ${asset.mediaSide.toLowerCase()}`;
  }

  return asset.mediaType.toLowerCase().replace("_", " ");
}

async function pickEvidenceFile(mediaType: MediaType, mediaSide?: MediaSide) {
  const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
  if (!permission.granted) {
    throw new Error("Photo library access is required to upload KYC evidence.");
  }

  const result = await ImagePicker.launchImageLibraryAsync({
    mediaTypes: ["images"],
    allowsEditing: false,
    quality: 0.85,
    base64: true
  });

  if (result.canceled || !result.assets.length) {
    return null;
  }

  const asset = result.assets[0];
  if (!asset.base64) {
    throw new Error("The selected image could not be read for upload.");
  }

  const contentType = asset.mimeType ?? "image/jpeg";
  const fileExtension = contentType.split("/")[1] ?? "jpg";
  const fileName = asset.fileName ?? `${mediaType.toLowerCase()}${mediaSide ? `-${mediaSide.toLowerCase()}` : ""}.${fileExtension}`;

  return {
    fileName,
    contentType,
    dataUrl: `data:${contentType};base64,${asset.base64}`
  };
}
