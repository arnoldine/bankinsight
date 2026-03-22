/**
 * Validation helpers for BankInsight frontend validation.
 * Ghana Card and BoG compliance rules.
 */

// Ghana Card ID: GHA-000000000-0 (9 digits, 1 check digit)
const GHANA_CARD_REGEX = /^GHA-\d{9}-\d{1}$/;

export type GhanaCard = string;

export function validateGhanaCard(value: string): { success: true; data: string } | { success: false; error: string } {
  if (GHANA_CARD_REGEX.test(value)) {
    return { success: true, data: value };
  }
  return { success: false, error: 'Ghana Card must be in format GHA-000000000-0' };
}

// Customer creation payload (BoG KYC tiers)
export interface CustomerCreateInput {
  name: string;
  ghanaCard: string;
  digitalAddress?: string;
  kycLevel: 'Tier 1' | 'Tier 2' | 'Tier 3';
  phone: string;
  email?: string;
  riskRating?: 'Low' | 'Medium' | 'High';
}

// Teller transaction payload
export interface TellerTxnInput {
  accountId: string;
  type: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'LOAN_REPAYMENT';
  amount: number;
  narration?: string;
  tellerId: string;
}
