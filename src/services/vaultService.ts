import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface BranchVaultDto {
  id: string;
  branchId: string;
  branchCode: string;
  branchName: string;
  currency: string;
  cashOnHand: number;
  vaultLimit?: number;
  minBalance?: number;
  lastCountDate?: string;
  lastCountBy?: string;
  lastCountByName?: string;
  updatedAt: string;
}

export interface TellerTillSummaryDto {
  tellerId: string;
  tellerName: string;
  branchId?: string;
  branchCode: string;
  branchName: string;
  currency: string;
  isOpen: boolean;
  openedAt?: string;
  closedAt?: string;
  openingBalance: number;
  midDayCashLimit: number;
  allocatedFromVault: number;
  returnedToVault: number;
  cashReceipts: number;
  cashDispensed: number;
  currentBalance: number;
  variance: number;
  status: string;
  lastAction?: string;
  lastActionAt?: string;
  notes?: string;
}

export interface CashDenominationLineDto {
  denomination: string;
  pieces: number;
  fitPieces?: number;
  unfitPieces?: number;
  suspectPieces?: number;
  totalValue: number;
  suspectValue?: number;
}

export interface VaultCountRequest {
  branchId: string;
  currency: string;
  amount: number;
  controlReference?: string;
  countReason?: string;
  witnessOfficer?: string;
  sealNumber?: string;
  denominations?: CashDenominationLineDto[];
}

export interface VaultTransactionRequest {
  branchId: string;
  currency: string;
  amount: number;
  type: string;
  reference?: string;
  narration?: string;
  controlReference?: string;
  witnessOfficer?: string;
  sealNumber?: string;
  denominations?: CashDenominationLineDto[];
}

export interface OpenTillRequest {
  tellerId: string;
  branchId?: string;
  currency: string;
  openingBalance: number;
  midDayCashLimit?: number;
  notes?: string;
  witnessOfficer?: string;
}

export interface TillCashTransferRequest {
  tellerId: string;
  branchId?: string;
  currency: string;
  amount: number;
  reference?: string;
  narration?: string;
  controlReference?: string;
  witnessOfficer?: string;
  sealNumber?: string;
  denominations?: CashDenominationLineDto[];
}

export interface CloseTillRequest {
  tellerId: string;
  branchId?: string;
  currency: string;
  physicalCashCount: number;
  notes?: string;
  controlReference?: string;
  witnessOfficer?: string;
  sealNumber?: string;
  denominations?: CashDenominationLineDto[];
}

class VaultService {
  async getAllVaults(): Promise<BranchVaultDto[]> {
    return httpClient.get<BranchVaultDto[]>(API_ENDPOINTS.vault.all);
  }

  async getBranchVaults(branchId: string): Promise<BranchVaultDto[]> {
    return httpClient.get<BranchVaultDto[]>(API_ENDPOINTS.vault.byBranch(branchId));
  }

  async getVaultDetail(branchId: string, currency: string): Promise<BranchVaultDto> {
    return httpClient.get<BranchVaultDto>(API_ENDPOINTS.vault.detail(branchId, currency));
  }

  async recordVaultCount(data: VaultCountRequest): Promise<BranchVaultDto> {
    return httpClient.post<BranchVaultDto>(API_ENDPOINTS.vault.count, data);
  }

  async processVaultTransaction(data: VaultTransactionRequest): Promise<BranchVaultDto> {
    return httpClient.post<BranchVaultDto>(API_ENDPOINTS.vault.transaction, data);
  }

  async getTillSummaries(branchId?: string, currency = 'GHS'): Promise<TellerTillSummaryDto[]> {
    const params = new URLSearchParams();
    if (branchId) {
      params.append('branchId', branchId);
    }
    params.append('currency', currency);
    return httpClient.get<TellerTillSummaryDto[]>(`${API_ENDPOINTS.vault.tills}?${params.toString()}`);
  }

  async openTill(data: OpenTillRequest): Promise<TellerTillSummaryDto> {
    return httpClient.post<TellerTillSummaryDto>(API_ENDPOINTS.vault.openTill, data);
  }

  async allocateTillCash(data: TillCashTransferRequest): Promise<TellerTillSummaryDto> {
    return httpClient.post<TellerTillSummaryDto>(API_ENDPOINTS.vault.allocateTill, data);
  }

  async returnTillCash(data: TillCashTransferRequest): Promise<TellerTillSummaryDto> {
    return httpClient.post<TellerTillSummaryDto>(API_ENDPOINTS.vault.returnTill, data);
  }

  async closeTill(data: CloseTillRequest): Promise<TellerTillSummaryDto> {
    return httpClient.post<TellerTillSummaryDto>(API_ENDPOINTS.vault.closeTill, data);
  }
}

export const vaultService = new VaultService();
