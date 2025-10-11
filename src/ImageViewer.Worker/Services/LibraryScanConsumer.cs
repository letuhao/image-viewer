using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Messaging;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for library scan messages
/// Scans library folder and creates collections for discovered items
/// </summary>
public class LibraryScanConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public LibraryScanConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<LibraryScanConsumer> logger)
        : base(connection, options, logger, options.Value.LibraryScanQueue ?? "library_scan_queue", "library-scan-consumer")
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("📚 Received library scan message: {Message}", message);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var scanMessage = JsonSerializer.Deserialize<LibraryScanMessage>(message, options);
            if (scanMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize LibraryScanMessage from: {Message}", message);
                return;
            }

            _logger.LogInformation(
                "📚 Processing library scan for library {LibraryId} at path {Path}, JobRunId: {JobRunId}",
                scanMessage.LibraryId,
                scanMessage.LibraryPath,
                scanMessage.JobRunId);

            // Try to create scope, handle disposal gracefully
            IServiceScope? scope = null;
            try
            {
                scope = _serviceScopeFactory.CreateScope();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("⚠️ Service provider disposed, worker is shutting down. Skipping library scan.");
                return;
            }

            using (scope)
            {
                var libraryRepository = scope.ServiceProvider.GetRequiredService<ILibraryRepository>();
                var collectionRepository = scope.ServiceProvider.GetRequiredService<ICollectionRepository>();
                var scheduledJobRunRepository = scope.ServiceProvider.GetRequiredService<IScheduledJobRunRepository>();
                var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();

                // Get the library
                var libraryId = ObjectId.Parse(scanMessage.LibraryId);
                var library = await libraryRepository.GetByIdAsync(libraryId);
                if (library == null)
                {
                    _logger.LogWarning("❌ Library {LibraryId} not found, skipping scan", scanMessage.LibraryId);
                    
                    // Update job run status to failed
                    if (!string.IsNullOrEmpty(scanMessage.JobRunId))
                    {
                        await UpdateJobRunStatusAsync(
                            scheduledJobRunRepository,
                            scanMessage.JobRunId,
                            "Failed",
                            $"Library {scanMessage.LibraryId} not found");
                    }
                    return;
                }

                if (library.IsDeleted)
                {
                    _logger.LogWarning("⚠️ Library {LibraryId} is deleted, skipping scan", scanMessage.LibraryId);
                    
                    if (!string.IsNullOrEmpty(scanMessage.JobRunId))
                    {
                        await UpdateJobRunStatusAsync(
                            scheduledJobRunRepository,
                            scanMessage.JobRunId,
                            "Completed",
                            "Library is deleted, no scan performed");
                    }
                    return;
                }

                // Verify library path exists
                if (!Directory.Exists(scanMessage.LibraryPath))
                {
                    _logger.LogError("❌ Library path does not exist: {Path}", scanMessage.LibraryPath);
                    
                    if (!string.IsNullOrEmpty(scanMessage.JobRunId))
                    {
                        await UpdateJobRunStatusAsync(
                            scheduledJobRunRepository,
                            scanMessage.JobRunId,
                            "Failed",
                            $"Library path does not exist: {scanMessage.LibraryPath}");
                    }
                    return;
                }

                _logger.LogInformation("🔍 Scanning library folder: {Path}", scanMessage.LibraryPath);
                
                // Scan for potential collections
                var collectionFolders = await ScanForCollectionsAsync(
                    scanMessage.LibraryPath,
                    scanMessage.IncludeSubfolders);

                _logger.LogInformation(
                    "📁 Found {Count} potential collection folders in library {LibraryId}",
                    collectionFolders.Count,
                    scanMessage.LibraryId);

                // Create or update collections
                var createdCount = 0;
                var updatedCount = 0;
                var skippedCount = 0;

                foreach (var folderPath in collectionFolders)
                {
                    try
                    {
                        // Check if collection already exists for this path
                        Collection? existingCollection = null;
                        try
                        {
                            existingCollection = await collectionRepository.GetByPathAsync(folderPath);
                        }
                        catch (EntityNotFoundException)
                        {
                            // Collection doesn't exist - this is expected for new collections
                            existingCollection = null;
                        }

                        if (existingCollection != null)
                        {
                            _logger.LogDebug("⏭️ Collection already exists for path: {Path}", folderPath);
                            skippedCount++;
                            
                            // TODO: Optionally trigger re-scan if ForceRescan is true
                            // This would update existing collections with new images
                            continue;
                        }

                        // Create new collection
                        var collectionName = Path.GetFileName(folderPath) ?? folderPath;
                        var collection = new Collection(
                            libraryId: libraryId,
                            name: collectionName,
                            path: folderPath,
                            type: CollectionType.Folder,  // Default to Folder type
                            description: $"Auto-discovered from library: {library.Name}",
                            createdBySystem: "LibraryScanConsumer");

                        var createdCollection = await collectionRepository.CreateAsync(collection);
                        createdCount++;

                        _logger.LogInformation(
                            "✅ Created collection: {CollectionName} (ID: {CollectionId}) at path: {Path}",
                            collectionName,
                            createdCollection.Id,
                            folderPath);

                        // Trigger collection scan to discover images
                        var collectionScanMessage = new CollectionScanMessage
                        {
                            CollectionId = createdCollection.Id.ToString(),
                            CollectionPath = folderPath,
                            CollectionType = CollectionType.Folder,
                            ForceRescan = false,
                            CreatedBy = null,
                            CreatedBySystem = "LibraryScanConsumer",
                            JobId = scanMessage.JobRunId  // Link to parent job for tracking
                        };

                        await messageQueueService.PublishAsync(collectionScanMessage);

                        _logger.LogInformation(
                            "📤 Published collection scan message for collection {CollectionId}",
                            createdCollection.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to process collection folder: {Path}", folderPath);
                    }
                }

                _logger.LogInformation(
                    "✅ Library scan completed. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                    createdCount,
                    updatedCount,
                    skippedCount);

                // Update job run status to completed
                if (!string.IsNullOrEmpty(scanMessage.JobRunId))
                {
                    await UpdateJobRunStatusAsync(
                        scheduledJobRunRepository,
                        scanMessage.JobRunId,
                        "Completed",
                        $"Scan completed. Created: {createdCount}, Updated: {updatedCount}, Skipped: {skippedCount}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing library scan message");
            throw;
        }
    }

    private async Task<List<string>> ScanForCollectionsAsync(string libraryPath, bool includeSubfolders)
    {
        var collectionFolders = new List<string>();

        try
        {
            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            // Get all directories in the library path
            var directories = Directory.GetDirectories(libraryPath, "*", searchOption);
            
            foreach (var directory in directories)
            {
                // Check if directory contains supported image files
                var hasImages = await HasSupportedImagesAsync(directory);
                if (hasImages)
                {
                    collectionFolders.Add(directory);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error scanning library path: {Path}", libraryPath);
        }

        return collectionFolders;
    }

    private async Task<bool> HasSupportedImagesAsync(string directoryPath)
    {
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".zip" };
        
        try
        {
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly);
            return files.Any(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error checking directory for images: {Path}", directoryPath);
            return false;
        }
    }

    private async Task UpdateJobRunStatusAsync(
        IScheduledJobRunRepository repository,
        string jobRunId,
        string status,
        string message)
    {
        try
        {
            var jobRun = await repository.GetByIdAsync(ObjectId.Parse(jobRunId));
            if (jobRun != null)
            {
                if (status == "Failed")
                {
                    jobRun.Fail(message);
                }
                else if (status == "Completed")
                {
                    jobRun.Complete(new Dictionary<string, object>
                    {
                        { "message", message }
                    });
                }
                
                await repository.UpdateAsync(jobRun);
                _logger.LogInformation("✅ Updated job run {JobRunId} status to {Status}", jobRunId, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to update job run status for {JobRunId}", jobRunId);
        }
    }
}

