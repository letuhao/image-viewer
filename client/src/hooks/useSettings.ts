import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'react-hot-toast';
import {
  getUserSettings,
  updateUserSettings,
  resetUserSettings,
  getAllSystemSettings,
  getSystemSettingsByCategory,
  updateSystemSetting,
  batchUpdateSystemSettings,
  type UserSettings,
  type UpdateUserSettingsRequest,
  type SystemSetting,
  type UpdateSystemSettingRequest,
  type BatchUpdateSettingsRequest,
} from '../services/settingsApi';

// ============================================================================
// User Settings Hooks
// ============================================================================

/**
 * Hook to fetch user settings
 */
export const useUserSettings = () => {
  return useQuery<UserSettings, Error>({
    queryKey: ['userSettings'],
    queryFn: getUserSettings,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to update user settings
 */
export const useUpdateUserSettings = () => {
  const queryClient = useQueryClient();

  return useMutation<UserSettings, Error, UpdateUserSettingsRequest>({
    mutationFn: updateUserSettings,
    onSuccess: (data) => {
      queryClient.setQueryData(['userSettings'], data);
      toast.success('Settings saved successfully!');
    },
    onError: (error) => {
      toast.error(`Failed to save settings: ${error.message}`);
    },
  });
};

/**
 * Hook to reset user settings to default
 */
export const useResetUserSettings = () => {
  const queryClient = useQueryClient();

  return useMutation<UserSettings, Error>({
    mutationFn: resetUserSettings,
    onSuccess: (data) => {
      queryClient.setQueryData(['userSettings'], data);
      toast.success('Settings reset to default!');
    },
    onError: (error) => {
      toast.error(`Failed to reset settings: ${error.message}`);
    },
  });
};

// ============================================================================
// System Settings Hooks
// ============================================================================

/**
 * Hook to fetch all system settings
 */
export const useSystemSettings = () => {
  return useQuery<SystemSetting[], Error>({
    queryKey: ['systemSettings'],
    queryFn: getAllSystemSettings,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Hook to fetch system settings by category
 */
export const useSystemSettingsByCategory = (category: string) => {
  return useQuery<SystemSetting[], Error>({
    queryKey: ['systemSettings', category],
    queryFn: () => getSystemSettingsByCategory(category),
    staleTime: 10 * 60 * 1000, // 10 minutes
    enabled: !!category,
  });
};

/**
 * Hook to update a single system setting
 */
export const useUpdateSystemSetting = () => {
  const queryClient = useQueryClient();

  return useMutation<SystemSetting, Error, { key: string; request: UpdateSystemSettingRequest }>({
    mutationFn: ({ key, request }) => updateSystemSetting(key, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['systemSettings'] });
      toast.success(`Setting '${data.settingKey}' updated!`);
    },
    onError: (error) => {
      toast.error(`Failed to update setting: ${error.message}`);
    },
  });
};

/**
 * Hook to batch update system settings
 */
export const useBatchUpdateSystemSettings = () => {
  const queryClient = useQueryClient();

  return useMutation<
    { results: Array<{ key: string; success: boolean; error?: string }> },
    Error,
    BatchUpdateSettingsRequest
  >({
    mutationFn: batchUpdateSystemSettings,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['systemSettings'] });
      
      const successCount = data.results.filter((r) => r.success).length;
      const failCount = data.results.filter((r) => !r.success).length;

      if (failCount === 0) {
        toast.success(`All ${successCount} settings updated successfully!`);
      } else {
        toast.error(`${successCount} updated, ${failCount} failed`);
      }
    },
    onError: (error) => {
      toast.error(`Failed to update settings: ${error.message}`);
    },
  });
};

