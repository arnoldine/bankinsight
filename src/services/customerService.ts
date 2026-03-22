import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import { ClientDocument, ClientNote, Customer } from '../../types';

interface CustomerApiModel {
  id: string;
  type?: string;
  name?: string;
  email?: string | null;
  phone?: string | null;
  digitalAddress?: string | null;
  kycLevel?: string | null;
  riskRating?: string | null;
  ghanaCard?: string | null;
  employer?: string | null;
  maritalStatus?: string | null;
  spouseName?: string | null;
  dateOfBirth?: string | null;
  gender?: string | null;
  nationality?: string | null;
  tin?: string | null;
  sector?: string | null;
  businessRegNo?: string | null;
  createdAt?: string | null;
  notes?: ClientNote[];
  documents?: ClientDocument[];
}

export interface CustomerKycStatus {
  customerId: string;
  kycLevel: string;
  transactionLimit: number;
  dailyLimit: number;
  remainingDailyLimit: number;
  isUnlimited: boolean;
  ghanaCardMatchesProfile: boolean;
  todayPostedTotal: number;
}

const normalizeKycLevel = (value?: string | null): Customer['kycLevel'] => {
  const normalized = (value || 'Tier 1').trim().toUpperCase().replace(/[_\s]+/g, '');
  if (normalized === 'TIER3') return 'Tier 3';
  if (normalized === 'TIER2') return 'Tier 2';
  return 'Tier 1';
};

const normalizeRiskRating = (value?: string | null): Customer['riskRating'] => {
  const normalized = (value || 'Low').trim().toUpperCase();
  if (normalized === 'HIGH') return 'High';
  if (normalized === 'MEDIUM') return 'Medium';
  return 'Low';
};

const normalizeGender = (value?: string | null): Customer['gender'] | undefined => {
  if (!value) return undefined;
  const normalized = value.trim().toUpperCase();
  if (normalized === 'FEMALE') return 'Female';
  if (normalized === 'OTHER') return 'Other';
  return 'Male';
};

const normalizeMaritalStatus = (value?: string | null): Customer['maritalStatus'] | undefined => {
  if (!value) return undefined;
  const normalized = value.trim().toUpperCase();
  if (normalized === 'MARRIED') return 'Married';
  if (normalized === 'DIVORCED') return 'Divorced';
  if (normalized === 'WIDOWED') return 'Widowed';
  return 'Single';
};

const mapCustomer = (customer: CustomerApiModel): Customer => ({
  id: customer.id,
  cif: customer.id,
  name: customer.name || customer.id,
  ghanaCard: customer.ghanaCard || '',
  digitalAddress: customer.digitalAddress || '',
  kycLevel: normalizeKycLevel(customer.kycLevel),
  phone: customer.phone || '',
  phoneNumber: customer.phone || '',
  email: customer.email || '',
  riskRating: normalizeRiskRating(customer.riskRating),
  registrationDate: customer.createdAt || undefined,
  type: (customer.type?.toUpperCase() === 'CORPORATE' ? 'CORPORATE' : 'INDIVIDUAL'),
  dateOfBirth: customer.dateOfBirth || undefined,
  gender: normalizeGender(customer.gender),
  maritalStatus: normalizeMaritalStatus(customer.maritalStatus),
  spouseName: customer.spouseName || undefined,
  nationality: customer.nationality || undefined,
  employer: customer.employer || undefined,
  tin: customer.tin || undefined,
  sector: customer.sector || undefined,
  businessRegistrationNo: customer.businessRegNo || undefined,
  notes: customer.notes || [],
  documents: customer.documents || [],
});

class CustomerService {
  async getCustomers(): Promise<Customer[]> {
    const customers = await httpClient.get<CustomerApiModel[]>(API_ENDPOINTS.customers.list);
    return customers.map(mapCustomer);
  }

  async getCustomerProfile(id: string): Promise<Customer> {
    const customer = await httpClient.get<CustomerApiModel>(API_ENDPOINTS.customers.profile(id));
    return mapCustomer(customer);
  }

  async getCustomerKyc(id: string): Promise<CustomerKycStatus> {
    return httpClient.get<CustomerKycStatus>(API_ENDPOINTS.customers.kyc(id));
  }

  async createCustomer(data: Partial<Customer>): Promise<Customer> {
    const payload = {
      name: data.name,
      type: data.type || 'INDIVIDUAL',
      ghanaCard: data.ghanaCard,
      digitalAddress: data.digitalAddress,
      kycLevel: data.kycLevel,
      phone: data.phone,
      email: data.email,
      riskRating: data.riskRating,
    };

    const created = await httpClient.post<CustomerApiModel>(API_ENDPOINTS.customers.create, payload);
    return mapCustomer(created);
  }

  async updateCustomer(id: string, data: Partial<Customer>): Promise<Customer> {
    const payload = {
      name: data.name,
      digitalAddress: data.digitalAddress,
      phone: data.phone,
      email: data.email,
      riskRating: data.riskRating,
    };

    const updated = await httpClient.put<CustomerApiModel>(API_ENDPOINTS.customers.update(id), payload);
    return mapCustomer(updated);
  }

  async addCustomerNote(id: string, text: string, category = 'GENERAL'): Promise<ClientNote> {
    return httpClient.post<ClientNote>(API_ENDPOINTS.customers.addNote(id), {
      text,
      category,
    });
  }

  async addCustomerDocument(id: string, type: string, name: string): Promise<ClientDocument> {
    return httpClient.post<ClientDocument>(API_ENDPOINTS.customers.addDocument(id), {
      type,
      name,
    });
  }
}

export const customerService = new CustomerService();
