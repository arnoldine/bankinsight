import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import { Account, Customer, Loan } from '../../types';

export interface DigitalBankingDashboard {
  activeSavingsAccounts: number;
  totalSavingsBalance: number;
  activeInvestmentProfiles: number;
  totalInvestmentBalance: number;
  activeLoans: number;
  totalLoanExposure: number;
  pendingApprovals: number;
}

export interface DigitalBankingProduct {
  productCode: string;
  name: string;
  type: string;
  currency: string;
  interestRate?: number;
  interestMethod?: string;
  minAmount?: number;
  maxAmount?: number;
  defaultTerm?: number;
  status: string;
}

export interface DigitalInvestmentProfile {
  id: string;
  accountId: string;
  customerId: string;
  fundingAccountId: string;
  productCode: string;
  currency: string;
  principal: number;
  rate: number;
  tenorDays: number;
  payoutOption: string;
  autoRollover: boolean;
  status: string;
  startDate: string;
  maturityDate: string;
  projectedMaturityValue: number;
  notes?: string;
}

export interface DigitalInvestmentPortfolio {
  activeProfiles: number;
  totalPrincipal: number;
  totalProjectedMaturityValue: number;
  byCurrency: Record<string, number>;
  items: DigitalInvestmentProfile[];
}

export interface DigitalLoanEligibility {
  isEligible: boolean;
  reasons: string[];
  creditCheck: {
    customerId: string;
    score: number;
    internalScore?: number;
    bureauScore?: number | null;
    compositeScore?: number;
    probabilityGood?: number;
    riskBand: string;
    riskGrade: string;
    decision: string;
    recommendation: string;
    checkedAt?: string;
  };
}

export interface OpenDigitalSavingsAccountRequest {
  customerId: string;
  productCode: string;
  branchId?: string;
  currency?: string;
  initialDepositAmount?: number;
  fundingAccountId?: string;
}

export interface CreateDigitalInvestmentRequest {
  customerId: string;
  fundingAccountId: string;
  productCode: string;
  principal: number;
  rate: number;
  tenorDays: number;
  payoutOption: string;
  autoRollover?: boolean;
  notes?: string;
}

export interface DigitalSavingsTransferRequest {
  counterpartyAccountId: string;
  amount: number;
  narration: string;
}

export interface DigitalInvestmentActionRequest {
  fundingAccountId?: string;
  amount?: number;
  newMaturityDate?: string;
  newRate?: number;
  destinationAccountId?: string;
  penaltyAmount?: number;
  notes?: string;
}

export interface CheckDigitalLoanEligibilityRequest {
  customerId: string;
  loanProductId?: string;
  principal?: number;
  providerName?: string;
}

export interface CreateDigitalLoanApplicationRequest {
  customerId: string;
  loanProductId: string;
  principal: number;
  servicingAccountId?: string;
  collateralAccountId?: string;
  clientReference?: string;
}

class DigitalBankingService {
  async getDashboard(): Promise<DigitalBankingDashboard> {
    return httpClient.get<DigitalBankingDashboard>(API_ENDPOINTS.digitalBanking.dashboard);
  }

  async getSavingsProducts(): Promise<DigitalBankingProduct[]> {
    return httpClient.get<DigitalBankingProduct[]>(API_ENDPOINTS.digitalBanking.savingsProducts);
  }

  async getCustomerSavingsAccounts(customerId: string): Promise<Account[]> {
    return httpClient.get<Account[]>(API_ENDPOINTS.digitalBanking.customerSavingsAccounts(customerId));
  }

  async openSavingsAccount(data: OpenDigitalSavingsAccountRequest): Promise<Account> {
    return httpClient.post<Account>(API_ENDPOINTS.digitalBanking.openSavingsAccount, data);
  }

  async fundSavingsAccount(accountId: string, data: DigitalSavingsTransferRequest): Promise<Account> {
    return httpClient.post<Account>(API_ENDPOINTS.digitalBanking.fundSavingsAccount(accountId), data);
  }

  async withdrawSavingsAccount(accountId: string, data: DigitalSavingsTransferRequest): Promise<Account> {
    return httpClient.post<Account>(API_ENDPOINTS.digitalBanking.withdrawSavingsAccount(accountId), data);
  }

  async getInvestmentPortfolio(customerId?: string): Promise<DigitalInvestmentPortfolio> {
    const suffix = customerId ? `?customerId=${encodeURIComponent(customerId)}` : '';
    return httpClient.get<DigitalInvestmentPortfolio>(`${API_ENDPOINTS.digitalBanking.investmentPortfolio}${suffix}`);
  }

  async createInvestment(data: CreateDigitalInvestmentRequest): Promise<DigitalInvestmentProfile> {
    return httpClient.post<DigitalInvestmentProfile>(API_ENDPOINTS.digitalBanking.createInvestment, data);
  }

  async topUpInvestment(profileId: string, data: DigitalInvestmentActionRequest): Promise<DigitalInvestmentProfile> {
    return httpClient.post<DigitalInvestmentProfile>(API_ENDPOINTS.digitalBanking.topUpInvestment(profileId), data);
  }

  async rolloverInvestment(profileId: string, data: DigitalInvestmentActionRequest): Promise<DigitalInvestmentProfile> {
    return httpClient.post<DigitalInvestmentProfile>(API_ENDPOINTS.digitalBanking.rolloverInvestment(profileId), data);
  }

  async liquidateInvestment(profileId: string, data: DigitalInvestmentActionRequest): Promise<DigitalInvestmentProfile> {
    return httpClient.post<DigitalInvestmentProfile>(API_ENDPOINTS.digitalBanking.liquidateInvestment(profileId), data);
  }

  async checkLoanEligibility(data: CheckDigitalLoanEligibilityRequest): Promise<DigitalLoanEligibility> {
    return httpClient.post<DigitalLoanEligibility>(API_ENDPOINTS.digitalBanking.checkLoanEligibility, data);
  }

  async applyLoan(data: CreateDigitalLoanApplicationRequest): Promise<Loan> {
    return httpClient.post<Loan>(API_ENDPOINTS.digitalBanking.applyLoan, data);
  }

  async repayLoan(loanId: string, data: { amount: number; accountId: string; clientReference?: string }): Promise<Loan> {
    return httpClient.post<Loan>(API_ENDPOINTS.digitalBanking.repayLoan(loanId), data);
  }

  async restructureLoan(data: { loanId: string; newTermInPeriods: number; newAnnualRate?: number; newRepaymentFrequency?: string; reason: string }): Promise<Loan> {
    return httpClient.post<Loan>(API_ENDPOINTS.digitalBanking.restructureLoan, data);
  }

  async getLoanStatement(loanId: string): Promise<any> {
    return httpClient.get<any>(API_ENDPOINTS.digitalBanking.loanStatement(loanId));
  }

  async getLoanSchedule(loanId: string): Promise<any[]> {
    return httpClient.get<any[]>(API_ENDPOINTS.digitalBanking.loanSchedule(loanId));
  }
}

export const digitalBankingService = new DigitalBankingService();
