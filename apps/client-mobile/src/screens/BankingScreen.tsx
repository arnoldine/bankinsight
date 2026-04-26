import { Ionicons } from "@expo/vector-icons";
import { useEffect, useMemo, useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";
import { Card } from "../components/Card";
import { Screen } from "../components/Screen";
import { useSession } from "../context/SessionContext";
import { useClientData } from "../hooks/useClientData";
import { initiateStepUp, verifyStepUp } from "../services/authApi";
import {
  createClientFixedDeposit,
  createClientInternalTransfer,
  createClientLoanApplication,
  createClientMerchantPayment,
  createClientMerchantProfile,
  createClientQrPayment,
  createClientStandingOrder,
  resolveClientQrPayment,
  updateClientStandingOrderStatus,
  type ClientQrPaymentPreview
} from "../services/clientChannelApi";
import { colors, spacing, typography } from "../theme";

type ProtectedPurpose =
  | "TRANSFER_INTERNAL"
  | "MERCHANT_PAYMENT"
  | "MERCHANT_PROFILE_ENROLLMENT"
  | "STANDING_ORDER"
  | "INVESTMENT_FIXED_DEPOSIT"
  | "LOAN_APPLICATION";

export function BankingScreen() {
  const { user } = useSession();
  const { accounts, merchants, merchantAcceptanceEligibility, merchantProfiles, standingOrders, fixedDeposits, loans, loanProducts, bankingOverview, reload } =
    useClientData(user);

  const liquidAccounts = useMemo(
    () => accounts.filter((account) => account.type !== "FIXED_DEPOSIT" && account.status === "ACTIVE"),
    [accounts]
  );

  const [feedback, setFeedback] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [challengeToken, setChallengeToken] = useState<string | null>(null);
  const [stepUpToken, setStepUpToken] = useState<string | null>(null);
  const [purpose, setPurpose] = useState<ProtectedPurpose | null>(null);
  const [authFactor, setAuthFactor] = useState<"otp" | "pin">(user?.hasTransactionPin ? "pin" : "otp");
  const [challengeFactor, setChallengeFactor] = useState<"otp" | "pin">("otp");
  const [otpCode, setOtpCode] = useState("");

  const [fromAccountId, setFromAccountId] = useState(liquidAccounts[0]?.id ?? "");
  const [toAccountId, setToAccountId] = useState(liquidAccounts[1]?.id ?? liquidAccounts[0]?.id ?? "");
  const [transferAmount, setTransferAmount] = useState("150");
  const [transferNarration, setTransferNarration] = useState("Monthly savings sweep");
  const [merchantCode, setMerchantCode] = useState(merchants[0]?.code ?? "ECG");
  const [merchantSourceAccountId, setMerchantSourceAccountId] = useState(liquidAccounts[0]?.id ?? "");
  const [merchantAmount, setMerchantAmount] = useState("85");
  const [merchantNarration, setMerchantNarration] = useState("Merchant settlement");
  const [merchantDisplayName, setMerchantDisplayName] = useState("");
  const [merchantCategory, setMerchantCategory] = useState("Retail");
  const [merchantSettlementAccountId, setMerchantSettlementAccountId] = useState(liquidAccounts[0]?.id ?? "");
  const [qrPayload, setQrPayload] = useState("");
  const [qrSourceAccountId, setQrSourceAccountId] = useState(liquidAccounts[0]?.id ?? "");
  const [qrAmount, setQrAmount] = useState("25");
  const [qrNarration, setQrNarration] = useState("QR merchant payment");
  const [qrPreview, setQrPreview] = useState<ClientQrPaymentPreview | null>(null);
  const [standingOrderFrequency, setStandingOrderFrequency] = useState("MONTHLY");
  const [standingOrderNarration, setStandingOrderNarration] = useState("Scheduled merchant payment");
  const [standingOrderAmount, setStandingOrderAmount] = useState("50");
  const [investmentSourceAccountId, setInvestmentSourceAccountId] = useState(liquidAccounts[0]?.id ?? "");
  const [investmentPrincipal, setInvestmentPrincipal] = useState("1000");
  const [investmentRate, setInvestmentRate] = useState("12");
  const [investmentTenureDays, setInvestmentTenureDays] = useState("90");
  const [loanProductId, setLoanProductId] = useState(loanProducts[0]?.id ?? "");
  const [loanAmount, setLoanAmount] = useState("2500");
  const [loanServicingAccountId, setLoanServicingAccountId] = useState(liquidAccounts[0]?.id ?? "");
  const activeStandingOrders = standingOrders.filter((order) => order.status === "ACTIVE").length;
  const standingOrderTotal = standingOrders.reduce((sum, order) => sum + order.amount, 0);
  const standingOrderSchedulePreview = useMemo(() => {
    const merchant = merchants.find((item) => item.code === merchantCode);
    return {
      merchantName: merchant?.name ?? merchantCode,
      sourceLabel: formatAccountLabel(merchantSourceAccountId),
      amountLabel: Number(standingOrderAmount || "0").toFixed(2),
      cadence: standingOrderFrequency || "MONTHLY"
    };
  }, [merchantCode, merchantSourceAccountId, standingOrderAmount, standingOrderFrequency, merchants]);

  useEffect(() => {
    if (!fromAccountId && liquidAccounts[0]?.id) setFromAccountId(liquidAccounts[0].id);
    if (!toAccountId && (liquidAccounts[1]?.id || liquidAccounts[0]?.id)) {
      setToAccountId(liquidAccounts[1]?.id ?? liquidAccounts[0].id);
    }
    if (!merchantSourceAccountId && liquidAccounts[0]?.id) setMerchantSourceAccountId(liquidAccounts[0].id);
    if (!merchantSettlementAccountId && liquidAccounts[0]?.id) setMerchantSettlementAccountId(liquidAccounts[0].id);
    if (!qrSourceAccountId && liquidAccounts[0]?.id) setQrSourceAccountId(liquidAccounts[0].id);
    if (!investmentSourceAccountId && liquidAccounts[0]?.id) setInvestmentSourceAccountId(liquidAccounts[0].id);
    if (!loanServicingAccountId && liquidAccounts[0]?.id) setLoanServicingAccountId(liquidAccounts[0].id);
  }, [fromAccountId, toAccountId, merchantSourceAccountId, merchantSettlementAccountId, qrSourceAccountId, investmentSourceAccountId, loanServicingAccountId, liquidAccounts]);

  useEffect(() => {
    if (!merchantCode && merchants[0]?.code) setMerchantCode(merchants[0].code);
  }, [merchantCode, merchants]);

  useEffect(() => {
    if (!loanProductId && loanProducts[0]?.id) setLoanProductId(loanProducts[0].id);
  }, [loanProductId, loanProducts]);

  useEffect(() => {
    if (user?.hasTransactionPin) {
      setAuthFactor("pin");
    }
  }, [user?.hasTransactionPin]);

  async function startProtectedAction(nextPurpose: ProtectedPurpose) {
    setIsSubmitting(true);
    setFeedback(null);
    setPurpose(nextPurpose);
    setStepUpToken(null);
    try {
      const challenge = await initiateStepUp({ purpose: nextPurpose, factor: authFactor });
      setChallengeToken(challenge.challengeToken);
      setChallengeFactor(challenge.factor);
      setFeedback(
        challenge.factor === "pin"
          ? "Enter your transaction PIN to approve this action."
          : `Verification code sent to ${challenge.deliveryHint}.`
      );
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to start verification.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function confirmProtectedAction() {
    if (!challengeToken) {
      setFeedback("Start verification first.");
      return;
    }
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const verified = await verifyStepUp({ challengeToken, code: otpCode.trim() });
      setStepUpToken(verified.stepUpToken);
      setChallengeFactor(verified.factor);
      setOtpCode("");
      setFeedback(`Verification approved using ${verified.factor === "pin" ? "your transaction PIN" : "MFA"}. You can now submit the selected banking action.`);
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to verify code.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitInternalTransfer() {
    if (!stepUpToken || purpose !== "TRANSFER_INTERNAL") return void setFeedback("Verify the internal transfer first.");
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const result = await createClientInternalTransfer({ fromAccountId, toAccountId, amount: Number(transferAmount), narration: transferNarration, stepUpToken });
      setFeedback(`Transfer posted. Reference ${result.reference}.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to post transfer.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitMerchantPayment() {
    if (!stepUpToken || purpose !== "MERCHANT_PAYMENT") return void setFeedback("Verify the merchant payment first.");
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const result = await createClientMerchantPayment({ merchantCode, sourceAccountId: merchantSourceAccountId, amount: Number(merchantAmount), narration: merchantNarration, stepUpToken });
      setFeedback(`Merchant payment posted. Reference ${result.reference}.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to pay merchant.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitMerchantEnrollment() {
    if (!stepUpToken || purpose !== "MERCHANT_PROFILE_ENROLLMENT") {
      return void setFeedback("Verify merchant enrollment first.");
    }

    setIsSubmitting(true);
    setFeedback(null);
    try {
      const profile = await createClientMerchantProfile({
        settlementAccountId: merchantSettlementAccountId,
        displayName: merchantDisplayName,
        category: merchantCategory,
        stepUpToken
      });
      setQrPayload(profile.qrPayload);
      setFeedback(`Merchant profile ${profile.merchantCode} is live and ready for BankInsight QR payments.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to enroll merchant profile.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function resolveQr() {
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const preview = await resolveClientQrPayment({ qrPayload });
      setQrPreview(preview);
      if (preview.suggestedAmount && !Number(qrAmount)) {
        setQrAmount(String(preview.suggestedAmount));
      }
      setFeedback(`QR resolved for ${preview.merchantName}.`);
    } catch (error) {
      setQrPreview(null);
      setFeedback(error instanceof Error ? error.message : "Unable to resolve QR payment.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitQrPayment() {
    if (!stepUpToken || purpose !== "MERCHANT_PAYMENT") {
      return void setFeedback("Verify the QR payment first.");
    }

    setIsSubmitting(true);
    setFeedback(null);
    try {
      const result = await createClientQrPayment({
        qrPayload,
        sourceAccountId: qrSourceAccountId,
        amount: Number(qrAmount),
        narration: qrNarration,
        stepUpToken
      });
      setFeedback(`QR payment posted. Reference ${result.reference}.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to pay via QR.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitStandingOrder() {
    if (!stepUpToken || purpose !== "STANDING_ORDER") return void setFeedback("Verify the standing order first.");
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const standingOrder = await createClientStandingOrder({
        sourceAccountId: merchantSourceAccountId,
        instructionType: "MERCHANT_PAYMENT",
        merchantCode,
        amount: Number(standingOrderAmount),
        frequency: standingOrderFrequency,
        narration: standingOrderNarration,
        stepUpToken
      });
      setFeedback(`Standing order ${standingOrder.id} created for ${standingOrder.frequency}.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to create standing order.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitFixedDeposit() {
    if (!stepUpToken || purpose !== "INVESTMENT_FIXED_DEPOSIT") return void setFeedback("Verify the fixed deposit placement first.");
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const deposit = await createClientFixedDeposit({
        sourceAccountId: investmentSourceAccountId,
        principal: Number(investmentPrincipal),
        rate: Number(investmentRate),
        tenureDays: Number(investmentTenureDays),
        currency: "GHS",
        stepUpToken
      });
      setFeedback(`Fixed deposit ${deposit.accountId} placed. Maturity value ${deposit.maturityValue.toFixed(2)} GHS.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to place fixed deposit.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitLoanApplication() {
    if (!stepUpToken || purpose !== "LOAN_APPLICATION") return void setFeedback("Verify the loan application first.");
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const loan = await createClientLoanApplication({ loanProductId, principal: Number(loanAmount), servicingAccountId: loanServicingAccountId, stepUpToken });
      setFeedback(`Loan application ${loan.id} submitted with status ${loan.status}.`);
      clearProtectedAction();
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to apply for loan.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function pauseStandingOrder(id: string, nextStatus: string) {
    setIsSubmitting(true);
    setFeedback(null);
    try {
      await updateClientStandingOrderStatus(id, nextStatus);
      setFeedback(`Standing order ${id} updated to ${nextStatus}.`);
      reload();
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Unable to update standing order.");
    } finally {
      setIsSubmitting(false);
    }
  }

  function clearProtectedAction() {
    setChallengeToken(null);
    setStepUpToken(null);
    setChallengeFactor(authFactor);
    setOtpCode("");
    setPurpose(null);
  }

  function formatAccountLabel(accountId: string) {
    const account = liquidAccounts.find((item) => item.id === accountId) ?? accounts.find((item) => item.id === accountId);
    return account ? `${account.productCode ?? account.type} - ${account.id.slice(-4)}` : accountId;
  }

  return (
    <Screen>
      <Card title="Banking" description="Payments, schedules, deposits, and lending">
        <View style={styles.heroPanel}>
          <View style={styles.heroTopRow}>
            <View style={styles.heroTextBlock}>
              <Text style={styles.heroEyebrow}>Money</Text>
              <Text style={styles.heroTitle}>Move, collect, schedule, invest.</Text>
              <Text style={styles.heroCopy}>Protected by MFA or transaction PIN.</Text>
            </View>
            <View style={styles.heroSignal}>
              <Text style={styles.heroSignalValue}>GHS {(bankingOverview?.totalVisibleBalance ?? 0).toFixed(2)}</Text>
              <Text style={styles.heroSignalLabel}>available</Text>
            </View>
          </View>

          <View style={styles.heroMetrics}>
            <View style={styles.heroMetricTile}>
              <Ionicons name="wallet-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.heroMetricValue}>{bankingOverview?.activeAccountCount ?? 0}</Text>
              <Text style={styles.heroMetricLabel}>active accounts</Text>
            </View>
            <View style={styles.heroMetricTile}>
              <Ionicons name="repeat-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.heroMetricValue}>{bankingOverview?.activeStandingOrderCount ?? 0}</Text>
              <Text style={styles.heroMetricLabel}>scheduled flows</Text>
            </View>
            <View style={styles.heroMetricTile}>
              <Ionicons name="diamond-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.heroMetricValue}>GHS {(bankingOverview?.totalInvestmentBalance ?? 0).toFixed(2)}</Text>
              <Text style={styles.heroMetricLabel}>fixed deposits</Text>
            </View>
            <View style={styles.heroMetricTile}>
              <Ionicons name="document-text-outline" size={18} color={colors.copperSoft} />
              <Text style={styles.heroMetricValue}>GHS {(bankingOverview?.totalLoanExposure ?? 0).toFixed(2)}</Text>
              <Text style={styles.heroMetricLabel}>loan exposure</Text>
            </View>
          </View>
        </View>
      </Card>

      {purpose ? (
        <Card title="Approval" description="Required before posting">
          <View style={styles.protectedBanner}>
            <Text style={styles.protectedBannerLabel}>Current protected action</Text>
            <Text style={styles.protectedBannerValue}>{purpose.replaceAll("_", " ")}</Text>
          </View>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.chipRow}>
            <Pressable style={[styles.chip, authFactor === "otp" && styles.chipActive]} onPress={() => setAuthFactor("otp")}>
              <Text style={[styles.chipLabel, authFactor === "otp" && styles.chipLabelActive]}>MFA verification</Text>
            </Pressable>
            {user?.hasTransactionPin ? (
              <Pressable style={[styles.chip, authFactor === "pin" && styles.chipActive]} onPress={() => setAuthFactor("pin")}>
                <Text style={[styles.chipLabel, authFactor === "pin" && styles.chipLabelActive]}>Transaction PIN</Text>
              </Pressable>
            ) : null}
          </ScrollView>
          <TextInput
            value={otpCode}
            onChangeText={setOtpCode}
            placeholder={challengeFactor === "pin" ? "4-digit transaction PIN" : "Verification code"}
            placeholderTextColor={colors.muted}
            keyboardType="number-pad"
            secureTextEntry={challengeFactor === "pin"}
            style={styles.input}
          />
          <View style={styles.actionRow}>
            <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void confirmProtectedAction()}>
              <Text style={styles.secondaryActionLabel}>{stepUpToken ? "Verification approved" : `Confirm ${challengeFactor === "pin" ? "PIN" : "MFA"}`}</Text>
            </Pressable>
            <Pressable style={styles.ghostAction} onPress={clearProtectedAction}>
              <Text style={styles.ghostActionLabel}>Clear</Text>
            </Pressable>
          </View>
        </Card>
      ) : null}

      {feedback ? (
        <Card title="Status" description="Latest action">
          <Text style={styles.feedbackText}>{feedback}</Text>
        </Card>
      ) : null}

      <Card title="Transfer" description="Own accounts">
        <View style={styles.sectionHeader}>
          <View style={styles.sectionTitleRow}>
            <Ionicons name="swap-horizontal-outline" size={18} color={colors.copper} />
            <Text style={styles.sectionEyebrow}>Own-account rail</Text>
          </View>
          <Text style={styles.sectionLead}>Select path and amount.</Text>
        </View>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.selectorRow}>
          {liquidAccounts.map((account) => (
            <Pressable key={account.id} style={[styles.selectorCard, fromAccountId === account.id && styles.selectorCardActive]} onPress={() => setFromAccountId(account.id)}>
              <Text style={[styles.selectorTitle, fromAccountId === account.id && styles.selectorTitleActive]}>{account.productCode ?? account.type}</Text>
              <Text style={[styles.selectorMeta, fromAccountId === account.id && styles.selectorMetaActive]}>From ref {account.id.slice(-4)}</Text>
            </Pressable>
          ))}
        </ScrollView>
        <TextInput value={fromAccountId} onChangeText={setFromAccountId} placeholder="From account ID" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={toAccountId} onChangeText={setToAccountId} placeholder="To account ID" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={transferAmount} onChangeText={setTransferAmount} placeholder="Amount" placeholderTextColor={colors.muted} keyboardType="decimal-pad" style={styles.input} />
        <TextInput value={transferNarration} onChangeText={setTransferNarration} placeholder="Narration" placeholderTextColor={colors.muted} style={styles.input} />
        <View style={styles.summaryPanel}>
          <Text style={styles.summaryLabel}>Transfer path</Text>
          <Text style={styles.summaryValue}>{formatAccountLabel(fromAccountId)} to {formatAccountLabel(toAccountId)}</Text>
        </View>
        <View style={styles.actionRow}>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void startProtectedAction("TRANSFER_INTERNAL")}>
            <Text style={styles.secondaryActionLabel}>Verify transfer</Text>
          </Pressable>
          <Pressable style={[styles.action, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void submitInternalTransfer()}>
            <Text style={styles.actionLabel}>Transfer now</Text>
          </Pressable>
        </View>
      </Card>

      <Card title="Merchant pay" description="Supported merchants">
        <View style={styles.sectionHeader}>
          <View style={styles.sectionTitleRow}>
            <Ionicons name="storefront-outline" size={18} color={colors.copper} />
            <Text style={styles.sectionEyebrow}>Merchant network</Text>
          </View>
          <Text style={styles.sectionLead}>Merchant and source account.</Text>
        </View>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.chipRow}>
          {merchants.map((merchant) => (
            <Pressable key={merchant.code} style={[styles.chip, merchantCode === merchant.code && styles.chipActive]} onPress={() => setMerchantCode(merchant.code)}>
              <Text style={[styles.chipLabel, merchantCode === merchant.code && styles.chipLabelActive]}>{merchant.name}</Text>
            </Pressable>
          ))}
        </ScrollView>
        <TextInput value={merchantCode} onChangeText={setMerchantCode} placeholder="Merchant code" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={merchantSourceAccountId} onChangeText={setMerchantSourceAccountId} placeholder="Source account ID" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={merchantAmount} onChangeText={setMerchantAmount} placeholder="Amount" placeholderTextColor={colors.muted} keyboardType="decimal-pad" style={styles.input} />
        <TextInput value={merchantNarration} onChangeText={setMerchantNarration} placeholder="Narration" placeholderTextColor={colors.muted} style={styles.input} />
        <View style={styles.summaryPanel}>
          <Text style={styles.summaryLabel}>Payment source</Text>
          <Text style={styles.summaryValue}>{formatAccountLabel(merchantSourceAccountId)}</Text>
        </View>
        <View style={styles.actionRow}>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void startProtectedAction("MERCHANT_PAYMENT")}>
            <Text style={styles.secondaryActionLabel}>Verify merchant payment</Text>
          </Pressable>
          <Pressable style={[styles.action, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void submitMerchantPayment()}>
            <Text style={styles.actionLabel}>Pay merchant</Text>
          </Pressable>
        </View>
      </Card>

      <Card title="Merchant acceptance" description="Business collections">
        <View style={styles.sectionHeader}>
          <View style={styles.sectionTitleRow}>
            <Ionicons name="qr-code-outline" size={18} color={colors.copper} />
            <Text style={styles.sectionEyebrow}>Collect payments</Text>
          </View>
          <Text style={styles.sectionLead}>Enroll settlement account and QR profile.</Text>
        </View>
        <View style={styles.summaryPanel}>
          <Text style={styles.summaryLabel}>Merchant readiness</Text>
          <Text style={styles.summaryValue}>
            {merchantAcceptanceEligibility?.canEnroll
              ? `${merchantAcceptanceEligibility.businessName} can enroll as an app merchant.`
              : merchantAcceptanceEligibility?.reason ?? "Merchant acceptance is available to eligible business customers."}
          </Text>
        </View>
        <TextInput value={merchantDisplayName} onChangeText={setMerchantDisplayName} placeholder="Merchant display name" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={merchantCategory} onChangeText={setMerchantCategory} placeholder="Merchant category" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={merchantSettlementAccountId} onChangeText={setMerchantSettlementAccountId} placeholder="Settlement account ID" placeholderTextColor={colors.muted} style={styles.input} />
        <View style={styles.actionRow}>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting || !merchantAcceptanceEligibility?.canEnroll} onPress={() => void startProtectedAction("MERCHANT_PROFILE_ENROLLMENT")}>
            <Text style={styles.secondaryActionLabel}>Verify enrollment</Text>
          </Pressable>
          <Pressable style={[styles.action, isSubmitting && styles.disabled]} disabled={isSubmitting || !merchantAcceptanceEligibility?.canEnroll} onPress={() => void submitMerchantEnrollment()}>
            <Text style={styles.actionLabel}>Create merchant profile</Text>
          </Pressable>
        </View>
        <View style={styles.listStack}>
          {merchantProfiles.map((profile) => (
            <View key={profile.id} style={styles.listItem}>
              <View style={styles.listRow}>
                <View style={styles.listIdentity}>
                  <Text style={styles.itemEyebrow}>Merchant profile</Text>
                  <Text style={styles.itemTitle}>{profile.displayName}</Text>
                  <Text style={styles.meta}>{profile.merchantCode} | {profile.settlementAccountLabel}</Text>
                </View>
                <View style={[styles.statusPill, styles.statusPillPositive]}>
                  <Text style={[styles.statusPillLabel, styles.statusPillLabelPositive]}>{profile.ghQrReady ? "GH-QR READY" : profile.qrScheme}</Text>
                </View>
              </View>
              <Text style={styles.meta}>{profile.category} | App pay {profile.acceptsAppPayments ? "enabled" : "disabled"}</Text>
              <Text style={styles.payloadText}>{profile.qrPayload}</Text>
            </View>
          ))}
        </View>
      </Card>

      <Card title="QR pay" description="BankInsight QR">
        <View style={styles.sectionHeader}>
          <View style={styles.sectionTitleRow}>
            <Ionicons name="scan-outline" size={18} color={colors.copper} />
            <Text style={styles.sectionEyebrow}>QR checkout</Text>
          </View>
          <Text style={styles.sectionLead}>Resolve and pay.</Text>
        </View>
        <TextInput value={qrPayload} onChangeText={(value) => { setQrPayload(value); setQrPreview(null); }} placeholder="Paste merchant QR payload" placeholderTextColor={colors.muted} style={[styles.input, styles.textArea]} multiline />
        <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void resolveQr()}>
          <Text style={styles.secondaryActionLabel}>Resolve QR</Text>
        </Pressable>
        {qrPreview ? (
          <View style={styles.summaryPanel}>
            <Text style={styles.summaryLabel}>Resolved merchant</Text>
            <Text style={styles.summaryValue}>{qrPreview.merchantName} | {qrPreview.category}</Text>
            <Text style={styles.meta}>{qrPreview.currency} | {qrPreview.qrScheme}{qrPreview.ghQrReady ? " | GH-QR ready" : " | GH-QR future support"}</Text>
          </View>
        ) : null}
        <TextInput value={qrSourceAccountId} onChangeText={setQrSourceAccountId} placeholder="Source account ID" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={qrAmount} onChangeText={setQrAmount} placeholder="Amount" placeholderTextColor={colors.muted} keyboardType="decimal-pad" style={styles.input} />
        <TextInput value={qrNarration} onChangeText={setQrNarration} placeholder="Narration" placeholderTextColor={colors.muted} style={styles.input} />
        <View style={styles.actionRow}>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting || !qrPreview} onPress={() => void startProtectedAction("MERCHANT_PAYMENT")}>
            <Text style={styles.secondaryActionLabel}>Verify QR payment</Text>
          </Pressable>
          <Pressable style={[styles.action, isSubmitting && styles.disabled]} disabled={isSubmitting || !qrPreview} onPress={() => void submitQrPayment()}>
            <Text style={styles.actionLabel}>Pay with QR</Text>
          </Pressable>
        </View>
      </Card>

      <Card title="Standing orders" description="Recurring instructions">
        <View style={styles.sectionHeader}>
          <View style={styles.sectionTitleRow}>
            <Ionicons name="repeat-outline" size={18} color={colors.copper} />
            <Text style={styles.sectionEyebrow}>Automation layer</Text>
          </View>
          <Text style={styles.sectionLead}>Create and manage schedules.</Text>
        </View>
        <View style={styles.standingOrderHero}>
          <View style={styles.standingOrderMetric}>
            <Ionicons name="radio-outline" size={18} color={colors.copper} />
            <Text style={styles.standingOrderMetricValue}>{activeStandingOrders}</Text>
            <Text style={styles.standingOrderMetricLabel}>active instructions</Text>
          </View>
          <View style={styles.standingOrderMetric}>
            <Ionicons name="cash-outline" size={18} color={colors.copper} />
            <Text style={styles.standingOrderMetricValue}>GHS {standingOrderTotal.toFixed(2)}</Text>
            <Text style={styles.standingOrderMetricLabel}>scheduled value</Text>
          </View>
        </View>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.chipRow}>
          {["DAILY", "WEEKLY", "MONTHLY", "QUARTERLY"].map((frequency) => (
            <Pressable
              key={frequency}
              style={[styles.chip, standingOrderFrequency === frequency && styles.chipActive]}
              onPress={() => setStandingOrderFrequency(frequency)}
            >
              <Text style={[styles.chipLabel, standingOrderFrequency === frequency && styles.chipLabelActive]}>{frequency}</Text>
            </Pressable>
          ))}
        </ScrollView>
        <TextInput value={standingOrderAmount} onChangeText={setStandingOrderAmount} placeholder="Recurring amount" placeholderTextColor={colors.muted} keyboardType="decimal-pad" style={styles.input} />
        <TextInput value={standingOrderFrequency} onChangeText={setStandingOrderFrequency} placeholder="Frequency" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={standingOrderNarration} onChangeText={setStandingOrderNarration} placeholder="Narration" placeholderTextColor={colors.muted} style={styles.input} />
        <View style={styles.standingOrderPreview}>
          <View style={styles.standingOrderPreviewRow}>
            <Ionicons name="storefront-outline" size={18} color={colors.forest} />
            <Text style={styles.summaryValue}>{standingOrderSchedulePreview.merchantName}</Text>
          </View>
          <View style={styles.standingOrderPreviewRow}>
            <Ionicons name="wallet-outline" size={18} color={colors.forest} />
            <Text style={styles.summaryValue}>{standingOrderSchedulePreview.sourceLabel}</Text>
          </View>
          <View style={styles.standingOrderPreviewRow}>
            <Ionicons name="calendar-outline" size={18} color={colors.forest} />
            <Text style={styles.summaryValue}>
              GHS {standingOrderSchedulePreview.amountLabel} every {standingOrderSchedulePreview.cadence.toLowerCase()}
            </Text>
          </View>
        </View>
        <View style={styles.actionRow}>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void startProtectedAction("STANDING_ORDER")}>
            <Text style={styles.secondaryActionLabel}>Verify standing order</Text>
          </Pressable>
          <Pressable style={[styles.action, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void submitStandingOrder()}>
            <Text style={styles.actionLabel}>Create standing order</Text>
          </Pressable>
        </View>
        <View style={styles.listStack}>
          {standingOrders.map((order) => (
            <View key={order.id} style={styles.listItem}>
              <View style={styles.listRow}>
                <View style={styles.listIconWrap}>
                  <Ionicons name="repeat-outline" size={20} color={colors.forest} />
                </View>
                <View style={styles.listIdentity}>
                  <Text style={styles.itemEyebrow}>Recurring instruction</Text>
                  <Text style={styles.itemTitle}>{order.instructionType} {order.amount.toFixed(2)} {order.currency}</Text>
                  <Text style={styles.meta}>{order.frequency} | Source {formatAccountLabel(order.sourceAccountId)}</Text>
                </View>
                <View style={[styles.statusPill, order.status === "ACTIVE" ? styles.statusPillPositive : styles.statusPillMuted]}>
                  <Text style={[styles.statusPillLabel, order.status === "ACTIVE" ? styles.statusPillLabelPositive : undefined]}>{order.status}</Text>
                </View>
              </View>
              <Text style={styles.meta}>{order.narration}</Text>
              <Text style={styles.meta}>Next run {new Date(order.nextRunAt).toLocaleString()}</Text>
              <View style={styles.inlineButtonRow}>
                <Pressable style={styles.linkAction} onPress={() => void pauseStandingOrder(order.id, order.status === "ACTIVE" ? "PAUSED" : "ACTIVE")}>
                  <Text style={styles.linkLabel}>{order.status === "ACTIVE" ? "Pause instruction" : "Resume instruction"}</Text>
                </Pressable>
              </View>
            </View>
          ))}
        </View>
      </Card>

      <Card title="Investments" description="Fixed deposits">
        <View style={styles.sectionHeader}>
          <View style={styles.sectionTitleRow}>
            <Ionicons name="diamond-outline" size={18} color={colors.copper} />
            <Text style={styles.sectionEyebrow}>Savings growth</Text>
          </View>
          <Text style={styles.sectionLead}>Source, tenor, rate.</Text>
        </View>
        <TextInput value={investmentSourceAccountId} onChangeText={setInvestmentSourceAccountId} placeholder="Source account ID" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={investmentPrincipal} onChangeText={setInvestmentPrincipal} placeholder="Principal" placeholderTextColor={colors.muted} keyboardType="decimal-pad" style={styles.input} />
        <TextInput value={investmentRate} onChangeText={setInvestmentRate} placeholder="Rate (%)" placeholderTextColor={colors.muted} keyboardType="decimal-pad" style={styles.input} />
        <TextInput value={investmentTenureDays} onChangeText={setInvestmentTenureDays} placeholder="Tenure (days)" placeholderTextColor={colors.muted} keyboardType="number-pad" style={styles.input} />
        <View style={styles.summaryPanel}>
          <Text style={styles.summaryLabel}>Projected instruction</Text>
          <Text style={styles.summaryValue}>GHS {Number(investmentPrincipal || "0").toFixed(2)} for {investmentTenureDays || "0"} days at {investmentRate || "0"}%</Text>
        </View>
        <View style={styles.actionRow}>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void startProtectedAction("INVESTMENT_FIXED_DEPOSIT")}>
            <Text style={styles.secondaryActionLabel}>Verify placement</Text>
          </Pressable>
          <Pressable style={[styles.action, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void submitFixedDeposit()}>
            <Text style={styles.actionLabel}>Place investment</Text>
          </Pressable>
        </View>
        <View style={styles.listStack}>
          {fixedDeposits.map((deposit) => (
            <View key={deposit.id} style={styles.listItem}>
              <Text style={styles.itemEyebrow}>Fixed deposit</Text>
              <Text style={styles.itemTitle}>{deposit.currency} {deposit.principal.toFixed(2)}</Text>
              <Text style={styles.meta}>Matures {new Date(deposit.maturityDate).toLocaleDateString()} | {deposit.status}</Text>
              <Text style={styles.meta}>Projected maturity value {deposit.maturityValue.toFixed(2)}</Text>
            </View>
          ))}
        </View>
      </Card>

      <Card title="Loans" description="Products and applications">
        <View style={styles.sectionHeader}>
          <View style={styles.sectionTitleRow}>
            <Ionicons name="document-text-outline" size={18} color={colors.copper} />
            <Text style={styles.sectionEyebrow}>Borrowing</Text>
          </View>
          <Text style={styles.sectionLead}>Choose product and amount.</Text>
        </View>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.chipRow}>
          {loanProducts.map((product) => (
            <Pressable key={product.id} style={[styles.chip, loanProductId === product.id && styles.chipActive]} onPress={() => setLoanProductId(product.id)}>
              <Text style={[styles.chipLabel, loanProductId === product.id && styles.chipLabelActive]}>{product.name}</Text>
            </Pressable>
          ))}
        </ScrollView>
        <TextInput value={loanProductId} onChangeText={setLoanProductId} placeholder="Loan product ID" placeholderTextColor={colors.muted} style={styles.input} />
        <TextInput value={loanAmount} onChangeText={setLoanAmount} placeholder="Requested amount" placeholderTextColor={colors.muted} keyboardType="decimal-pad" style={styles.input} />
        <TextInput value={loanServicingAccountId} onChangeText={setLoanServicingAccountId} placeholder="Servicing account ID" placeholderTextColor={colors.muted} style={styles.input} />
        <View style={styles.actionRow}>
          <Pressable style={[styles.secondaryAction, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void startProtectedAction("LOAN_APPLICATION")}>
            <Text style={styles.secondaryActionLabel}>Verify application</Text>
          </Pressable>
          <Pressable style={[styles.action, isSubmitting && styles.disabled]} disabled={isSubmitting} onPress={() => void submitLoanApplication()}>
            <Text style={styles.actionLabel}>Apply for loan</Text>
          </Pressable>
        </View>
        <View style={styles.listStack}>
          {loans.map((loan) => (
            <View key={loan.id} style={styles.listItem}>
              <View style={styles.listRow}>
                <View style={styles.listIdentity}>
                  <Text style={styles.itemEyebrow}>Loan case</Text>
                  <Text style={styles.itemTitle}>{loan.productName ?? loan.productCode ?? "Loan"} | {loan.status}</Text>
                </View>
                <View style={styles.statusPill}>
                  <Text style={styles.statusPillLabel}>{loan.parBucket}</Text>
                </View>
              </View>
              <Text style={styles.meta}>Principal {loan.principal.toFixed(2)} | Outstanding {(loan.outstandingBalance ?? 0).toFixed(2)}</Text>
              <Text style={styles.meta}>Repayment {loan.repaymentFrequency ?? "N/A"}</Text>
            </View>
          ))}
        </View>
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  heroPanel: { backgroundColor: colors.surfaceStrong, borderRadius: 20, padding: spacing.lg, gap: spacing.md },
  heroTopRow: { flexDirection: "row", gap: spacing.md, alignItems: "flex-start" },
  heroTextBlock: { flex: 1, gap: 6 },
  heroEyebrow: { color: colors.copperSoft, fontSize: 11, textTransform: "uppercase", letterSpacing: 1.5, fontWeight: "700", fontFamily: typography.body },
  heroTitle: { color: colors.inkInverse, fontSize: 28, lineHeight: 32, fontWeight: "800", letterSpacing: -0.9, fontFamily: typography.display },
  heroCopy: { color: "rgba(245, 248, 252, 0.76)", lineHeight: 18, fontSize: 13, fontFamily: typography.body },
  heroSignal: { minWidth: 126, borderRadius: 20, backgroundColor: "rgba(245, 238, 229, 0.08)", borderWidth: 1, borderColor: "rgba(245, 238, 229, 0.12)", paddingHorizontal: spacing.md, paddingVertical: spacing.md, gap: 6 },
  heroSignalValue: { color: colors.inkInverse, fontSize: 20, lineHeight: 26, fontWeight: "800", letterSpacing: -0.6, fontFamily: typography.display },
  heroSignalLabel: { color: "rgba(245, 248, 252, 0.68)", fontSize: 11, textTransform: "uppercase", letterSpacing: 1, fontFamily: typography.body },
  heroMetrics: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
  heroMetricTile: { minWidth: "47%", flexGrow: 1, borderRadius: 18, padding: spacing.md, backgroundColor: "rgba(245, 238, 229, 0.08)", borderWidth: 1, borderColor: "rgba(245, 238, 229, 0.12)", gap: 4 },
  heroMetricValue: { color: colors.inkInverse, fontSize: 18, fontWeight: "800", fontFamily: typography.display },
  heroMetricLabel: { color: "rgba(245, 248, 252, 0.68)", fontSize: 11, textTransform: "uppercase", letterSpacing: 0.9, fontFamily: typography.body },
  protectedBanner: { borderRadius: 18, backgroundColor: colors.surfaceMuted, borderWidth: 1, borderColor: colors.border, padding: spacing.md, gap: 4 },
  protectedBannerLabel: { color: colors.copper, fontSize: 11, textTransform: "uppercase", letterSpacing: 1, fontWeight: "700", fontFamily: typography.body },
  protectedBannerValue: { color: colors.text, fontSize: 17, fontWeight: "800", fontFamily: typography.display },
  feedbackText: { color: colors.textSoft, lineHeight: 20, fontFamily: typography.body },
  sectionHeader: { gap: 4 },
  sectionTitleRow: { flexDirection: "row", alignItems: "center", gap: spacing.sm },
  sectionEyebrow: { color: colors.copper, fontSize: 11, textTransform: "uppercase", letterSpacing: 1, fontWeight: "700", fontFamily: typography.body },
  sectionLead: { color: colors.muted, lineHeight: 18, fontSize: 13, fontFamily: typography.body },
  selectorRow: { gap: spacing.sm },
  selectorCard: { width: 180, borderRadius: 18, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surfaceSoft, padding: spacing.md, gap: 4 },
  selectorCardActive: { borderColor: colors.copper, backgroundColor: "#f3e4d7" },
  selectorTitle: { color: colors.text, fontWeight: "700" },
  selectorTitleActive: { color: colors.forest },
  selectorMeta: { color: colors.muted, fontSize: 12 },
  selectorMetaActive: { color: colors.textSoft },
  input: { backgroundColor: colors.surfaceSoft, borderRadius: 16, borderWidth: 1, borderColor: colors.border, paddingHorizontal: spacing.md, paddingVertical: 14, color: colors.text },
  textArea: { minHeight: 110, textAlignVertical: "top" },
  summaryPanel: { borderRadius: 18, backgroundColor: colors.surfaceMuted, borderWidth: 1, borderColor: colors.border, padding: spacing.md, gap: 4 },
  summaryLabel: { color: colors.muted, fontSize: 11, textTransform: "uppercase", letterSpacing: 0.9 },
  summaryValue: { color: colors.text, fontWeight: "700", lineHeight: 21 },
  payloadText: { color: colors.textSoft, fontSize: 12, lineHeight: 18 },
  standingOrderHero: { flexDirection: "row", gap: spacing.sm },
  standingOrderMetric: {
    flex: 1,
    borderRadius: 18,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: 4
  },
  standingOrderMetricValue: { color: colors.text, fontWeight: "700", fontSize: 20 },
  standingOrderMetricLabel: { color: colors.muted, fontSize: 11, textTransform: "uppercase", letterSpacing: 0.8 },
  standingOrderPreview: {
    borderRadius: 18,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: spacing.sm
  },
  standingOrderPreviewRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm
  },
  actionRow: { gap: spacing.sm },
  action: { backgroundColor: colors.forest, borderRadius: 999, paddingVertical: 14, paddingHorizontal: spacing.lg },
  actionLabel: { color: colors.white, fontWeight: "700", textAlign: "center" },
  secondaryAction: { borderRadius: 999, borderWidth: 1, borderColor: colors.forest, paddingVertical: 14, paddingHorizontal: spacing.lg },
  secondaryActionLabel: { color: colors.forest, fontWeight: "700", textAlign: "center" },
  ghostAction: { borderRadius: 999, borderWidth: 1, borderColor: colors.borderStrong, paddingVertical: 14, paddingHorizontal: spacing.lg },
  ghostActionLabel: { color: colors.textSoft, textAlign: "center", fontWeight: "700" },
  disabled: { opacity: 0.6 },
  chipRow: { gap: spacing.sm },
  chip: { borderRadius: 999, borderWidth: 1, borderColor: colors.borderStrong, backgroundColor: colors.surfaceMuted, paddingHorizontal: spacing.md, paddingVertical: 10 },
  chipActive: { borderColor: colors.copper, backgroundColor: "#f3e4d7" },
  chipLabel: { color: colors.textSoft, fontWeight: "600" },
  chipLabelActive: { color: colors.forest },
  listStack: { gap: spacing.sm },
  listItem: { borderRadius: 18, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surfaceSoft, padding: spacing.md, gap: 6 },
  listRow: { flexDirection: "row", justifyContent: "space-between", gap: spacing.md, alignItems: "flex-start" },
  listIconWrap: {
    width: 42,
    height: 42,
    borderRadius: 14,
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center"
  },
  listIdentity: { flex: 1, gap: 4 },
  itemEyebrow: { color: colors.copper, fontSize: 11, fontWeight: "700", textTransform: "uppercase", letterSpacing: 0.8 },
  itemTitle: { color: colors.text, fontWeight: "700", lineHeight: 21 },
  meta: { color: colors.muted, lineHeight: 20 },
  statusPill: { borderRadius: 999, paddingHorizontal: spacing.sm, paddingVertical: 8, backgroundColor: colors.surfaceMuted, borderWidth: 1, borderColor: colors.border },
  statusPillPositive: { backgroundColor: "rgba(33, 117, 79, 0.10)", borderColor: "rgba(33, 117, 79, 0.24)" },
  statusPillMuted: { backgroundColor: colors.surfaceMuted },
  statusPillLabel: { color: colors.textSoft, fontSize: 11, fontWeight: "700", textTransform: "uppercase", letterSpacing: 0.8 },
  statusPillLabelPositive: { color: colors.stable },
  inlineButtonRow: { flexDirection: "row", gap: spacing.sm },
  linkAction: { paddingTop: spacing.xs },
  linkLabel: { color: colors.forestSoft, fontWeight: "700" }
});
