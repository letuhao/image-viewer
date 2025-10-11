using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for Library operations
/// </summary>
public class LibraryService : ILibraryService
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly IScheduledJobManagementService? _scheduledJobManagementService;
    private readonly ILogger<LibraryService> _logger;

    public LibraryService(
        ILibraryRepository libraryRepository,
        ILogger<LibraryService> logger,
        IScheduledJobManagementService? scheduledJobManagementService = null)
    {
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scheduledJobManagementService = scheduledJobManagementService; // Optional for backward compatibility
    }

    public async Task<Library> CreateLibraryAsync(string name, string path, ObjectId ownerId, string description = "", bool autoScan = false)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Library name cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(path))
                throw new ValidationException("Library path cannot be null or empty");

            // Check if library already exists at this path
            var existingLibrary = await _libraryRepository.GetByPathAsync(path);
            if (existingLibrary != null)
                throw new DuplicateEntityException($"Library at path '{path}' already exists");

            // Create new library
            var library = new Library(name, path, ownerId, description);
            
            // Set AutoScan if requested (must be done before saving)
            if (autoScan)
            {
                var settings = new LibrarySettings();
                settings.UpdateAutoScan(true);
                library.UpdateSettings(settings);
            }
            
            var createdLibrary = await _libraryRepository.CreateAsync(library);

            // Create scheduled job if auto-scan is enabled and scheduler service is available
            _logger.LogInformation(
                "Checking scheduled job creation: AutoScan={AutoScan}, ServiceAvailable={ServiceAvailable}",
                createdLibrary.Settings.AutoScan,
                _scheduledJobManagementService != null);
            
            if (createdLibrary.Settings.AutoScan && _scheduledJobManagementService != null)
            {
                try
                {
                    _logger.LogInformation("Creating scheduled scan job for library {LibraryId}...", createdLibrary.Id);
                    
                    // Default: scan every day at 2 AM
                    var cronExpression = "0 2 * * *";
                    var scheduledJob = await _scheduledJobManagementService.CreateOrUpdateLibraryScanJobAsync(
                        createdLibrary.Id,
                        createdLibrary.Name,
                        cronExpression,
                        isEnabled: true);

                    _logger.LogInformation(
                        "✅ Created scheduled scan job for library {LibraryId} ({LibraryName}). JobId: {JobId}, Enabled: {IsEnabled}",
                        createdLibrary.Id,
                        createdLibrary.Name,
                        scheduledJob.Id,
                        scheduledJob.IsEnabled);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "❌ Failed to create scheduled job for library {LibraryId}, library created but job registration failed",
                        createdLibrary.Id);
                    // Don't throw - library was created successfully
                }
            }
            else if (createdLibrary.Settings.AutoScan && _scheduledJobManagementService == null)
            {
                _logger.LogWarning(
                    "⚠️ AutoScan enabled but IScheduledJobManagementService is not available (DI registration issue)");
            }

            return createdLibrary;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is DuplicateEntityException))
        {
            _logger.LogError(ex, "Failed to create library with name {Name} at path {Path}", name, path);
            throw new BusinessRuleException($"Failed to create library with name '{name}' at path '{path}'", ex);
        }
    }

    public async Task<Library> GetLibraryByIdAsync(ObjectId libraryId)
    {
        try
        {
            var library = await _libraryRepository.GetByIdAsync(libraryId);
            if (library == null)
                throw new EntityNotFoundException($"Library with ID '{libraryId}' not found");
            
            return library;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to get library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> GetLibraryByPathAsync(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ValidationException("Library path cannot be null or empty");

            var library = await _libraryRepository.GetByPathAsync(path);
            if (library == null)
                throw new EntityNotFoundException($"Library at path '{path}' not found");
            
            return library;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get library at path {Path}", path);
            throw new BusinessRuleException($"Failed to get library at path '{path}'", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetLibrariesByOwnerIdAsync(ObjectId ownerId)
    {
        try
        {
            return await _libraryRepository.GetByOwnerIdAsync(ownerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get libraries for owner {OwnerId}", ownerId);
            throw new BusinessRuleException($"Failed to get libraries for owner '{ownerId}'", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetPublicLibrariesAsync()
    {
        try
        {
            return await _libraryRepository.GetPublicLibrariesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get public libraries");
            throw new BusinessRuleException("Failed to get public libraries", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetLibrariesAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _libraryRepository.FindAsync(
                Builders<Library>.Filter.Empty,
                Builders<Library>.Sort.Descending(l => l.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get libraries for page {Page} with page size {PageSize}", page, pageSize);
            throw new BusinessRuleException($"Failed to get libraries for page {page}", ex);
        }
    }

    public async Task<Library> UpdateLibraryAsync(ObjectId libraryId, UpdateLibraryRequest request)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            
            if (request.Name != null)
            {
                library.UpdateName(request.Name);
            }
            
            if (request.Description != null)
            {
                library.UpdateDescription(request.Description);
            }
            
            if (request.Path != null)
            {
                // Check if path is already taken by another library
                var existingLibrary = await _libraryRepository.GetByPathAsync(request.Path);
                if (existingLibrary != null && existingLibrary.Id != libraryId)
                    throw new DuplicateEntityException($"Library at path '{request.Path}' already exists");
                
                library.UpdatePath(request.Path);
            }
            
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException || ex is DuplicateEntityException))
        {
            _logger.LogError(ex, "Failed to update library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to update library with ID '{libraryId}'", ex);
        }
    }

    public async Task DeleteLibraryAsync(ObjectId libraryId)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            
            // Delete the library
            await _libraryRepository.DeleteAsync(libraryId);

            // Delete associated scheduled job if scheduler service is available
            if (_scheduledJobManagementService != null)
            {
                try
                {
                    var existingJob = await _scheduledJobManagementService.GetJobByLibraryIdAsync(libraryId);
                    if (existingJob != null)
                    {
                        await _scheduledJobManagementService.DeleteJobAsync(existingJob.Id);
                        _logger.LogInformation(
                            "Deleted scheduled job {JobId} for library {LibraryId}",
                            existingJob.Id,
                            libraryId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete scheduled job for library {LibraryId}, library deleted but job cleanup failed",
                        libraryId);
                    // Don't throw - library was deleted successfully
                }
            }
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to delete library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to delete library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> UpdateSettingsAsync(ObjectId libraryId, UpdateLibrarySettingsRequest request)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            var oldAutoScan = library.Settings.AutoScan;
            
            var newSettings = new LibrarySettings();
            
            if (request.AutoScan.HasValue)
                newSettings.UpdateAutoScan(request.AutoScan.Value);
            
            if (request.GenerateThumbnails.HasValue)
                newSettings.UpdateGenerateThumbnails(request.GenerateThumbnails.Value);
            
            if (request.GenerateCache.HasValue)
                newSettings.UpdateGenerateCache(request.GenerateCache.Value);
            
            if (request.EnableWatching.HasValue)
                newSettings.UpdateEnableWatching(request.EnableWatching.Value);
            
            if (request.ScanInterval.HasValue)
                newSettings.UpdateScanInterval(request.ScanInterval.Value);
            
            if (request.MaxFileSize.HasValue)
                newSettings.UpdateMaxFileSize(request.MaxFileSize.Value);
            
            if (request.AllowedFormats != null)
            {
                foreach (var format in request.AllowedFormats)
                {
                    newSettings.AddAllowedFormat(format);
                }
            }
            
            if (request.ExcludedPaths != null)
            {
                foreach (var path in request.ExcludedPaths)
                {
                    newSettings.AddExcludedPath(path);
                }
            }
            
            if (request.ThumbnailSettings != null)
                newSettings.UpdateThumbnailSettings(request.ThumbnailSettings);
            
            if (request.CacheSettings != null)
                newSettings.UpdateCacheSettings(request.CacheSettings);
            
            library.UpdateSettings(newSettings);
            var updatedLibrary = await _libraryRepository.UpdateAsync(library);

            // Handle scheduled job based on AutoScan setting change
            if (_scheduledJobManagementService != null && request.AutoScan.HasValue && request.AutoScan.Value != oldAutoScan)
            {
                try
                {
                    var existingJob = await _scheduledJobManagementService.GetJobByLibraryIdAsync(libraryId);
                    
                    if (request.AutoScan.Value)
                    {
                        // AutoScan enabled - create or enable job
                        if (existingJob != null)
                        {
                            await _scheduledJobManagementService.EnableJobAsync(existingJob.Id);
                            _logger.LogInformation("Enabled scheduled job for library {LibraryId}", libraryId);
                        }
                        else
                        {
                            var cronExpression = "0 2 * * *"; // Default: 2 AM daily
                            await _scheduledJobManagementService.CreateOrUpdateLibraryScanJobAsync(
                                libraryId,
                                updatedLibrary.Name,
                                cronExpression,
                                isEnabled: true);
                            _logger.LogInformation("Created scheduled job for library {LibraryId}", libraryId);
                        }
                    }
                    else
                    {
                        // AutoScan disabled - disable job
                        if (existingJob != null)
                        {
                            await _scheduledJobManagementService.DisableJobAsync(existingJob.Id);
                            _logger.LogInformation("Disabled scheduled job for library {LibraryId}", libraryId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to update scheduled job for library {LibraryId}, settings updated but job synchronization failed",
                        libraryId);
                    // Don't throw - settings were updated successfully
                }
            }

            return updatedLibrary;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update settings for library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to update settings for library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> UpdateMetadataAsync(ObjectId libraryId, UpdateLibraryMetadataRequest request)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            
            var newMetadata = new LibraryMetadata();
            
            if (request.Description != null)
                newMetadata.UpdateDescription(request.Description);
            
            if (request.Tags != null)
            {
                foreach (var tag in request.Tags)
                {
                    newMetadata.AddTag(tag);
                }
            }
            
            if (request.Categories != null)
            {
                foreach (var category in request.Categories)
                {
                    newMetadata.AddCategory(category);
                }
            }
            
            if (request.CustomFields != null)
            {
                foreach (var field in request.CustomFields)
                {
                    newMetadata.AddCustomField(field.Key, field.Value);
                }
            }
            
            if (request.Version != null)
                newMetadata.UpdateVersion(request.Version);
            
            if (request.CreatedBy != null)
                newMetadata.UpdateCreatedBy(request.CreatedBy);
            
            if (request.ModifiedBy != null)
                newMetadata.UpdateModifiedBy(request.ModifiedBy);
            
            library.UpdateMetadata(newMetadata);
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update metadata for library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to update metadata for library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> UpdateStatisticsAsync(ObjectId libraryId, UpdateLibraryStatisticsRequest request)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            
            var newStatistics = new ImageViewer.Domain.ValueObjects.LibraryStatistics();
            
            if (request.TotalCollections.HasValue)
                newStatistics.IncrementCollections(request.TotalCollections.Value);
            
            if (request.TotalMediaItems.HasValue)
                newStatistics.IncrementMediaItems(request.TotalMediaItems.Value);
            
            if (request.TotalSize.HasValue)
                newStatistics.IncrementSize(request.TotalSize.Value);
            
            if (request.TotalViews.HasValue)
                newStatistics.IncrementViews(request.TotalViews.Value);
            
            if (request.TotalDownloads.HasValue)
                newStatistics.IncrementDownloads(request.TotalDownloads.Value);
            
            if (request.TotalShares.HasValue)
                newStatistics.IncrementShares(request.TotalShares.Value);
            
            if (request.TotalLikes.HasValue)
                newStatistics.IncrementLikes(request.TotalLikes.Value);
            
            if (request.TotalComments.HasValue)
                newStatistics.IncrementComments(request.TotalComments.Value);
            
            if (request.LastScanDate.HasValue)
                newStatistics.UpdateLastScanDate(request.LastScanDate.Value);
            
            if (request.LastActivity.HasValue)
                newStatistics.UpdateLastActivity();
            
            library.UpdateStatistics(newStatistics);
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update statistics for library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to update statistics for library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> ActivateLibraryAsync(ObjectId libraryId)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            library.Activate();
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to activate library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to activate library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> DeactivateLibraryAsync(ObjectId libraryId)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            library.Deactivate();
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to deactivate library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to deactivate library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> SetPublicAsync(ObjectId libraryId, bool isPublic)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            library.SetPublic(isPublic);
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to set library visibility for library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to set library visibility for library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> EnableWatchingAsync(ObjectId libraryId)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            library.EnableWatching();
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to enable watching for library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to enable watching for library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> DisableWatchingAsync(ObjectId libraryId)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            library.DisableWatching();
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to disable watching for library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to disable watching for library with ID '{libraryId}'", ex);
        }
    }

    public async Task<Library> UpdateWatchSettingsAsync(ObjectId libraryId, UpdateWatchSettingsRequest request)
    {
        try
        {
            var library = await GetLibraryByIdAsync(libraryId);
            
            if (request.IsWatching.HasValue)
            {
                if (request.IsWatching.Value)
                {
                    library.EnableWatching();
                }
                else
                {
                    library.DisableWatching();
                }
            }
            
            if (request.WatchPath != null)
            {
                library.WatchInfo.UpdateWatchPath(request.WatchPath);
            }
            
            if (request.WatchFilters != null)
            {
                foreach (var filter in request.WatchFilters)
                {
                    library.WatchInfo.AddWatchFilter(filter);
                }
            }
            
            return await _libraryRepository.UpdateAsync(library);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update watch settings for library with ID {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to update watch settings for library with ID '{libraryId}'", ex);
        }
    }

    public async Task<IEnumerable<Library>> SearchLibrariesAsync(string query, int page = 1, int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ValidationException("Search query cannot be null or empty");
            
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _libraryRepository.FindAsync(
                Builders<Library>.Filter.Or(
                    Builders<Library>.Filter.Regex(l => l.Name, new BsonRegularExpression(query, "i")),
                    Builders<Library>.Filter.Regex(l => l.Description, new BsonRegularExpression(query, "i")),
                    Builders<Library>.Filter.Regex(l => l.Path, new BsonRegularExpression(query, "i"))
                ),
                Builders<Library>.Sort.Descending(l => l.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to search libraries with query {Query}", query);
            throw new BusinessRuleException($"Failed to search libraries with query '{query}'", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetLibrariesByFilterAsync(LibraryFilterRequest filter, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var libraryFilter = new LibraryFilter
            {
                OwnerId = filter.OwnerId,
                IsPublic = filter.IsPublic,
                IsActive = filter.IsActive,
                CreatedAfter = filter.CreatedAfter,
                CreatedBefore = filter.CreatedBefore,
                Path = filter.Path,
                Tags = filter.Tags,
                Categories = filter.Categories
            };

            var skip = (page - 1) * pageSize;
            return await _libraryRepository.FindAsync(
                Builders<Library>.Filter.Empty,
                Builders<Library>.Sort.Descending(l => l.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get libraries by filter");
            throw new BusinessRuleException("Failed to get libraries by filter", ex);
        }
    }

    public async Task<ImageViewer.Domain.ValueObjects.LibraryStatistics> GetLibraryStatisticsAsync()
    {
        try
        {
            return await _libraryRepository.GetLibraryStatisticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library statistics");
            throw new BusinessRuleException("Failed to get library statistics", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetTopLibrariesByActivityAsync(int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
                throw new ValidationException("Limit must be between 1 and 100");

            return await _libraryRepository.GetTopLibrariesByActivityAsync(limit);
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get top libraries by activity");
            throw new BusinessRuleException("Failed to get top libraries by activity", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetRecentLibrariesAsync(int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
                throw new ValidationException("Limit must be between 1 and 100");

            return await _libraryRepository.GetRecentLibrariesAsync(limit);
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get recent libraries");
            throw new BusinessRuleException("Failed to get recent libraries", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetLibrariesByOwnerAsync(ObjectId ownerId, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _libraryRepository.FindAsync(
                Builders<Library>.Filter.Eq(l => l.OwnerId, ownerId),
                Builders<Library>.Sort.Descending(l => l.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get libraries for owner {OwnerId}", ownerId);
            throw new BusinessRuleException($"Failed to get libraries for owner '{ownerId}'", ex);
        }
    }
}
