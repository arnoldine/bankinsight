import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface Loan {
    id: string;
    cif: string;
    groupId?: string;
    productCode?: string;
    productName?: string;
    principal: number;
    rate: number;
    termMonths: number;
    disbursementDate?: string;
    parBucket: string;
    outstandingBalance?: number;
    collateralType?: string;
    collateralValue?: number;
    status: string;
    interestMethod?: string;
    repaymentFrequency?: string;
    scheduleType?: string;
    loanProductId?: string;
}

export interface DisburseLoanRequest {
    loanId?: string;
    cif?: string;
    groupId?: string;
    productCode?: string;
    principal?: number;
    rate?: number;
    termMonths?: number;
    clientReference?: string;
    collateralType?: string;
    collateralValue?: number;
}

export interface LoanRepayRequest {
    amount: number;
    accountId: string;
    clientReference?: string;
}

export interface LoanProductConfigRequest {
    id: string;
    code: string;
    name: string;
    productType: 'DigitalLoan30Days' | 'WeeklyGroupLoan' | 'MonthlyBusinessLoan' | 'MonthlyConsumerLoan';
    interestMethod: 'Flat' | 'ReducingBalance';
    repaymentFrequency: 'Weekly' | 'Monthly' | 'Bullet';
    termInPeriods: number;
    annualInterestRate: number;
    minAmount: number;
    maxAmount: number;
}

export interface LoanApplyRequest {
    customerId: string;
    groupId?: string;
    loanProductId: string;
    principal: number;
    annualInterestRate: number;
    termInPeriods: number;
    interestMethod: 'Flat' | 'ReducingBalance';
    repaymentFrequency: 'Weekly' | 'Monthly' | 'Bullet';
    scheduleType: 'Weekly' | 'Monthly' | 'Bullet';
    clientReference?: string;
}

export interface LoanApproveRequest {
    loanId: string;
    decisionNotes?: string;
}

export interface LoanAppraiseRequest {
    loanId: string;
    decision: string;
    notes?: string;
}

export interface LoanRestructureRequest {
    loanId: string;
    newTermInPeriods: number;
    newAnnualRate?: number;
    newRepaymentFrequency?: string;
    reason: string;
}

export interface LoanRepaymentReversalRequest {
    loanId: string;
    repaymentId: string;
    reason: string;
}

export interface LoanAccrualBatchRequest {
    asOfDate?: string;
    loanId?: string;
}

export interface LoanWriteOffRequest {
    loanId: string;
    amount: number;
    reason: string;
}

export interface LoanRecoveryRequest {
    loanId: string;
    amount: number;
    accountId: string;
    reference?: string;
}

export interface CreditCheckRequest {
    customerId: string;
    loanId?: string;
    providerName?: string;
}

export interface CreditCheckResult {
    customerId: string;
    loanId?: string;
    score: number;
    riskBand: string;
    riskGrade: string;
    decision: string;
    recommendation: string;
    providerName: string;
    inquiryReference: string;
    checkedAt: string;
}

export interface LoanDelinquencyDashboard {
    totalActiveLoans: number;
    nonAccrualLoans: number;
    portfolioAtRisk30: number;
    portfolioAtRisk90: number;
    agingBuckets: Record<string, number>;
}

export interface LoanProfitabilityItem {
    groupingKey: string;
    interestIncome: number;
    processingFeeIncome: number;
    penaltyIncome: number;
    impairmentExpense: number;
    recoveryIncome: number;
    netContribution: number;
}

export interface LoanProfitabilityReport {
    fromDate: string;
    toDate: string;
    productLevel: LoanProfitabilityItem[];
    branchLevel: LoanProfitabilityItem[];
}

export interface LoanBalanceSheetReport {
    asOfDate: string;
    total: {
        grossLoanPortfolio: number;
        accruedInterestReceivable: number;
        accruedPenaltyReceivable: number;
        impairmentAllowance: number;
        netLoanPortfolio: number;
    };
    branchContributions: LoanProfitabilityItem[];
}

export interface LoanGlPosting {
    journalId: string;
    reference?: string;
    description?: string;
    createdAt: string;
    lines: Array<{ accountCode: string; debit: number; credit: number }>;
}

export interface LoanStatement {
    loanId: string;
    customerId: string;
    principal: number;
    outstandingBalance: number;
    totalInterestPaid: number;
    totalPenaltyPaid: number;
    status: string;
    schedule: LoanScheduleDto[];
}

export interface CreditProvider {
    providerName: string;
}

export interface LoanScheduleDto {
    period: number;
    dueDate: string;
    principal: number;
    interest: number;
    total: number;
    balance: number;
    status: string;
    paidDate?: string;
}

class LoanService {
    async getLoans(): Promise<Loan[]> {
        return httpClient.get<Loan[]>(API_ENDPOINTS.loans.list);
    }

    async getLoan(id: string): Promise<Loan> {
        return httpClient.get<Loan>(API_ENDPOINTS.loans.get(id));
    }

    async disburseLoan(data: DisburseLoanRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.disburse(''), data);
    }

    async applyLoan(data: LoanApplyRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.apply, data);
    }

    async appraiseLoan(data: LoanAppraiseRequest): Promise<any> {
        return httpClient.post<any>(API_ENDPOINTS.loans.appraise, data);
    }

    async approveLoan(data: LoanApproveRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.approve, data);
    }

    async restructureLoan(data: LoanRestructureRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.restructure, data);
    }

    async reverseRepayment(data: LoanRepaymentReversalRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.repayReverse, data);
    }

    async repayLoan(id: string, data: LoanRepayRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.repay(id), data);
    }

    async repayLoanUnified(data: { loanId: string } & LoanRepayRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.repayUnified, data);
    }

    async processAccrualBatch(data: LoanAccrualBatchRequest): Promise<any> {
        return httpClient.post<any>(API_ENDPOINTS.loans.accrualProcess, data);
    }

    async writeOffLoan(data: LoanWriteOffRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.writeoff, data);
    }

    async recoverLoan(data: LoanRecoveryRequest): Promise<Loan> {
        return httpClient.post<Loan>(API_ENDPOINTS.loans.recover, data);
    }

    async getLoanSchedule(id: string): Promise<LoanScheduleDto[]> {
        return httpClient.get<LoanScheduleDto[]>(API_ENDPOINTS.loans.schedule(id));
    }

    async generateSchedule(data: any): Promise<any> {
        return httpClient.post<any>(API_ENDPOINTS.loans.generateSchedule, data);
    }

    async getLoanStatement(id: string): Promise<LoanStatement> {
        return httpClient.get<LoanStatement>(API_ENDPOINTS.loans.statement(id));
    }

    async checkCredit(data: CreditCheckRequest): Promise<CreditCheckResult> {
        return httpClient.post<CreditCheckResult>(API_ENDPOINTS.loans.checkCredit, data);
    }

    async getCreditProviders(): Promise<CreditProvider[]> {
        return httpClient.get<CreditProvider[]>(API_ENDPOINTS.loans.creditProviders);
    }

    async getDelinquencyDashboard(): Promise<LoanDelinquencyDashboard> {
        return httpClient.get<LoanDelinquencyDashboard>(API_ENDPOINTS.loans.delinquencyDashboard);
    }

    async getProfitabilityReport(fromDate?: string, toDate?: string): Promise<LoanProfitabilityReport> {
        const params = new URLSearchParams();
        if (fromDate) params.append('fromDate', fromDate);
        if (toDate) params.append('toDate', toDate);
        const suffix = params.toString() ? `?${params.toString()}` : '';
        return httpClient.get<LoanProfitabilityReport>(`${API_ENDPOINTS.loans.profitabilityReport}${suffix}`);
    }

    async getBalanceSheetReport(asOfDate?: string): Promise<LoanBalanceSheetReport> {
        const suffix = asOfDate ? `?asOfDate=${encodeURIComponent(asOfDate)}` : '';
        return httpClient.get<LoanBalanceSheetReport>(`${API_ENDPOINTS.loans.balanceSheetReport}${suffix}`);
    }

    async getGlPostings(loanId: string): Promise<LoanGlPosting[]> {
        return httpClient.get<LoanGlPosting[]>(API_ENDPOINTS.loans.glPostings(loanId));
    }

    async configureLoanProduct(data: LoanProductConfigRequest): Promise<any> {
        return httpClient.post<any>(API_ENDPOINTS.loans.configureProduct, data);
    }

    async configureAccountingProfile(data: any): Promise<any> {
        return httpClient.post<any>(API_ENDPOINTS.loans.configureAccountingProfile, data);
    }
}

export const loanService = new LoanService();
