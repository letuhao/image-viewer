using MongoDB.Bson;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Discovery service implementation using embedded design
/// Refactored to use ImageEmbedded instead of separate Image entity
/// </summary>
public class DiscoveryService : IDiscoveryService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly IViewSessionRepository _viewSessionRepository;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(
        ICollectionRepository collectionRepository,
        IMediaItemRepository mediaItemRepository,
        IViewSessionRepository viewSessionRepository,
        ILogger<DiscoveryService> logger)
    {
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _mediaItemRepository = mediaItemRepository ?? throw new ArgumentNullException(nameof(mediaItemRepository));
        _viewSessionRepository = viewSessionRepository ?? throw new ArgumentNullException(nameof(viewSessionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Content Discovery

    public async Task<IEnumerable<ContentRecommendation>> DiscoverContentAsync(ObjectId userId, DiscoveryRequest request)
    {
        _logger.LogInformation("Discovering content for user: {UserId}", userId);

        try
        {
            var collections = await _collectionRepository.GetAllAsync();
            var filteredCollections = collections.AsEnumerable();

            // Apply filters
            if (request.LibraryId.HasValue)
            {
                filteredCollections = filteredCollections.Where(c => c.LibraryId == request.LibraryId.Value);
            }

            if (request.CollectionId.HasValue)
            {
                filteredCollections = filteredCollections.Where(c => c.Id == request.CollectionId.Value);
            }

            if (request.CreatedAfter.HasValue)
            {
                filteredCollections = filteredCollections.Where(c => c.CreatedAt >= request.CreatedAfter.Value);
            }

            if (request.CreatedBefore.HasValue)
            {
                filteredCollections = filteredCollections.Where(c => c.CreatedAt <= request.CreatedBefore.Value);
            }

            // Convert to recommendations
            var recommendations = filteredCollections
                .Take(request.Limit)
                .Select(c => new ContentRecommendation
                {
                    Id = c.Id,
                    Title = c.Name,
                    Description = c.Description ?? "",
                    Type = ContentType.Collection,
                    Path = c.Path,
                    RelevanceScore = 0.8,
                    ConfidenceScore = 0.9,
                    Reason = RecommendationReason.Popular,
                    Tags = new List<string>(),
                    Categories = new List<string>(),
                    Metadata = new Dictionary<string, object>(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    ViewCount = c.Statistics.TotalViews,
                    AverageRating = null,
                    RatingCount = 0
                })
                .ToList();

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering content for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<ContentRecommendation>> GetTrendingContentAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 20)
    {
        _logger.LogInformation("Getting trending content from {FromDate} to {ToDate}", fromDate, toDate);

        try
        {
            var collections = await _collectionRepository.GetAllAsync();
            var viewSessions = await _viewSessionRepository.GetAllAsync();

            // Filter view sessions by date range
            var filteredSessions = viewSessions
                .Where(vs => (!fromDate.HasValue || vs.StartedAt >= fromDate.Value) &&
                             (!toDate.HasValue || vs.StartedAt <= toDate.Value))
                .ToList();

            // Group by collection and count views
            var trendingCollections = filteredSessions
                .GroupBy(vs => vs.CollectionId)
                .OrderByDescending(g => g.Count())
                .Take(limit)
                .Select(g =>
                {
                    var collection = collections.FirstOrDefault(c => c.Id == g.Key);
                    return collection != null ? new ContentRecommendation
                    {
                        Id = collection.Id,
                        Title = collection.Name,
                        Description = collection.Description ?? "",
                        Type = ContentType.Collection,
                        Path = collection.Path,
                        RelevanceScore = 0.9,
                        ConfidenceScore = 0.95,
                        Reason = RecommendationReason.Trending,
                        ReasonDescription = $"{g.Count()} recent views",
                        Tags = new List<string>(),
                        Categories = new List<string>(),
                        Metadata = new Dictionary<string, object> { { "recentViews", g.Count() } },
                        CreatedAt = collection.CreatedAt,
                        UpdatedAt = collection.UpdatedAt,
                        ViewCount = collection.Statistics.TotalViews,
                        AverageRating = null,
                        RatingCount = 0
                    } : null;
                })
                .Where(r => r != null)
                .Cast<ContentRecommendation>()
                .ToList();

            return trendingCollections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending content");
            throw;
        }
    }

    public async Task<IEnumerable<ContentRecommendation>> GetPopularContentAsync(TimeSpan period, int limit = 20)
    {
        _logger.LogInformation("Getting popular content for period: {Period}", period);

        try
        {
            var fromDate = DateTime.UtcNow - period;
            return await GetTrendingContentAsync(fromDate, null, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular content");
            throw;
        }
    }

    public async Task<IEnumerable<ContentRecommendation>> GetSimilarContentAsync(ObjectId contentId, ContentType contentType, int limit = 10)
    {
        _logger.LogInformation("Getting similar content for {ContentType} {ContentId}", contentType, contentId);

        try
        {
            // Simple implementation: return collections from the same library
            if (contentType == ContentType.Collection)
            {
                var sourceCollection = await _collectionRepository.GetByIdAsync(contentId);
                if (sourceCollection == null)
                {
                    return Enumerable.Empty<ContentRecommendation>();
                }

                var allCollections = await _collectionRepository.GetAllAsync();
                var similarCollections = allCollections
                    .Where(c => c.Id != contentId && c.LibraryId == sourceCollection.LibraryId)
                    .Take(limit)
                    .Select(c => new ContentRecommendation
                    {
                        Id = c.Id,
                        Title = c.Name,
                        Description = c.Description ?? "",
                        Type = ContentType.Collection,
                        Path = c.Path,
                        RelevanceScore = 0.7,
                        ConfidenceScore = 0.8,
                        Reason = RecommendationReason.Similar,
                        ReasonDescription = "From same library",
                        Tags = new List<string>(),
                        Categories = new List<string>(),
                        Metadata = new Dictionary<string, object>(),
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        ViewCount = c.Statistics.TotalViews,
                        AverageRating = null,
                        RatingCount = 0
                    })
                    .ToList();

                return similarCollections;
            }

            return Enumerable.Empty<ContentRecommendation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar content for {ContentType} {ContentId}", contentType, contentId);
            throw;
        }
    }

    #endregion

    #region Personalized Recommendations

    public async Task<IEnumerable<ContentRecommendation>> GetPersonalizedRecommendationsAsync(ObjectId userId, int limit = 10)
    {
        _logger.LogInformation("Getting personalized recommendations for user: {UserId}", userId);

        try
        {
            // Simple implementation: return most viewed collections
            var collections = await _collectionRepository.GetAllAsync();
            var recommendations = collections
                .OrderByDescending(c => c.Statistics.TotalViews)
                .Take(limit)
                .Select(c => new ContentRecommendation
                {
                    Id = c.Id,
                    Title = c.Name,
                    Description = c.Description ?? "",
                    Type = ContentType.Collection,
                    Path = c.Path,
                    RelevanceScore = 0.85,
                    ConfidenceScore = 0.9,
                    Reason = RecommendationReason.Personalized,
                    ReasonDescription = "Based on popularity",
                    Tags = new List<string>(),
                    Categories = new List<string>(),
                    Metadata = new Dictionary<string, object>(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    ViewCount = c.Statistics.TotalViews,
                    AverageRating = null,
                    RatingCount = 0
                })
                .ToList();

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personalized recommendations for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<ContentRecommendation>> GetRecommendationsByCategoryAsync(ObjectId userId, string category, int limit = 10)
    {
        _logger.LogInformation("Getting recommendations by category for user: {UserId}, category: {Category}", userId, category);
        // Stub implementation
        return Enumerable.Empty<ContentRecommendation>();
    }

    public async Task<IEnumerable<ContentRecommendation>> GetRecommendationsByTagsAsync(ObjectId userId, List<string> tags, int limit = 10)
    {
        _logger.LogInformation("Getting recommendations by tags for user: {UserId}", userId);
        // Stub implementation
        return Enumerable.Empty<ContentRecommendation>();
    }

    public async Task<IEnumerable<ContentRecommendation>> GetRecommendationsByHistoryAsync(ObjectId userId, int limit = 10)
    {
        _logger.LogInformation("Getting recommendations by history for user: {UserId}", userId);

        try
        {
            // Get user's view history and recommend similar content
            var viewSessions = await _viewSessionRepository.GetAllAsync();
            var userSessions = viewSessions.Where(vs => vs.UserId == userId).ToList();

            if (!userSessions.Any())
            {
                return Enumerable.Empty<ContentRecommendation>();
            }

            // Get collections the user has viewed
            var viewedCollectionIds = userSessions.Select(vs => vs.CollectionId).Distinct().ToList();
            var collections = await _collectionRepository.GetAllAsync();
            
            // Recommend collections from the same libraries
            var recommendations = collections
                .Where(c => !viewedCollectionIds.Contains(c.Id))
                .Where(c => viewedCollectionIds.Any(vcid => 
                {
                    var viewedCol = collections.FirstOrDefault(col => col.Id == vcid);
                    return viewedCol != null && viewedCol.LibraryId == c.LibraryId;
                }))
                .Take(limit)
                .Select(c => new ContentRecommendation
                {
                    Id = c.Id,
                    Title = c.Name,
                    Description = c.Description ?? "",
                    Type = ContentType.Collection,
                    Path = c.Path,
                    RelevanceScore = 0.75,
                    ConfidenceScore = 0.85,
                    Reason = RecommendationReason.History,
                    ReasonDescription = "Based on your viewing history",
                    Tags = new List<string>(),
                    Categories = new List<string>(),
                    Metadata = new Dictionary<string, object>(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    ViewCount = c.Statistics.TotalViews,
                    AverageRating = null,
                    RatingCount = 0
                })
                .ToList();

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations by history for user: {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Content Analytics

    public async Task<ContentAnalytics> GetContentAnalyticsAsync(ObjectId? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Getting content analytics for user: {UserId}", userId);

        try
        {
            var collections = await _collectionRepository.GetAllAsync();
            var viewSessions = await _viewSessionRepository.GetAllAsync();

            // Filter by user if specified
            if (userId.HasValue)
            {
                viewSessions = viewSessions.Where(vs => vs.UserId == userId.Value);
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                viewSessions = viewSessions.Where(vs => vs.StartedAt >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                viewSessions = viewSessions.Where(vs => vs.StartedAt <= toDate.Value);
            }

            var viewSessionsList = viewSessions.ToList();
            var totalContent = collections.Count();
            var totalViews = viewSessionsList.Sum(vs => vs.ImagesViewed);
            var totalInteractions = viewSessionsList.Count;

            return new ContentAnalytics
            {
                UserId = userId,
                FromDate = fromDate ?? DateTime.MinValue,
                ToDate = toDate ?? DateTime.UtcNow,
                TotalContent = totalContent,
                TotalViews = totalViews,
                TotalInteractions = totalInteractions,
                AverageRating = 0,
                TopContent = new List<PopularContent>(),
                Trends = new List<ContentTrend>(),
                ContentByCategory = new Dictionary<string, long>(),
                ContentByType = new Dictionary<string, long>
                {
                    { "Collections", totalContent }
                },
                EngagementRate = totalContent > 0 ? (double)totalInteractions / totalContent : 0,
                DiscoveryRate = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content analytics");
            throw;
        }
    }

    public async Task<IEnumerable<ContentTrend>> GetContentTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Getting content trends");
        // Stub implementation
        return Enumerable.Empty<ContentTrend>();
    }

    public async Task<IEnumerable<PopularContent>> GetPopularContentAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 20)
    {
        _logger.LogInformation("Getting popular content");

        try
        {
            var collections = await _collectionRepository.GetAllAsync();
            var popularCollections = collections
                .OrderByDescending(c => c.Statistics.TotalViews)
                .Take(limit)
                .Select(c => new PopularContent
                {
                    Id = c.Id,
                    Title = c.Name,
                    Type = ContentType.Collection,
                    Path = c.Path,
                    ViewCount = c.Statistics.TotalViews,
                    InteractionCount = 0,
                    AverageRating = 0,
                    RatingCount = 0,
                    PopularityScore = c.Statistics.TotalViews,
                    Tags = new List<string>(),
                    Categories = new List<string>(),
                    CreatedAt = c.CreatedAt,
                    LastViewed = c.Statistics.LastViewed ?? DateTime.MinValue
                })
                .ToList();

            return popularCollections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular content");
            throw;
        }
    }

    public async Task<IEnumerable<ContentInsight>> GetContentInsightsAsync(ObjectId? userId = null)
    {
        _logger.LogInformation("Getting content insights for user: {UserId}", userId);
        // Stub implementation
        return Enumerable.Empty<ContentInsight>();
    }

    #endregion

    #region User Preferences & Behavior

    public async Task UpdateUserPreferencesAsync(ObjectId userId, UserDiscoveryPreferences preferences)
    {
        _logger.LogInformation("Updating user preferences for user: {UserId}", userId);
        // Stub implementation - would need UserDiscoveryPreferences entity
        await Task.CompletedTask;
    }

    public async Task<UserDiscoveryPreferences> GetUserPreferencesAsync(ObjectId userId)
    {
        _logger.LogInformation("Getting user preferences for user: {UserId}", userId);
        // Stub implementation - return defaults
        return new UserDiscoveryPreferences
        {
            UserId = userId,
            PreferredCategories = new List<string>(),
            PreferredTags = new List<string>(),
            ExcludedCategories = new List<string>(),
            ExcludedTags = new List<string>(),
            DefaultSortBy = DiscoverySortBy.Relevance,
            DefaultSortOrder = DiscoverySortOrder.Descending,
            DefaultPageSize = 20,
            EnablePersonalizedRecommendations = true,
            EnableTrendingContent = true,
            EnableSimilarContent = true,
            MinRecommendationScore = 0.5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task RecordUserInteractionAsync(ObjectId userId, ObjectId contentId, InteractionType interactionType, double? rating = null)
    {
        _logger.LogInformation("Recording user interaction: User={UserId}, Content={ContentId}, Type={InteractionType}", 
            userId, contentId, interactionType);
        // Stub implementation - would need UserInteraction entity
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<UserInteraction>> GetUserInteractionsAsync(ObjectId userId, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Getting user interactions for user: {UserId}", userId);
        // Stub implementation
        return Enumerable.Empty<UserInteraction>();
    }

    #endregion

    #region Content Categorization

    public async Task<IEnumerable<ContentCategory>> GetContentCategoriesAsync()
    {
        _logger.LogInformation("Getting content categories");
        // Stub implementation
        return Enumerable.Empty<ContentCategory>();
    }

    public async Task<ContentCategory> CreateContentCategoryAsync(CreateContentCategoryRequest request)
    {
        _logger.LogInformation("Creating content category: {Name}", request.Name);
        throw new NotImplementedException("Content categories need entity implementation");
    }

    public async Task<ContentCategory> UpdateContentCategoryAsync(ObjectId categoryId, UpdateContentCategoryRequest request)
    {
        _logger.LogInformation("Updating content category: {CategoryId}", categoryId);
        throw new NotImplementedException("Content categories need entity implementation");
    }

    public async Task DeleteContentCategoryAsync(ObjectId categoryId)
    {
        _logger.LogInformation("Deleting content category: {CategoryId}", categoryId);
        throw new NotImplementedException("Content categories need entity implementation");
    }

    public async Task<IEnumerable<ContentRecommendation>> GetContentByCategoryAsync(string category, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Getting content by category: {Category}", category);
        // Stub implementation
        return Enumerable.Empty<ContentRecommendation>();
    }

    #endregion

    #region Smart Suggestions

    public async Task<IEnumerable<SmartSuggestion>> GetSmartSuggestionsAsync(ObjectId userId, string context = "")
    {
        _logger.LogInformation("Getting smart suggestions for user: {UserId}, context: {Context}", userId, context);
        // Stub implementation
        return Enumerable.Empty<SmartSuggestion>();
    }

    public async Task<IEnumerable<SmartSuggestion>> GetContextualSuggestionsAsync(ObjectId userId, ObjectId currentContentId, int limit = 5)
    {
        _logger.LogInformation("Getting contextual suggestions for user: {UserId}, content: {ContentId}", userId, currentContentId);
        // Stub implementation
        return Enumerable.Empty<SmartSuggestion>();
    }

    public async Task<IEnumerable<SmartSuggestion>> GetTrendingSuggestionsAsync(int limit = 10)
    {
        _logger.LogInformation("Getting trending suggestions");
        // Stub implementation
        return Enumerable.Empty<SmartSuggestion>();
    }

    #endregion
}

