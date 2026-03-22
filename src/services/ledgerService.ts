/**
 * BankInsight Ledger Service (Frontend)
 * Client-side service for interacting with the backend Ledger Engine
 * Handles deposits, withdrawals, transfers, and cheque processing with BOG compliance
 */

import { httpClient } from './httpClient';
import { customerService } from './customerService';

export interface DepositRequest {
  accountId: string;
  customerId: string;
  amount: number;
  depositMethod: 'CASH' | 'CHEQUE';
  narration: string;
  tellerId: string;
  customerGhanaCard: string;
  chequeNumber?: string;
  chequeBank?: string;
  branchId?: string;
}

export interface WithdrawalRequest {
  accountId: string;
  customerId: string;
  amount: number;
  withdrawalMethod: 'CASH' | 'CHEQUE';
  narration: string;
  tellerId: string;
  customerGhanaCard: string;
  chequeNumber?: string;
  branchId?: string;
}

export interface TransferRequest {
  fromAccountId: string;
  toAccountId: string;
  customerId: string;
  amount: number;
  narration: string;
  tellerId: string;
  customerGhanaCard: string;
  branchId?: string;
}

export interface ChequeRequest {
  accountId: string;
  customerId: string;
  chequeNumber: string;
  chequeBank: string;
  amount: number;
  narration: string;
  transactionType: 'DEPOSIT' | 'WITHDRAWAL';
  tellerId: string;
  branchId?: string;
}

export interface LedgerPostingResult {
  success: boolean;
  transactionId: string;
  reference: string;
  narration: string;
  amount: number;
  appliedFees: number;
  netAmount: number;
  newBalance: number;
  availableMargin: number;
  status: 'POSTED' | 'PENDING' | 'REJECTED';
  message: string;
  journalLines?: Array<{
    glCode: string;
    debit: number;
    credit: number;
    narration: string;
  }>;
}

export interface LedgerEntry {
  id: string;
  journalId: string;
  glCode: string;
  glName: string;
  debit: number;
  credit: number;
  narration: string;
  postedDate: string;
}

export interface LedgerBalance {
  accountId: string;
  balance: number;
  lienAmount: number;
  availableBalance: number;
  dailyDebitTotal: number;
  dailyCreditTotal: number;
}

class LedgerService {
  private baseUrl = '/ledger';

  /**
   * Post a cash or cheque deposit
   */
  async postDeposit(request: DepositRequest): Promise<LedgerPostingResult> {
    try {
      return await httpClient.post<LedgerPostingResult>(`${this.baseUrl}/deposits`, request);
    } catch (error: any) {
      const message = error.message || 'Failed to post deposit';
      throw new Error(message);
    }
  }

  /**
   * Post a cash or cheque withdrawal
   */
  async postWithdrawal(request: WithdrawalRequest): Promise<LedgerPostingResult> {
    try {
      return await httpClient.post<LedgerPostingResult>(`${this.baseUrl}/withdrawals`, request);
    } catch (error: any) {
      const message = error.message || 'Failed to post withdrawal';
      throw new Error(message);
    }
  }

  /**
   * Post an inter-account transfer
   */
  async postTransfer(request: TransferRequest): Promise<LedgerPostingResult> {
    try {
      return await httpClient.post<LedgerPostingResult>(`${this.baseUrl}/transfers`, request);
    } catch (error: any) {
      const message = error.message || 'Failed to post transfer';
      throw new Error(message);
    }
  }

  /**
   * Post a cheque transaction (deposit or withdrawal)
   */
  async postCheque(request: ChequeRequest): Promise<LedgerPostingResult> {
    try {
      return await httpClient.post<LedgerPostingResult>(`${this.baseUrl}/cheques`, request);
    } catch (error: any) {
      const message = error.message || 'Failed to post cheque';
      throw new Error(message);
    }
  }

  /**
   * Get ledger entries for an account
   */
  async getLedgerEntries(
    accountId: string,
    fromDate?: string,
    toDate?: string
  ): Promise<LedgerEntry[]> {
    const params = new URLSearchParams({ accountId });
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    try {
      return await httpClient.get<LedgerEntry[]>(`${this.baseUrl}/ledger?${params.toString()}`);
    } catch (error: any) {
      const message = error.message || 'Failed to fetch ledger entries';
      throw new Error(message);
    }
  }

  /**
   * Get available credit margin for a customer (for lending decisions)
   */
  async getAvailableMargin(customerId: string): Promise<number> {
    try {
      const data = await httpClient.get<{ availableMargin: number }>(`${this.baseUrl}/margins/${customerId}`);
      return data.availableMargin;
    } catch (error) {
      console.warn('Failed to fetch available margin:', error);
      return 0;
    }
  }

  /**
   * Get account balance and daily transaction totals
   */
  async getAccountBalance(accountId: string): Promise<LedgerBalance> {
    try {
      return await httpClient.get<LedgerBalance>(`${this.baseUrl}/balance/${accountId}`);
    } catch (error: any) {
      const message = error.message || 'Failed to fetch account balance';
      throw new Error(message);
    }
  }

  /**
   * Get KYC daily limit for a customer (BOG compliance)
   */
  async getKycDailyLimit(customerId: string): Promise<number> {
    try {
      const data = await httpClient.get<{ dailyLimit: number }>(`/kyc/daily-limit/${customerId}`);
      return data.dailyLimit;
    } catch (error) {
      console.warn('Failed to fetch KYC daily limit:', error);
      return 5000; // Default to Tier 1 limit
    }
  }

  /**
   * Validate customer Ghana Card information (BOG requirement)
   */
  async validateCustomerGhanaCard(customerId: string, ghanaCardNumber: string): Promise<boolean> {
    try {
      const data = await httpClient.post<{ isValid: boolean }>(`/kyc/validate-ghana-card`, { customerId, ghanaCardNumber });
      return data.isValid;
    } catch (error) {
      console.error('Ghana Card validation failed:', error);
      return false;
    }
  }

  /**
   * Get transaction limits based on KYC tier
   */
  async getKycLimits(customerId: string): Promise<{ transactionLimit: number; dailyLimit: number; kycLevel: string }> {
    try {
      const result = await customerService.getCustomerKyc(customerId);
      return {
        transactionLimit: result.transactionLimit,
        dailyLimit: result.dailyLimit,
        kycLevel: result.kycLevel,
      };
    } catch (error) {
      console.warn('Failed to fetch KYC limits:', error);
      return {
        transactionLimit: 1000,
        dailyLimit: 5000,
        kycLevel: 'TIER1'
      };
    }
  }

  /**
   * Get customer credit scoring and margins
   */
  async getCustomerMargins(customerId: string): Promise<{
    creditScore: number;
    maxLoanAmount: number;
    baseMargin: number;
    riskAdjustment: number;
    availableMargin: number;
  }> {
    try {
      return await httpClient.get<{
        creditScore: number;
        maxLoanAmount: number;
        baseMargin: number;
        riskAdjustment: number;
        availableMargin: number;
      }>(`/api/margins/customer/${customerId}`);
    } catch (error) {
      console.warn('Failed to fetch customer margins:', error);
      return {
        creditScore: 0,
        maxLoanAmount: 0,
        baseMargin: 0,
        riskAdjustment: 1,
        availableMargin: 0
      };
    }
  }

  /**
   * Verify transaction compliance with all BOG requirements
   */
  async verifyTransactionCompliance(
    customerId: string,
    amount: number,
    transactionType: string
  ): Promise<{ compliant: boolean; reason?: string; warnings?: string[] }> {
    try {
      return await httpClient.post<{ compliant: boolean; reason?: string; warnings?: string[] }>(`/api/compliance/verify`, {
        customerId,
        amount,
        transactionType
      });
    } catch (error: any) {
      console.error('Compliance verification failed:', error);
      return {
        compliant: false,
        reason: error.message || 'Compliance check failed'
      };
    }
  }

  /**
   * Get suspicious transaction flags
   */
  async checkSuspiciousActivity(accountId: string, amount: number): Promise<{
    isSuspicious: boolean;
    riskScore: number;
    flags: string[];
  }> {
    try {
      return await httpClient.post<{
        isSuspicious: boolean;
        riskScore: number;
        flags: string[];
      }>(`/api/compliance/suspicious-check`, {
        accountId,
        amount
      });
    } catch (error) {
      console.warn('Suspicious activity check failed:', error);
      return {
        isSuspicious: false,
        riskScore: 0,
        flags: []
      };
    }
  }

  /**
   * Post a generic teller transaction (legacy/wrapper)
   */
  async postTellerTransaction(
    accountId: string,
    type: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER',
    amount: number,
    narration: string,
    tellerId: string,
    customerId: string,
    ghanaCard: string,
    method: 'CASH' | 'CHEQUE' = 'CASH'
  ): Promise<LedgerPostingResult> {
    if (type === 'DEPOSIT') {
      return this.postDeposit({
        accountId,
        customerId,
        amount,
        depositMethod: method,
        narration,
        tellerId,
        customerGhanaCard: ghanaCard
      });
    } else if (type === 'WITHDRAWAL') {
      return this.postWithdrawal({
        accountId,
        customerId,
        amount,
        withdrawalMethod: method,
        narration,
        tellerId,
        customerGhanaCard: ghanaCard
      });
    } else {
      throw new Error('Use postTransfer for TRANSFER transactions');
    }
  }
}

export const ledgerService = new LedgerService();
export default ledgerService;
