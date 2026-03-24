import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import { DeviceScanResult, FailedLoginAttempt, IrregularTransaction, SecurityAlert, SecurityDevice, SecuritySession, SecuritySummary } from '../../types';

class SecurityService {
  async getSummary(hours: number = 24): Promise<SecuritySummary> {
    return httpClient.get<SecuritySummary>(`${API_ENDPOINTS.security.summary}?sinceHours=${hours}`);
  }

  async getAlerts(limit: number = 25): Promise<SecurityAlert[]> {
    return httpClient.get<SecurityAlert[]>(`${API_ENDPOINTS.security.alerts}?limit=${limit}`);
  }

  async getFailedLogins(sinceMinutes: number = 1440, limit: number = 50): Promise<FailedLoginAttempt[]> {
    return httpClient.get<FailedLoginAttempt[]>(`${API_ENDPOINTS.security.failedLogins}?sinceMinutes=${sinceMinutes}&limit=${limit}`);
  }

  async getSessions(): Promise<SecuritySession[]> {
    return httpClient.get<SecuritySession[]>(API_ENDPOINTS.security.sessions);
  }

  async getDevices(): Promise<SecurityDevice[]> {
    return httpClient.get<SecurityDevice[]>(API_ENDPOINTS.security.devices);
  }

  async registerDevice(payload: Partial<SecurityDevice> & { deviceId: string; name: string; softwareVersion: string }): Promise<SecurityDevice> {
    return httpClient.post<SecurityDevice>(API_ENDPOINTS.security.devices, payload);
  }

  async executeDeviceAction(deviceId: string, action: string, payload: { reason?: string; softwareVersion?: string; minimumSupportedVersion?: string; notes?: string } = {}): Promise<SecurityDevice> {
    return httpClient.post<SecurityDevice>(API_ENDPOINTS.security.deviceActions(deviceId), {
      action,
      ...payload,
    });
  }

  async scanOutdatedDevices(): Promise<DeviceScanResult> {
    return httpClient.post<DeviceScanResult>(API_ENDPOINTS.security.scanOutdated);
  }

  async getIrregularTransactions(hours: number = 72, limit: number = 50): Promise<IrregularTransaction[]> {
    return httpClient.get<IrregularTransaction[]>(`${API_ENDPOINTS.security.irregularTransactions}?hours=${hours}&limit=${limit}`);
  }
}

export const securityService = new SecurityService();
