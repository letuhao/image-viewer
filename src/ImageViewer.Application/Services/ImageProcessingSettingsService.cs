using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for managing image processing settings (format, quality)
/// 中文：图像处理设置服务
/// Tiếng Việt: Dịch vụ cài đặt xử lý hình ảnh
/// </summary>
public interface IImageProcessingSettingsService
{
    Task<string> GetCacheFormatAsync();
    Task<int> GetCacheQualityAsync();
    Task<string> GetThumbnailFormatAsync();
    Task<int> GetThumbnailQualityAsync();
    Task<int> GetThumbnailSizeAsync();
}

/// <summary>
/// Implementation of image processing settings service
/// </summary>
public class ImageProcessingSettingsService : IImageProcessingSettingsService
{
    private readonly ISystemSettingService _systemSettingService;
    private readonly ILogger<ImageProcessingSettingsService> _logger;

    // Cache settings in memory for performance
    private string? _cachedCacheFormat;
    private int? _cachedCacheQuality;
    private string? _cachedThumbnailFormat;
    private int? _cachedThumbnailQuality;
    private int? _cachedThumbnailSize;
    private DateTime _lastCacheRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public ImageProcessingSettingsService(
        ISystemSettingService systemSettingService,
        ILogger<ImageProcessingSettingsService> logger)
    {
        _systemSettingService = systemSettingService ?? throw new ArgumentNullException(nameof(systemSettingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetCacheFormatAsync()
    {
        await RefreshCacheIfNeeded();
        return _cachedCacheFormat ?? "jpeg";
    }

    public async Task<int> GetCacheQualityAsync()
    {
        await RefreshCacheIfNeeded();
        return _cachedCacheQuality ?? 85;
    }

    public async Task<string> GetThumbnailFormatAsync()
    {
        await RefreshCacheIfNeeded();
        return _cachedThumbnailFormat ?? "jpeg";
    }

    public async Task<int> GetThumbnailQualityAsync()
    {
        await RefreshCacheIfNeeded();
        return _cachedThumbnailQuality ?? 90;
    }

    public async Task<int> GetThumbnailSizeAsync()
    {
        await RefreshCacheIfNeeded();
        return _cachedThumbnailSize ?? 300;
    }

    private async Task RefreshCacheIfNeeded()
    {
        if (DateTime.UtcNow - _lastCacheRefresh < _cacheExpiration)
        {
            return; // Cache is still valid
        }

        try
        {
            _logger.LogDebug("Refreshing image processing settings cache");

            // Fetch all settings in parallel
            var cacheFormatTask = _systemSettingService.GetSettingAsync("cache.default.format");
            var cacheQualityTask = _systemSettingService.GetSettingAsync("cache.default.quality");
            var thumbnailFormatTask = _systemSettingService.GetSettingAsync("thumbnail.default.format");
            var thumbnailQualityTask = _systemSettingService.GetSettingAsync("thumbnail.default.quality");
            var thumbnailSizeTask = _systemSettingService.GetSettingAsync("thumbnail.default.size");

            await Task.WhenAll(cacheFormatTask, cacheQualityTask, thumbnailFormatTask, thumbnailQualityTask, thumbnailSizeTask);

            // Parse and cache results
            var cacheFormatResult = cacheFormatTask.Result;
            _logger.LogDebug("🔧 ImageProcessingSettingsService: Loaded cache.default.format from DB: {SettingValue}", cacheFormatResult?.SettingValue);
            
            _cachedCacheFormat = cacheFormatResult?.SettingValue?.ToLowerInvariant() ?? "jpeg";
            _cachedCacheQuality = int.TryParse(cacheQualityTask.Result?.SettingValue, out var cq) ? cq : 85;
            _cachedThumbnailFormat = thumbnailFormatTask.Result?.SettingValue?.ToLowerInvariant() ?? "jpeg";
            _cachedThumbnailQuality = int.TryParse(thumbnailQualityTask.Result?.SettingValue, out var tq) ? tq : 90;
            _cachedThumbnailSize = int.TryParse(thumbnailSizeTask.Result?.SettingValue, out var ts) ? ts : 300;

            _lastCacheRefresh = DateTime.UtcNow;

            _logger.LogDebug("Image processing settings refreshed: CacheFormat={CacheFormat}, CacheQuality={CacheQuality}, ThumbnailFormat={ThumbnailFormat}, ThumbnailQuality={ThumbnailQuality}, ThumbnailSize={ThumbnailSize}",
                _cachedCacheFormat, _cachedCacheQuality, _cachedThumbnailFormat, _cachedThumbnailQuality, _cachedThumbnailSize);
                
            _logger.LogInformation("🔧 ImageProcessingSettingsService: Final cached format: {Format} (from DB: {DbValue})", 
                _cachedCacheFormat, cacheFormatResult?.SettingValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh image processing settings, using defaults");
            // Keep existing cached values or use defaults
        }
    }
}

