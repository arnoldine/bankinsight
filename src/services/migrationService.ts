import { API_ENDPOINTS } from './apiConfig';
import { httpClient } from './httpClient';

export interface MigrationDatasetInfo {
  key: string;
  name: string;
  templatePath: string;
  description: string;
  requiredColumns: string[];
}

export interface MigrationImportResult {
  dataset: string;
  totalRows: number;
  imported: number;
  updated: number;
  failed: number;
  errors: string[];
}

class MigrationService {
  async getDatasets(): Promise<MigrationDatasetInfo[]> {
    return httpClient.get<MigrationDatasetInfo[]>(API_ENDPOINTS.migration.datasets);
  }

  async importDataset(dataset: string, file: File): Promise<MigrationImportResult> {
    const form = new FormData();
    form.append('file', file);
    return httpClient.postForm<MigrationImportResult>(API_ENDPOINTS.migration.import(dataset), form);
  }
}

export const migrationService = new MigrationService();
