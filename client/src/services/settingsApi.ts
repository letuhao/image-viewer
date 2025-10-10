import api from './api';

// ============================================================================
// Types
// ============================================================================

export interface UserSettings {
  displaySettings: DisplaySettings;
  privacySettings: PrivacySettings;
  notificationSettings: NotificationSettings;
  language: string;
  timezone: string;
}

export interface DisplaySettings {
  theme: string;
  viewMode: string;
  itemsPerPage: number;
  cardSize: string;
  compactMode: boolean;
  enableAnimations: boolean;
}

export interface PrivacySettings {
  profilePublic: boolean;
  showOnlineStatus: boolean;
  allowAnalytics: boolean;
}

export interface NotificationSettings {
  emailNotifications: boolean;
  pushNotifications: boolean;
  desktopNotifications: boolean;
}

export interface SystemSetting {
  id: string;
  settingKey: string;
  settingValue: string;
  settingType: string;
  category: string;
  description?: string;
  isEditable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateUserSettingsRequest {
  displaySettings?: Partial<DisplaySettings>;
  notificationSettings?: Partial<NotificationSettings>;
  privacySettings?: Partial<PrivacySettings>;
  language?: string;
  timezone?: string;
}

export interface UpdateSystemSettingRequest {
  value: string;
}

export interface CreateSystemSettingRequest {
  key: string;
  value: string;
  type?: string;
  category?: string;
  description?: string;
}

export interface BatchUpdateSettingsRequest {
  settings: Array<{ key: string; value: string }>;
}

// ============================================================================
// User Settings API
// ============================================================================

/**
 * Get current user's settings
 */
export const getUserSettings = async (): Promise<UserSettings> => {
  const response = await api.get<UserSettings>('/usersettings');
  return response.data;
};

/**
 * Update current user's settings
 */
export const updateUserSettings = async (
  request: UpdateUserSettingsRequest
): Promise<UserSettings> => {
  const response = await api.put<UserSettings>('/usersettings', request);
  return response.data;
};

/**
 * Reset user settings to default
 */
export const resetUserSettings = async (): Promise<UserSettings> => {
  const response = await api.post<UserSettings>('/usersettings/reset');
  return response.data;
};

// ============================================================================
// System Settings API
// ============================================================================

/**
 * Get all system settings
 */
export const getAllSystemSettings = async (): Promise<SystemSetting[]> => {
  const response = await api.get<SystemSetting[]>('/systemsettings');
  return response.data;
};

/**
 * Get settings by category
 */
export const getSystemSettingsByCategory = async (
  category: string
): Promise<SystemSetting[]> => {
  const response = await api.get<SystemSetting[]>(`/systemsettings/category/${category}`);
  return response.data;
};

/**
 * Get single system setting by key
 */
export const getSystemSetting = async (key: string): Promise<SystemSetting> => {
  const response = await api.get<SystemSetting>(`/systemsettings/${key}`);
  return response.data;
};

/**
 * Update system setting value
 */
export const updateSystemSetting = async (
  key: string,
  request: UpdateSystemSettingRequest
): Promise<SystemSetting> => {
  const response = await api.put<SystemSetting>(`/systemsettings/${key}`, request);
  return response.data;
};

/**
 * Create new system setting
 */
export const createSystemSetting = async (
  request: CreateSystemSettingRequest
): Promise<SystemSetting> => {
  const response = await api.post<SystemSetting>('/systemsettings', request);
  return response.data;
};

/**
 * Delete system setting
 */
export const deleteSystemSetting = async (key: string): Promise<void> => {
  await api.delete(`/systemsettings/${key}`);
};

/**
 * Batch update system settings
 */
export const batchUpdateSystemSettings = async (
  request: BatchUpdateSettingsRequest
): Promise<{ results: Array<{ key: string; success: boolean; error?: string }> }> => {
  const response = await api.put<{ results: Array<{ key: string; success: boolean; error?: string }> }>(
    '/systemsettings/batch',
    request
  );
  return response.data;
};

