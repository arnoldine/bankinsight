import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface Investment {
    id: string;
    investmentNumber: string;
    instrument: string;
    currency: 'GHS' | 'USD';
    principalAmount: number;
    interestRate: number;
    placementDate: string;
    maturityDate: string;
    status: 'ACTIVE' | 'MATURED' | 'LIQUIDATED' | 'SCHEDULED';
    daysToMaturity: number;
    accruedInterest: number;
    maturityValue: number;
    investmentType: 'FIXED_DEPOSIT' | 'TREASURY_BILL' | 'BOND' | 'CERTIFICATE';
    counterparty?: string;
}

export interface FixedDeposit {
    id: string;
    customerId: string;
    accountId: string;
    principal: number;
    rate: number;
    tenure: number; // Days
    startDate: string;
    maturityDate: string;
    currency: 'GHS' | 'USD';
    interestPaymentFrequency: 'MONTHLY' | 'QUARTERLY' | 'AT_MATURITY';
    status: 'ACTIVE' | 'MATURED' | 'CLOSED';
    accruedInterest: number;
    maturityValue: number;
}

export interface CreateInvestmentRequest {
    instrument: string;
    currency: 'GHS' | 'USD';
    principalAmount: number;
    interestRate: number;
    placementDate: string;
    maturityDate: string;
    investmentType: string;
}

export interface CreateFixedDepositRequest {
    customerId: string;
    accountId: string;
    principal: number;
    rate: number;
    tenure: number;
    currency: 'GHS' | 'USD';
    interestPaymentFrequency: string;
}

class InvestmentService {
    async getInvestments(): Promise<Investment[]> {
        return httpClient.get<Investment[]>(API_ENDPOINTS.treasury.investments);
    }

    async getInvestment(id: string): Promise<Investment> {
        return httpClient.get<Investment>(`${API_ENDPOINTS.treasury.investments}/${id}`);
    }

    async createInvestment(data: CreateInvestmentRequest): Promise<Investment> {
        return httpClient.post<Investment>(API_ENDPOINTS.treasury.investments_create, data);
    }

    async liquidateInvestment(id: string): Promise<{ message: string; maturityValue: number }> {
        return httpClient.post(`${API_ENDPOINTS.treasury.investments}/${id}/liquidate`, {});
    }

    // Fixed Deposits
    async getFixedDeposits(): Promise<FixedDeposit[]> {
        return httpClient.get<FixedDeposit[]>(API_ENDPOINTS.deposits.list);
    }

    async getFixedDeposit(id: string): Promise<FixedDeposit> {
        return httpClient.get<FixedDeposit>(API_ENDPOINTS.deposits.get(id));
    }

    async createFixedDeposit(data: CreateFixedDepositRequest): Promise<FixedDeposit> {
        return httpClient.post<FixedDeposit>(API_ENDPOINTS.deposits.create, data);
    }

    async renewFixedDeposit(id: string, principal: number, tenure: number): Promise<FixedDeposit> {
        return httpClient.post<FixedDeposit>(API_ENDPOINTS.deposits.renew(id), { principal, tenure });
    }

    async closeFixedDeposit(id: string): Promise<{ message: string; finalAmount: number }> {
        return httpClient.post(API_ENDPOINTS.deposits.close(id), {});
    }

    async getPortfolioSummary(): Promise<{
        totalInvestments: number;
        totalPrincipal: number;
        totalAccruedInterest: number;
        totalMaturityValue: number;
        averageYield: number;
        byType: Record<string, number>;
        byCurrency: Record<string, number>;
    }> {
        return httpClient.get(`${API_ENDPOINTS.treasury.investments}/portfolio`);
    }
}

export const investmentService = new InvestmentService();
