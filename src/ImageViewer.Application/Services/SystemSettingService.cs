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
        return await GetSettingValueAsync("Cache.DefaultQuality", 85);
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
        return await GetSettingValueAsync("BulkAdd.DefaultQuality", 95); // Higher quality for bulk operations
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
            // Cache settings
            { "Cache.DefaultQuality", ("85", "Integer", "Cache", "Default JPEG quality for cache generation (0-100)") },
            { "Cache.DefaultFormat", ("jpeg", "String", "Cache", "Default format for cache images (jpeg, webp, original)") },
            { "Cache.DefaultWidth", ("1920", "Integer", "Cache", "Default maximum width for cache images") },
            { "Cache.DefaultHeight", ("1080", "Integer", "Cache", "Default maximum height for cache images") },
            { "Cache.PreserveOriginal", ("false", "Boolean", "Cache", "Preserve original image without resizing") },
            
            // Thumbnail settings
            { "Thumbnail.DefaultSize", ("300", "Integer", "Thumbnail", "Default thumbnail size in pixels") },
            { "Thumbnail.Quality", ("95", "Integer", "Thumbnail", "Thumbnail JPEG quality (0-100)") },
            { "Thumbnail.Format", ("jpeg", "String", "Thumbnail", "Thumbnail format (jpeg, webp)") },
            
            // Bulk operation settings
            { "BulkAdd.DefaultQuality", ("95", "Integer", "BulkOperation", "Default quality for bulk add operations (0-100) - Higher quality recommended") },
            { "BulkAdd.DefaultFormat", ("jpeg", "String", "BulkOperation", "Default format for bulk add cache generation") },
            { "BulkAdd.AutoScan", ("true", "Boolean", "BulkOperation", "Automatically scan collections after bulk add") },
            { "BulkAdd.GenerateCache", ("true", "Boolean", "BulkOperation", "Automatically generate cache for bulk added collections") },
            { "BulkAdd.GenerateThumbnails", ("true", "Boolean", "BulkOperation", "Automatically generate thumbnails for bulk added collections") },
            
            // Quality presets (JSON)
            { "Cache.QualityPresets", (
                JsonSerializer.Serialize(new[] {
                    new { Id = "perfect", Name = "Perfect (100%)", Quality = 100, Format = "jpeg", Description = "Maximum quality, preserve original details" },
                    new { Id = "high", Name = "High Quality (95%)", Quality = 95, Format = "jpeg", Description = "Best quality, larger file size - Recommended for bulk add" },
                    new { Id = "optimize", Name = "Optimized (85%)", Quality = 85, Format = "jpeg", Description = "Balanced quality and file size - Default" },
                    new { Id = "medium", Name = "Medium (75%)", Quality = 75, Format = "jpeg", Description = "Good quality, smaller file size" },
                    new { Id = "low", Name = "Low (60%)", Quality = 60, Format = "jpeg", Description = "Smaller file size, faster loading" },
                    new { Id = "webp", Name = "WebP (85%)", Quality = 85, Format = "webp", Description = "Modern format, excellent compression" },
                    new { Id = "webp-high", Name = "WebP High (95%)", Quality = 95, Format = "webp", Description = "Modern format with high quality" },
                    new { Id = "original", Name = "Original Quality", Quality = 100, Format = "original", Description = "Keep original quality and format (no resize)" }
                }), 
                "JSON", 
                "Cache", 
                "Predefined quality presets for cache generation") 
            },
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

