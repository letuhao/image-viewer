using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Cache;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for cache generation messages
/// </summary>
public class CacheGenerationConsumer : BaseMessageConsumer
{
    private readonly IServiceProvider _serviceProvider;

    public CacheGenerationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceProvider serviceProvider,
        ILogger<CacheGenerationConsumer> logger)
        : base(connection, options, logger, "cache.generation", "cache-generation-consumer")
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var cacheMessage = JsonSerializer.Deserialize<CacheGenerationMessage>(message);
            if (cacheMessage == null)
            {
                _logger.LogWarning("Failed to deserialize CacheGenerationMessage");
                return;
            }

            _logger.LogInformation("Processing cache generation for image {ImageId} ({Path})", 
                cacheMessage.ImageId, cacheMessage.ImagePath);

            using var scope = _serviceProvider.CreateScope();
            var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // Determine proper cache path using cache service
            var cachePath = await DetermineCachePath(cacheMessage, cacheService);
            if (string.IsNullOrEmpty(cachePath))
            {
                _logger.LogWarning("❌ Could not determine cache path for image {ImageId}", cacheMessage.ImageId);
                return;
            }

            // Check if cache already exists and force regeneration is disabled
            if (!cacheMessage.ForceRegenerate && File.Exists(cachePath))
            {
                _logger.LogInformation("📁 Cache already exists for image {ImageId}, skipping generation", cacheMessage.ImageId);
                return;
            }

            // Generate cache image using ResizeImageAsync
            var cacheImageData = await imageProcessingService.ResizeImageAsync(
                cacheMessage.ImagePath,
                cacheMessage.CacheWidth,
                cacheMessage.CacheHeight,
                cacheMessage.Quality,
                cancellationToken);

            // Ensure cache directory exists
            var cacheDir = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(cacheDir) && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            // Save cache image to file system
            await File.WriteAllBytesAsync(cachePath, cacheImageData, cancellationToken);

            // Update cache info in database
            await UpdateCacheInfoInDatabase(cacheMessage, cachePath, cacheService);

            _logger.LogInformation("✅ Cache generated for image {ImageId} at path {CachePath} with dimensions {Width}x{Height}", 
                cacheMessage.ImageId, cachePath, cacheMessage.CacheWidth, cacheMessage.CacheHeight);

            _logger.LogInformation("Successfully generated cache for image {ImageId}", cacheMessage.ImageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cache generation message for image {ImageId}", 
                JsonSerializer.Deserialize<CacheGenerationMessage>(message)?.ImageId);
            throw;
        }
    }

    private async Task<string?> DetermineCachePath(CacheGenerationMessage cacheMessage, ICacheService cacheService)
    {
        try
        {
            // Use cache service to determine the proper cache path
            var cacheFolders = await cacheService.GetCacheFoldersAsync();
            if (!cacheFolders.Any())
            {
                _logger.LogWarning("⚠️ No cache folders configured, using default cache directory");
                return Path.Combine("cache", $"{cacheMessage.ImageId}_cache_{cacheMessage.CacheWidth}x{cacheMessage.CacheHeight}.jpg");
            }

            // Select cache folder using hash-based distribution for equal load balancing
            var cacheFolder = SelectCacheFolderForEqualDistribution(cacheFolders, cacheMessage.ImageId);
            var fileName = $"{cacheMessage.ImageId}_cache_{cacheMessage.CacheWidth}x{cacheMessage.CacheHeight}.jpg";
            
            _logger.LogDebug("📁 Selected cache folder {CacheFolderName} for image {ImageId}", cacheFolder.Name, cacheMessage.ImageId);
            return Path.Combine(cacheFolder.Path, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error determining cache path for image {ImageId}", cacheMessage.ImageId);
            return null;
        }
    }

    private CacheFolderDto SelectCacheFolderForEqualDistribution(IEnumerable<CacheFolderDto> cacheFolders, ObjectId imageId)
    {
        // Filter to only active cache folders
        var activeCacheFolders = cacheFolders.Where(cf => cf.IsActive).ToList();
        
        if (!activeCacheFolders.Any())
        {
            throw new InvalidOperationException("No active cache folders available");
        }

        // Use hash-based distribution to ensure equal distribution across cache folders
        // This ensures the same image always goes to the same cache folder (for consistency)
        // while distributing images evenly across all available cache folders
        var hash = Math.Abs(imageId.GetHashCode());
        var selectedIndex = hash % activeCacheFolders.Count;
        var selectedFolder = activeCacheFolders[selectedIndex];
        
        _logger.LogDebug("🎯 Hash-based cache folder selection: ImageId={ImageId}, Hash={Hash}, Index={Index}, SelectedFolder={FolderName}", 
            imageId, hash, selectedIndex, selectedFolder.Name);
        
        return selectedFolder;
    }

    private async Task UpdateCacheInfoInDatabase(CacheGenerationMessage cacheMessage, string cachePath, ICacheService cacheService)
    {
        try
        {
            _logger.LogInformation("📝 Updating cache info in database for image {ImageId}", cacheMessage.ImageId);
            
            // Get file info for the cache file
            var fileInfo = new FileInfo(cachePath);
            var dimensions = $"{cacheMessage.CacheWidth}x{cacheMessage.CacheHeight}";
            var expiresAt = DateTime.UtcNow.AddDays(30); // Cache expires in 30 days
            
            // Create cache info entity
            var cacheInfo = new ImageCacheInfo(
                cacheMessage.ImageId,
                cachePath,
                dimensions,
                fileInfo.Length,
                expiresAt
            );
            
            // Persist the cache info to the database
            using var scope = _serviceProvider.CreateScope();
            var cacheInfoRepository = scope.ServiceProvider.GetRequiredService<IImageCacheInfoRepository>();
            await cacheInfoRepository.CreateAsync(cacheInfo);
            
            _logger.LogInformation("✅ Cache info created and persisted for image {ImageId}: {CachePath}", 
                cacheMessage.ImageId, cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating cache info in database for image {ImageId}", cacheMessage.ImageId);
        }
    }
}
