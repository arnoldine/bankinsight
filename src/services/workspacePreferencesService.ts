import { API_ENDPOINTS } from './apiConfig';
import { httpClient } from './httpClient';

export interface WorkspaceFavorite {
  workspaceKey: string;
  route?: string | null;
  isPinned: boolean;
}

export interface WorkspaceSavedView {
  id: string;
  workspaceKey: string;
  viewName: string;
  route?: string | null;
  filterJson?: string | null;
  isDefault: boolean;
  updatedAt: string;
}

export interface WorkspacePreferencesSummary {
  favorites: WorkspaceFavorite[];
  savedViews: WorkspaceSavedView[];
}

export const workspacePreferencesService = {
  async getSummary(): Promise<WorkspacePreferencesSummary> {
    return httpClient.get<WorkspacePreferencesSummary>(API_ENDPOINTS.workspacePreferences.summary);
  },

  async saveFavorite(workspaceKey: string, payload: { route?: string; isPinned?: boolean } = {}): Promise<void> {
    await httpClient.post(API_ENDPOINTS.workspacePreferences.favorite(workspaceKey), {
      route: payload.route,
      isPinned: payload.isPinned ?? false,
    });
  },

  async removeFavorite(workspaceKey: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.workspacePreferences.favorite(workspaceKey));
  },

  async saveView(payload: { workspaceKey: string; viewName: string; route?: string; filterJson?: string; isDefault?: boolean }): Promise<WorkspaceSavedView> {
    return httpClient.post<WorkspaceSavedView>(API_ENDPOINTS.workspacePreferences.views, {
      workspaceKey: payload.workspaceKey,
      viewName: payload.viewName,
      route: payload.route,
      filterJson: payload.filterJson,
      isDefault: payload.isDefault ?? false,
    });
  },

  async deleteView(id: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.workspacePreferences.view(id));
  },
};
