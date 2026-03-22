import { ApiError, httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import type {
  GroupCollectionBatch,
  GroupLoanApplication,
  GroupMeeting,
  GroupPortfolioSummary,
  LendingCenter,
  LendingGroup,
  ProductEligibilityRules,
  ProductGroupRules,
} from '../../types';

class GroupLendingService {
  async getGroups(): Promise<LendingGroup[]> {
    try {
      return await httpClient.get<LendingGroup[]>(API_ENDPOINTS.groupLending.groups);
    } catch (error) {
      if (error instanceof ApiError && (error.status === 400 || error.status === 403)) {
        return [];
      }

      throw error;
    }
  }

  async createGroup(payload: Partial<LendingGroup>): Promise<LendingGroup> {
    return httpClient.post<LendingGroup>(API_ENDPOINTS.groupLending.groups, payload);
  }

  async updateGroup(id: string, payload: Partial<LendingGroup>): Promise<LendingGroup> {
    return httpClient.put<LendingGroup>(API_ENDPOINTS.groupLending.group(id), payload);
  }

  async addMember(groupId: string, payload: Record<string, unknown>) {
    return httpClient.post(API_ENDPOINTS.groupLending.groupMembers(groupId), payload);
  }

  async removeMember(groupId: string, memberId: string) {
    return httpClient.delete(API_ENDPOINTS.groupLending.groupMember(groupId, memberId));
  }

  async getCenters(): Promise<LendingCenter[]> {
    try {
      return await httpClient.get<LendingCenter[]>(API_ENDPOINTS.groupLending.centers);
    } catch (error) {
      if (error instanceof ApiError && (error.status === 400 || error.status === 403)) {
        return [];
      }

      throw error;
    }
  }

  async createCenter(payload: Partial<LendingCenter>): Promise<LendingCenter> {
    return httpClient.post<LendingCenter>(API_ENDPOINTS.groupLending.centers, payload);
  }

  async createApplication(payload: Record<string, unknown>): Promise<GroupLoanApplication> {
    return httpClient.post<GroupLoanApplication>(API_ENDPOINTS.groupLending.applications, payload);
  }

  async getApplication(id: string): Promise<GroupLoanApplication> {
    return httpClient.get<GroupLoanApplication>(API_ENDPOINTS.groupLending.application(id));
  }

  async submitApplication(id: string): Promise<GroupLoanApplication> {
    return httpClient.post<GroupLoanApplication>(API_ENDPOINTS.groupLending.submitApplication(id), {});
  }

  async approveApplication(id: string, payload: Record<string, unknown> = {}): Promise<GroupLoanApplication> {
    return httpClient.post<GroupLoanApplication>(API_ENDPOINTS.groupLending.approveApplication(id), payload);
  }

  async rejectApplication(id: string, reason: string): Promise<GroupLoanApplication> {
    return httpClient.post<GroupLoanApplication>(API_ENDPOINTS.groupLending.rejectApplication(id), { reason });
  }

  async disburseApplication(id: string, payload: Record<string, unknown> = {}): Promise<GroupLoanApplication> {
    return httpClient.post<GroupLoanApplication>(API_ENDPOINTS.groupLending.disburseApplication(id), payload);
  }

  async createMeeting(payload: Record<string, unknown>): Promise<GroupMeeting> {
    return httpClient.post<GroupMeeting>(API_ENDPOINTS.groupLending.meetings, payload);
  }

  async getMeeting(id: string): Promise<GroupMeeting> {
    return httpClient.get<GroupMeeting>(API_ENDPOINTS.groupLending.meeting(id));
  }

  async recordAttendance(id: string, payload: Record<string, unknown>): Promise<GroupMeeting> {
    return httpClient.post<GroupMeeting>(API_ENDPOINTS.groupLending.meetingAttendance(id), payload);
  }

  async closeMeeting(id: string): Promise<GroupMeeting> {
    return httpClient.post<GroupMeeting>(API_ENDPOINTS.groupLending.closeMeeting(id), {});
  }

  async createCollectionBatch(payload: Record<string, unknown>): Promise<GroupCollectionBatch> {
    return httpClient.post<GroupCollectionBatch>(API_ENDPOINTS.groupLending.collectionBatches, payload);
  }

  async postCollectionBatch(id: string): Promise<GroupCollectionBatch> {
    return httpClient.post<GroupCollectionBatch>(API_ENDPOINTS.groupLending.postCollectionBatch(id), {});
  }

  async getGroupRules(productId: string): Promise<ProductGroupRules> {
    return httpClient.get<ProductGroupRules>(API_ENDPOINTS.groupLending.productGroupRules(productId));
  }

  async updateGroupRules(productId: string, payload: ProductGroupRules): Promise<ProductGroupRules> {
    return httpClient.put<ProductGroupRules>(API_ENDPOINTS.groupLending.productGroupRules(productId), payload);
  }

  async getEligibilityRules(productId: string): Promise<ProductEligibilityRules> {
    return httpClient.get<ProductEligibilityRules>(API_ENDPOINTS.groupLending.productEligibilityRules(productId));
  }

  async updateEligibilityRules(productId: string, payload: ProductEligibilityRules): Promise<ProductEligibilityRules> {
    return httpClient.put<ProductEligibilityRules>(API_ENDPOINTS.groupLending.productEligibilityRules(productId), payload);
  }

  async getPortfolioSummary(): Promise<GroupPortfolioSummary> {
    return httpClient.get<GroupPortfolioSummary>(API_ENDPOINTS.groupLending.reportsGroupPerformance);
  }

  async getParReport() {
    return httpClient.get(API_ENDPOINTS.groupLending.reportsPar);
  }

  async getOfficerPerformance() {
    return httpClient.get(API_ENDPOINTS.groupLending.reportsOfficerPerformance);
  }

  async getCycleAnalysis() {
    return httpClient.get(API_ENDPOINTS.groupLending.reportsCycleAnalysis);
  }

  async getDelinquencyReport() {
    return httpClient.get(API_ENDPOINTS.groupLending.reportsDelinquency);
  }

  async getMeetingCollectionsReport() {
    return httpClient.get(API_ENDPOINTS.groupLending.reportsMeetingCollections);
  }
}

export const groupLendingService = new GroupLendingService();
