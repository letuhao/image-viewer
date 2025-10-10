using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Text.Json;

namespace ImageViewer.Application.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly ISystemSettingRepository _settingRepository;
    private readonly ILogger<SystemSettingService> _logger;

    public SystemSettingService(
        ISystemSettingRepository settingRepository,
        ILogger<SystemSettingService> logger)
    {
        _settingRepository = settingRepository;
        _logger = logger;
    }

    public async Task<SystemSetting?> GetSettingAsync(string key)
    {
        var settings = await _settingRepository.GetAllAsync();
        return settings.FirstOrDefault(s => s.SettingKey == key && s.IsActive);
    }

    public async Task<T?> GetSettingValueAsync<T>(string key, T defaultValue = default!)
    {
        try
        {
            var setting = await GetSettingAsync(key);
            if (setting == null)
            {
                _logger.LogDebug("Setting {Key} not found, using default value", key);
                return defaultValue;
            }

            // Handle different types
            if (typeof(T) == typeof(string))
            {
                return (T)(object)setting.SettingValue;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(setting.SettingValue);
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)bool.Parse(setting.SettingValue);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.Parse(setting.SettingValue);
            }
            else
            {
                // Try JSON deserialization for complex types
                return JsonSerializer.Deserialize<T>(setting.SettingValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse setting {Key}, using default value", key);
            return defaultValue;
        }
    }

    public async Task<IEnumerable<SystemSetting>> GetAllSettingsAsync()
    {
        return await _settingRepository.GetAllAsync();
    }

    public async Task<IEnumerable<SystemSetting>> GetSettingsByCategoryAsync(string category)
    {
        var settings = await _settingRepository.GetAllAsync();
        return settings.Where(s => s.Category == category && s.IsActive);
    }

    public async Task<SystemSetting> CreateSettingAsync(string key, string value, string type = "String", string category = "General", string? description = null)
    {
        // Check if setting already exists
        var existing = await GetSettingAsync(key);
        if (existing != null)
        {
            throw new InvalidOperationException($"Setting with key '{key}' already exists");
        }

        var setting = SystemSetting.Create(key, value, type, category, description);
        await _settingRepository.CreateAsync(setting);
        
        _logger.LogInformation("Created system setting: {Key} = {Value}", key, value);
        return setting;
    }

    public async Task<SystemSetting> UpdateSettingAsync(string key, string value, ObjectId? modifiedBy = null)
    {
        var setting = await GetSettingAsync(key);
        if (setting == null)
        {
            throw new InvalidOperationException($"Setting with key '{key}' not found");
        }

        setting.UpdateValue(value, modifiedBy);
        await _settingRepository.UpdateAsync(setting);
        
        _logger.LogInformation("Updated system setting: {Key} = {Value}", key, value);
        return setting;
    }

    public async Task<SystemSetting> UpdateSettingAsync(ObjectId id, string value, ObjectId? modifiedBy = null)
    {
        var setting = await _settingRepository.GetByIdAsync(id);
        if (setting == null)
        {
            throw new InvalidOperationException($"Setting with ID '{id}' not found");
        }

        setting.UpdateValue(value, modifiedBy);
        await _settingRepository.UpdateAsync(setting);
        
        _logger.LogInformation("Updated system setting: {Key} = {Value}", setting.SettingKey, value);
        return setting;
    }

    public async Task DeleteSettingAsync(string key)
    {
        var setting = await GetSettingAsync(key);
        if (setting != null)
        {
            await _settingRepository.DeleteAsync(setting.Id);
            _logger.LogInformation("Deleted system setting: {Key}", key);
        }
    }

    public async Task DeleteSettingAsync(ObjectId id)
    {
        await _settingRepository.DeleteAsync(id);
        _logger.LogInformation("Deleted system setting with ID: {Id}", id);
    }

    // Cache-specific settings
    public async Task<int> GetDefaultCacheQualityAsync()
    {
        return await GetSettingValueAsync("Cache.DefaultQuality", 85); // Optimized for web (default)
    }

    public async Task<string> GetDefaultCacheFormatAsync()
    {
        return await GetSettingValueAsync("Cache.DefaultFormat", "jpeg") ?? "jpeg";
    }

    public async Task<(int width, int height)> GetDefaultCacheDimensionsAsync()
    {
        var width = await GetSettingValueAsync("Cache.DefaultWidth", 1920);
        var height = await GetSettingValueAsync("Cache.DefaultHeight", 1080);
        return (width, height);
    }

    public async Task<bool> GetCachePreserveOriginalAsync()
    {
        return await GetSettingValueAsync("Cache.PreserveOriginal", false);
    }

    // Bulk operation settings
    public async Task<int> GetBulkAddDefaultQualityAsync()
    {
        return await GetSettingValueAsync("BulkAdd.DefaultQuality", 85); // Optimized for web (default)
    }

    public async Task<string> GetBulkAddDefaultFormatAsync()
    {
        return await GetSettingValueAsync("BulkAdd.DefaultFormat", "jpeg") ?? "jpeg";
    }

    public async Task<bool> GetBulkAddAutoScanAsync()
    {
        return await GetSettingValueAsync("BulkAdd.AutoScan", true);
    }

    // Initialize default settings
    public async Task InitializeDefaultSettingsAsync()
    {
        var defaultSettings = new Dictionary<string, (string value, string type, string category, string description)>
        {
            // Image Processing Settings (dot-notation) - Used by IImageProcessingSettingsService
            { "cache.default.format", ("jpeg", "String", "ImageProcessing", "Default image format for cache generation (jpeg, png, webp)") },
            { "cache.default.quality", ("85", "Integer", "ImageProcessing", "Default quality for cache generation (0-100)") },
            { "thumbnail.default.format", ("jpeg", "String", "ImageProcessing", "Default image format for thumbnail generation (jpeg, png, webp)") },
            { "thumbnail.default.quality", ("90", "Integer", "ImageProcessing", "Default quality for thumbnail generation (0-100)") },
            { "thumbnail.default.size", ("300", "Integer", "ImageProcessing", "Default thumbnail size in pixels") },
        };

        foreach (var (key, (value, type, category, description)) in defaultSettings)
        {
            var existing = await GetSettingAsync(key);
            if (existing == null)
            {
                try
                {
                    await CreateSettingAsync(key, value, type, category, description);
                    _logger.LogInformation("✅ Initialized default setting: {Key} = {Value}", key, value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create default setting: {Key}", key);
                }
            }
        }
    }
}

