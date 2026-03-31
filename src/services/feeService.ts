import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface ProductChargeAssessment {
  id?: number;
  productCode: string;
  chargeCode: string;
  chargeName: string;
  chargeType: 'FEE' | 'COMMISSION';
  calculationType: 'FLAT' | 'PERCENTAGE';
  flatAmount?: number;
  rate?: number;
  minimumAmount?: number;
  maximumAmount?: number;
  applyOn: string;
  status: string;
}

export interface ApplyAccountChargeRequest {
  accountId: string;
  chargeCode: string;
  overrideAmount?: number;
  baseAmount?: number;
  narration?: string;
  clientReference?: string;
}

export interface AccountChargeResult {
  transactionId: string;
  accountId: string;
  productCode: string;
  chargeCode: string;
  chargeName: string;
  chargeType: 'FEE' | 'COMMISSION';
  amount: number;
  narration: string;
  postedAt: string;
}

class FeeService {
  async getApplicableCharges(accountId: string, applyOn: string = 'MANUAL'): Promise<ProductChargeAssessment[]> {
    const suffix = applyOn ? `?applyOn=${encodeURIComponent(applyOn)}` : '';
    return httpClient.get<ProductChargeAssessment[]>(`${API_ENDPOINTS.fees.accountCharges(accountId)}${suffix}`);
  }

  async applyAccountCharge(data: ApplyAccountChargeRequest): Promise<AccountChargeResult> {
    return httpClient.post<AccountChargeResult>(API_ENDPOINTS.fees.apply, data);
  }
}

export const feeService = new FeeService();
