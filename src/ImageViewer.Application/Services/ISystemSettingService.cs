using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

public interface ISystemSettingService
{
    // Get settings
    Task<SystemSetting?> GetSettingAsync(string key);
    Task<T?> GetSettingValueAsync<T>(string key, T defaultValue = default!);
    Task<IEnumerable<SystemSetting>> GetAllSettingsAsync();
    Task<IEnumerable<SystemSetting>> GetSettingsByCategoryAsync(string category);
    
    // Create/Update settings
    Task<SystemSetting> CreateSettingAsync(string key, string value, string type = "String", string category = "General", string? description = null);
    Task<SystemSetting> UpdateSettingAsync(string key, string value, ObjectId? modifiedBy = null);
    Task<SystemSetting> UpdateSettingAsync(ObjectId id, string value, ObjectId? modifiedBy = null);
    
    // Delete settings
    Task DeleteSettingAsync(string key);
    Task DeleteSettingAsync(ObjectId id);
    
    // Cache-specific settings
    Task<int> GetDefaultCacheQualityAsync();
    Task<string> GetDefaultCacheFormatAsync();
    Task<(int width, int height)> GetDefaultCacheDimensionsAsync();
    Task<bool> GetCachePreserveOriginalAsync();
    
    // Bulk operation settings
    Task<int> GetBulkAddDefaultQualityAsync();
    Task<string> GetBulkAddDefaultFormatAsync();
    Task<bool> GetBulkAddAutoScanAsync();
    
    // Initialize default settings
    Task InitializeDefaultSettingsAsync();
}

