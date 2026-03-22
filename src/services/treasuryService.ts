import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

// Treasury DTOs
export interface TreasuryPosition {
  id: string;
  currency: string;
  closingBalance: number;
  totalDeposits: number;
  totalWithdrawals: number;
  lastReconciliationDate?: string;
}

export interface FxRate {
  baseCurrency: string;
  quoteCurrency: string;
  bidRate: number;
  askRate: number;
  midRate: number;
  lastUpdated: string;
}

export interface FxTrade {
  id: string;
  buyCurrency: string;
  sellCurrency: string;
  buyAmount: number;
  sellAmount: number;
  exchangeRate: number;
  tradeDate: string;
  valueDate: string;
  counterparty: string;
  tradeType: string;
  status: string;
}

export interface Investment {
  id: string;
  instrumentType: string;
  issuer: string;
  principalAmount: number;
  currency: string;
  interestRate: number;
  purchaseDate: string;
  maturityDate: string;
  tenor: number;
  status: string;
}

export interface RiskMetric {
  id: string;
  metricType: string;
  metricValue: number;
  currency: string;
  calculatedDate: string;
}

class TreasuryService {
  async getTreasuryPositions(): Promise<TreasuryPosition[]> {
    return httpClient.get<TreasuryPosition[]>(API_ENDPOINTS.treasury.positions);
  }

  async getTreasuryPosition(id: string): Promise<TreasuryPosition> {
    return httpClient.get<TreasuryPosition>(API_ENDPOINTS.treasury.positions_detail(id));
  }

  async getFxRates(): Promise<FxRate[]> {
    return httpClient.get<FxRate[]>(API_ENDPOINTS.treasury.fxrates);
  }

  async updateFxRate(data: {
    baseCurrency: string;
    quoteCurrency: string;
    bidRate: number;
    askRate: number;
    midRate: number;
  }): Promise<FxRate> {
    return httpClient.post<FxRate>(API_ENDPOINTS.treasury.fxrates_update, data);
  }

  async getFxTrades(): Promise<FxTrade[]> {
    return httpClient.get<FxTrade[]>(API_ENDPOINTS.treasury.fxtrades);
  }

  async createFxTrade(data: {
    buyCurrency: string;
    sellCurrency: string;
    buyAmount: number;
    sellAmount: number;
    exchangeRate: number;
    valueDate: string;
    counterparty: string;
    tradeType: string;
  }): Promise<FxTrade> {
    return httpClient.post<FxTrade>(API_ENDPOINTS.treasury.fxtrades_create, data);
  }

  async getInvestments(): Promise<Investment[]> {
    return httpClient.get<Investment[]>(API_ENDPOINTS.treasury.investments);
  }

  async createInvestment(data: {
    instrumentType: string;
    issuer: string;
    principalAmount: number;
    currency: string;
    interestRate: number;
    purchaseDate: string;
    maturityDate: string;
    tenor: number;
  }): Promise<Investment> {
    return httpClient.post<Investment>(API_ENDPOINTS.treasury.investments_create, data);
  }

  async getRiskMetrics(): Promise<RiskMetric[]> {
    return httpClient.get<RiskMetric[]>(API_ENDPOINTS.treasury.riskmetrics);
  }
}

export const treasuryService = new TreasuryService();
