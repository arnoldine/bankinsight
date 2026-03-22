/**
 * useLedger - React Hook for Ledger Engine Operations
 * Provides transaction posting, balance queries, and BOG compliance checks
 */

import { useState, useCallback, useRef } from 'react';
import {
  ledgerService,
  DepositRequest,
  WithdrawalRequest,
  TransferRequest,
  ChequeRequest,
  LedgerPostingResult,
  LedgerEntry,
  LedgerBalance
} from '../services/ledgerService';

interface UseLedgerState {
  loading: boolean;
  error: string | null;
  success: string | null;
  result: LedgerPostingResult | null;
  ledgerEntries: LedgerEntry[];
  balance: LedgerBalance | null;
}

export const useLedger = () => {
  const [state, setState] = useState<UseLedgerState>({
    loading: false,
    error: null,
    success: null,
    result: null,
    ledgerEntries: [],
    balance: null
  });

  const abortControllerRef = useRef<AbortController | null>(null);

  const clearMessages = useCallback(() => {
    setState(prev => ({
      ...prev,
      error: null,
      success: null
    }));
  }, []);

  const postDeposit = useCallback(async (request: DepositRequest) => {
    clearMessages();
    setState(prev => ({ ...prev, loading: true }));

    try {
      const result = await ledgerService.postDeposit(request);
      setState(prev => ({
        ...prev,
        loading: false,
        result,
        success: `Deposit posted successfully (Ref: ${result.reference})`
      }));
      return result;
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to post deposit';
      setState(prev => ({
        ...prev,
        loading: false,
        error: errorMessage,
        result: null
      }));
      throw error;
    }
  }, [clearMessages]);

  const postWithdrawal = useCallback(async (request: WithdrawalRequest) => {
    clearMessages();
    setState(prev => ({ ...prev, loading: true }));

    try {
      const result = await ledgerService.postWithdrawal(request);
      setState(prev => ({
        ...prev,
        loading: false,
        result,
        success: `Withdrawal posted successfully (Ref: ${result.reference})`
      }));
      return result;
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to post withdrawal';
      setState(prev => ({
        ...prev,
        loading: false,
        error: errorMessage,
        result: null
      }));
      throw error;
    }
  }, [clearMessages]);

  const postTransfer = useCallback(async (request: TransferRequest) => {
    clearMessages();
    setState(prev => ({ ...prev, loading: true }));

    try {
      const result = await ledgerService.postTransfer(request);
      setState(prev => ({
        ...prev,
        loading: false,
        result,
        success: `Transfer posted successfully (Ref: ${result.reference})`
      }));
      return result;
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to post transfer';
      setState(prev => ({
        ...prev,
        loading: false,
        error: errorMessage,
        result: null
      }));
      throw error;
    }
  }, [clearMessages]);

  const postCheque = useCallback(async (request: ChequeRequest) => {
    clearMessages();
    setState(prev => ({ ...prev, loading: true }));

    try {
      const result = await ledgerService.postCheque(request);
      setState(prev => ({
        ...prev,
        loading: false,
        result,
        success: `Cheque posted successfully (Ref: ${result.reference})`
      }));
      return result;
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to post cheque';
      setState(prev => ({
        ...prev,
        loading: false,
        error: errorMessage,
        result: null
      }));
      throw error;
    }
  }, [clearMessages]);

  const getLedgerEntries = useCallback(async (
    accountId: string,
    fromDate?: string,
    toDate?: string
  ) => {
    setState(prev => ({ ...prev, loading: true }));

    try {
      const entries = await ledgerService.getLedgerEntries(accountId, fromDate, toDate);
      setState(prev => ({
        ...prev,
        loading: false,
        ledgerEntries: entries
      }));
      return entries;
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to fetch ledger entries';
      setState(prev => ({
        ...prev,
        loading: false,
        error: errorMessage,
        ledgerEntries: []
      }));
      throw error;
    }
  }, []);

  const getAccountBalance = useCallback(async (accountId: string) => {
    setState(prev => ({ ...prev, loading: true }));

    try {
      const balance = await ledgerService.getAccountBalance(accountId);
      setState(prev => ({
        ...prev,
        loading: false,
        balance
      }));
      return balance;
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to fetch account balance';
      setState(prev => ({
        ...prev,
        loading: false,
        error: errorMessage,
        balance: null
      }));
      throw error;
    }
  }, []);

  const validateBogCompliance = useCallback(async (customerId: string, amount: number) => {
    try {
      const limits = await ledgerService.getKycLimits(customerId);
      const isCompliant = amount <= limits.transactionLimit;
      return {
        compliant: isCompliant,
        limits,
        message: isCompliant
          ? `Transaction complies with KYC ${limits.kycLevel} limit`
          : `Amount exceeds KYC ${limits.kycLevel} limit of ${limits.transactionLimit}`
      };
    } catch (error: any) {
      return {
        compliant: false,
        message: 'BOG compliance check failed',
        error: error.message
      };
    }
  }, []);

  const checkGhanaCardValidity = useCallback(async (customerId: string, ghanaCardNumber: string) => {
    try {
      const isValid = await ledgerService.validateCustomerGhanaCard(customerId, ghanaCardNumber);
      return {
        valid: isValid,
        message: isValid ? 'Ghana Card verified' : 'Ghana Card does not match customer records'
      };
    } catch (error: any) {
      return {
        valid: false,
        message: 'Ghana Card validation failed',
        error: error.message
      };
    }
  }, []);

  const getAvailableMargin = useCallback(async (customerId: string) => {
    try {
      const margin = await ledgerService.getAvailableMargin(customerId);
      return margin;
    } catch (error) {
      console.warn('Failed to get available margin:', error);
      return 0;
    }
  }, []);

  const verifyCompliance = useCallback(async (customerId: string, amount: number, type: string) => {
    try {
      const result = await ledgerService.verifyTransactionCompliance(customerId, amount, type);
      return result;
    } catch (error: any) {
      return {
        compliant: false,
        reason: error.message || 'Compliance verification failed'
      };
    }
  }, []);

  const checkSuspiciousActivity = useCallback(async (accountId: string, amount: number) => {
    try {
      const result = await ledgerService.checkSuspiciousActivity(accountId, amount);
      return result;
    } catch (error) {
      console.warn('Suspicious activity check failed:', error);
      return {
        isSuspicious: false,
        riskScore: 0,
        flags: []
      };
    }
  }, []);

  return {
    // State
    loading: state.loading,
    error: state.error,
    success: state.success,
    result: state.result,
    ledgerEntries: state.ledgerEntries,
    balance: state.balance,

    // Actions
    postDeposit,
    postWithdrawal,
    postTransfer,
    postCheque,
    getLedgerEntries,
    getAccountBalance,
    validateBogCompliance,
    checkGhanaCardValidity,
    getAvailableMargin,
    verifyCompliance,
    checkSuspiciousActivity,
    clearMessages
  };
};

export default useLedger;
