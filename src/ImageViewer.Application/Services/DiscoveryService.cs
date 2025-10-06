using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for content discovery and recommendation operations
/// </summary>
public class DiscoveryService : IDiscoveryService
{
    private readonly IUserRepository _userRepository;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly IImageRepository _imageRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(
        IUserRepository userRepository,
        ILibraryRepository libraryRepository,
        ICollectionRepository collectionRepository,
        IMediaItemRepository mediaItemRepository,
        IImageRepository imageRepository,
        ITagRepository tagRepository,
        ILogger<DiscoveryService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _mediaItemRepository = mediaItemRepository ?? throw new ArgumentNullException(nameof(mediaItemRepository));
        _imageRepository = imageRepository ?? throw new ArgumentNullException(nameof(imageRepository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Content Discovery

    public async Task<IEnumerable<ContentRecommendation>> DiscoverContentAsync(ObjectId userId, DiscoveryRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Discovering content for user {UserId}", userId);

        var recommendations = new List<ContentRecommendation>();

        // Get user preferences
        var userPreferences = await GetUserPreferencesAsync(userId);

        // Discover libraries
        var libraries = await _libraryRepository.GetAllAsync();
        foreach (var library in libraries)
        {
            if (ShouldIncludeContent(library, request, userPreferences))
            {
                recommendations.Add(CreateContentRecommendation(library, ContentType.Library, RecommendationReason.Popular));
            }
        }

        // Discover collections
        var collections = await _collectionRepository.GetAllAsync();
        foreach (var collection in collections)
        {
            if (ShouldIncludeContent(collection, request, userPreferences))
            {
                recommendations.Add(CreateContentRecommendation(collection, ContentType.Collection, RecommendationReason.Popular));
            }
        }

        // Discover media items
        var mediaItems = await _mediaItemRepository.GetAllAsync();
        foreach (var mediaItem in mediaItems)
        {
            if (ShouldIncludeContent(mediaItem, request, userPreferences))
            {
                recommendations.Add(CreateContentRecommendation(mediaItem, ContentType.MediaItem, RecommendationReason.Popular));
            }
        }

        // Sort and limit results
        return SortAndLimitRecommendations(recommendations, request);
    }

    public async Task<IEnumerable<ContentRecommendation>> GetTrendingContentAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 20)
    {
        _logger.LogInformation("Getting trending content from {FromDate} to {ToDate}", fromDate, toDate);

        var trendingContent = new List<ContentRecommendation>();
        var cutoffDate = fromDate ?? DateTime.UtcNow.AddDays(-7);
        var endDate = toDate ?? DateTime.UtcNow;

        // Get trending libraries
        var libraries = await _libraryRepository.GetAllAsync();
        foreach (var library in libraries.Where(l => l.CreatedAt >= cutoffDate && l.CreatedAt <= endDate))
        {
            trendingContent.Add(CreateContentRecommendation(library, ContentType.Library, RecommendationReason.Trending));
        }

        // Get trending collections
        var collections = await _collectionRepository.GetAllAsync();
        foreach (var collection in collections.Where(c => c.CreatedAt >= cutoffDate && c.CreatedAt <= endDate))
        {
            trendingContent.Add(CreateContentRecommendation(collection, ContentType.Collection, RecommendationReason.Trending));
        }

        // Get trending media items
        var mediaItems = await _mediaItemRepository.GetAllAsync();
        foreach (var mediaItem in mediaItems.Where(m => m.CreatedAt >= cutoffDate && m.CreatedAt <= endDate))
        {
            trendingContent.Add(CreateContentRecommendation(mediaItem, ContentType.MediaItem, RecommendationReason.Trending));
        }

        return trendingContent
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit);
    }

    public async Task<IEnumerable<ContentRecommendation>> GetPopularContentAsync(TimeSpan period, int limit = 20)
    {
        _logger.LogInformation("Getting popular content for period {Period}", period);

        var popularContent = new List<ContentRecommendation>();
        var cutoffDate = DateTime.UtcNow.Subtract(period);

        // Get popular libraries
        var libraries = await _libraryRepository.GetAllAsync();
        foreach (var library in libraries.Where(l => l.CreatedAt >= cutoffDate))
        {
            popularContent.Add(CreateContentRecommendation(library, ContentType.Library, RecommendationReason.Popular));
        }

        // Get popular collections
        var collections = await _collectionRepository.GetAllAsync();
        foreach (var collection in collections.Where(c => c.CreatedAt >= cutoffDate))
        {
            popularContent.Add(CreateContentRecommendation(collection, ContentType.Collection, RecommendationReason.Popular));
        }

        // Get popular media items
        var mediaItems = await _mediaItemRepository.GetAllAsync();
        foreach (var mediaItem in mediaItems.Where(m => m.CreatedAt >= cutoffDate))
        {
            popularContent.Add(CreateContentRecommendation(mediaItem, ContentType.MediaItem, RecommendationReason.Popular));
        }

        return popularContent
            .OrderByDescending(c => c.ViewCount)
            .Take(limit);
    }

    public async Task<IEnumerable<ContentRecommendation>> GetSimilarContentAsync(ObjectId contentId, ContentType contentType, int limit = 10)
    {
        _logger.LogInformation("Getting similar content for {ContentType} {ContentId}", contentType, contentId);

        var similarContent = new List<ContentRecommendation>();

        switch (contentType)
        {
            case ContentType.Library:
                var library = await _libraryRepository.GetByIdAsync(contentId);
                if (library != null)
                {
                    var similarLibraries = await _libraryRepository.GetAllAsync();
                    foreach (var similarLibrary in similarLibraries.Where(l => l.Id != contentId))
                    {
                        similarContent.Add(CreateContentRecommendation(similarLibrary, ContentType.Library, RecommendationReason.Similar));
                    }
                }
                break;

            case ContentType.Collection:
                var collection = await _collectionRepository.GetByIdAsync(contentId);
                if (collection != null)
                {
                    var similarCollections = await _collectionRepository.GetAllAsync();
                    foreach (var similarCollection in similarCollections.Where(c => c.Id != contentId))
                    {
                        similarContent.Add(CreateContentRecommendation(similarCollection, ContentType.Collection, RecommendationReason.Similar));
                    }
                }
                break;

            case ContentType.MediaItem:
                var mediaItem = await _mediaItemRepository.GetByIdAsync(contentId);
                if (mediaItem != null)
                {
                    var similarMediaItems = await _mediaItemRepository.GetAllAsync();
                    foreach (var similarItem in similarMediaItems.Where(m => m.Id != contentId))
                    {
                        similarContent.Add(CreateContentRecommendation(similarItem, ContentType.MediaItem, RecommendationReason.Similar));
                    }
                }
                break;
        }

        return similarContent.Take(limit);
    }

    #endregion

    #region Personalized Recommendations

    public async Task<IEnumerable<ContentRecommendation>> GetPersonalizedRecommendationsAsync(ObjectId userId, int limit = 10)
    {
        _logger.LogInformation("Getting personalized recommendations for user {UserId}", userId);

        var userPreferences = await GetUserPreferencesAsync(userId);
        var recommendations = new List<ContentRecommendation>();

        // Get recommendations based on user preferences
        if (userPreferences.EnablePersonalizedRecommendations)
        {
            var request = new DiscoveryRequest
            {
                Categories = userPreferences.PreferredCategories,
                Tags = userPreferences.PreferredTags,
                Limit = limit
            };

            recommendations.AddRange(await DiscoverContentAsync(userId, request));
        }

        return recommendations.Take(limit);
    }

    public async Task<IEnumerable<ContentRecommendation>> GetRecommendationsByCategoryAsync(ObjectId userId, string category, int limit = 10)
    {
        _logger.LogInformation("Getting recommendations by category {Category} for user {UserId}", category, userId);

        var request = new DiscoveryRequest
        {
            Categories = new List<string> { category },
            Limit = limit
        };

        return await DiscoverContentAsync(userId, request);
    }

    public async Task<IEnumerable<ContentRecommendation>> GetRecommendationsByTagsAsync(ObjectId userId, List<string> tags, int limit = 10)
    {
        _logger.LogInformation("Getting recommendations by tags {Tags} for user {UserId}", string.Join(", ", tags), userId);

        var request = new DiscoveryRequest
        {
            Tags = tags,
            Limit = limit
        };

        return await DiscoverContentAsync(userId, request);
    }

    public async Task<IEnumerable<ContentRecommendation>> GetRecommendationsByHistoryAsync(ObjectId userId, int limit = 10)
    {
        _logger.LogInformation("Getting recommendations by history for user {UserId}", userId);

        // Get user interactions to understand preferences
        var interactions = await GetUserInteractionsAsync(userId, 1, 50);
        var recommendations = new List<ContentRecommendation>();

        // Get similar content based on user's interaction history
        foreach (var interaction in interactions.Take(10))
        {
            var similarContent = await GetSimilarContentAsync(interaction.ContentId, ContentType.MediaItem, 1);
            recommendations.AddRange(similarContent);
        }

        return recommendations.Take(limit);
    }

    #endregion

    #region Content Analytics

    public async Task<ContentAnalytics> GetContentAnalyticsAsync(ObjectId? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Getting content analytics for user {UserId}", userId);

        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var analytics = new ContentAnalytics
        {
            UserId = userId,
            FromDate = from,
            ToDate = to,
            TotalContent = 0,
            TotalViews = 0,
            TotalInteractions = 0,
            AverageRating = 0.0,
            EngagementRate = 0.0,
            DiscoveryRate = 0.0
        };

        // Get content counts
        var libraries = await _libraryRepository.GetAllAsync();
        var collections = await _collectionRepository.GetAllAsync();
        var mediaItems = await _mediaItemRepository.GetAllAsync();

        analytics.TotalContent = libraries.Count() + collections.Count() + mediaItems.Count();

        // Calculate analytics based on content
        foreach (var library in libraries.Where(l => l.CreatedAt >= from && l.CreatedAt <= to))
        {
            analytics.ContentByType["Library"] = analytics.ContentByType.GetValueOrDefault("Library", 0) + 1;
        }

        foreach (var collection in collections.Where(c => c.CreatedAt >= from && c.CreatedAt <= to))
        {
            analytics.ContentByType["Collection"] = analytics.ContentByType.GetValueOrDefault("Collection", 0) + 1;
        }

        foreach (var mediaItem in mediaItems.Where(m => m.CreatedAt >= from && m.CreatedAt <= to))
        {
            analytics.ContentByType["MediaItem"] = analytics.ContentByType.GetValueOrDefault("MediaItem", 0) + 1;
        }

        return analytics;
    }

    public async Task<IEnumerable<ContentTrend>> GetContentTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Getting content trends from {FromDate} to {ToDate}", fromDate, toDate);

        var trends = new List<ContentTrend>();
        var from = fromDate ?? DateTime.UtcNow.AddDays(-7);
        var to = toDate ?? DateTime.UtcNow;

        // Get trending libraries
        var libraries = await _libraryRepository.GetAllAsync();
        foreach (var library in libraries.Where(l => l.CreatedAt >= from && l.CreatedAt <= to))
        {
            trends.Add(new ContentTrend
            {
                ContentId = library.Id,
                Title = library.Name,
                Type = ContentType.Library,
                Date = library.CreatedAt,
                ViewCount = 0, // Would be calculated from actual view data
                InteractionCount = 0,
                TrendScore = 1.0,
                Direction = TrendDirection.Up
            });
        }

        return trends.OrderByDescending(t => t.TrendScore);
    }

    public async Task<IEnumerable<PopularContent>> GetPopularContentAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 20)
    {
        _logger.LogInformation("Getting popular content from {FromDate} to {ToDate}", fromDate, toDate);

        var popularContent = new List<PopularContent>();
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        // Get popular libraries
        var libraries = await _libraryRepository.GetAllAsync();
        foreach (var library in libraries.Where(l => l.CreatedAt >= from && l.CreatedAt <= to))
        {
            popularContent.Add(new PopularContent
            {
                Id = library.Id,
                Title = library.Name,
                Type = ContentType.Library,
                Path = library.Path,
                ViewCount = 0, // Would be calculated from actual view data
                InteractionCount = 0,
                AverageRating = 0.0,
                RatingCount = 0,
                PopularityScore = 1.0,
                CreatedAt = library.CreatedAt,
                LastViewed = library.UpdatedAt
            });
        }

        return popularContent
            .OrderByDescending(p => p.PopularityScore)
            .Take(limit);
    }

    public async Task<IEnumerable<ContentInsight>> GetContentInsightsAsync(ObjectId? userId = null)
    {
        _logger.LogInformation("Getting content insights for user {UserId}", userId);

        var insights = new List<ContentInsight>();

        // Generate basic insights
        insights.Add(new ContentInsight
        {
            Type = "ContentSummary",
            Title = "Content Overview",
            Description = "Summary of available content",
            Severity = InsightSeverity.Low,
            GeneratedAt = DateTime.UtcNow
        });

        return insights;
    }

    #endregion

    #region User Preferences & Behavior

    public async Task UpdateUserPreferencesAsync(ObjectId userId, UserDiscoveryPreferences preferences)
    {
        if (preferences == null)
            throw new ArgumentNullException(nameof(preferences));

        _logger.LogInformation("Updating user preferences for user {UserId}", userId);

        // In a real implementation, this would save to a user preferences repository
        // For now, we'll just log the update
        preferences.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<UserDiscoveryPreferences> GetUserPreferencesAsync(ObjectId userId)
    {
        _logger.LogInformation("Getting user preferences for user {UserId}", userId);

        // In a real implementation, this would retrieve from a user preferences repository
        // For now, return default preferences
        return new UserDiscoveryPreferences
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task RecordUserInteractionAsync(ObjectId userId, ObjectId contentId, InteractionType interactionType, double? rating = null)
    {
        _logger.LogInformation("Recording user interaction: User {UserId}, Content {ContentId}, Type {InteractionType}", userId, contentId, interactionType);

        // In a real implementation, this would save to a user interactions repository
        // For now, we'll just log the interaction
    }

    public async Task<IEnumerable<UserInteraction>> GetUserInteractionsAsync(ObjectId userId, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Getting user interactions for user {UserId}", userId);

        // In a real implementation, this would retrieve from a user interactions repository
        // For now, return empty list
        return new List<UserInteraction>();
    }

    #endregion

    #region Content Categorization

    public async Task<IEnumerable<ContentCategory>> GetContentCategoriesAsync()
    {
        _logger.LogInformation("Getting content categories");

        // In a real implementation, this would retrieve from a content categories repository
        // For now, return default categories
        return new List<ContentCategory>
        {
            new ContentCategory
            {
                Id = ObjectId.GenerateNewId(),
                Name = "General",
                Description = "General content category",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    public async Task<ContentCategory> CreateContentCategoryAsync(CreateContentCategoryRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Creating content category: {Name}", request.Name);

        var category = new ContentCategory
        {
            Id = ObjectId.GenerateNewId(),
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            Tags = request.Tags,
            SortOrder = request.SortOrder,
            Metadata = request.Metadata,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // In a real implementation, this would save to a content categories repository
        return category;
    }

    public async Task<ContentCategory> UpdateContentCategoryAsync(ObjectId categoryId, UpdateContentCategoryRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Updating content category {CategoryId}", categoryId);

        // In a real implementation, this would retrieve and update from a content categories repository
        // For now, return a mock updated category
        return new ContentCategory
        {
            Id = categoryId,
            Name = request.Name ?? "Updated Category",
            Description = request.Description ?? "Updated description",
            IsActive = request.IsActive ?? true,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task DeleteContentCategoryAsync(ObjectId categoryId)
    {
        _logger.LogInformation("Deleting content category {CategoryId}", categoryId);

        // In a real implementation, this would delete from a content categories repository
    }

    public async Task<IEnumerable<ContentRecommendation>> GetContentByCategoryAsync(string category, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Getting content by category {Category}", category);

        var request = new DiscoveryRequest
        {
            Categories = new List<string> { category },
            Limit = pageSize
        };

        // For now, return empty recommendations
        return new List<ContentRecommendation>();
    }

    #endregion

    #region Smart Suggestions

    public async Task<IEnumerable<SmartSuggestion>> GetSmartSuggestionsAsync(ObjectId userId, string context = "")
    {
        _logger.LogInformation("Getting smart suggestions for user {UserId} with context {Context}", userId, context);

        var suggestions = new List<SmartSuggestion>
        {
            new SmartSuggestion
            {
                Text = "Explore trending content",
                Type = SuggestionType.Query,
                Confidence = 0.8,
                Category = "Discovery",
                GeneratedAt = DateTime.UtcNow
            }
        };

        return suggestions;
    }

    public async Task<IEnumerable<SmartSuggestion>> GetContextualSuggestionsAsync(ObjectId userId, ObjectId currentContentId, int limit = 5)
    {
        _logger.LogInformation("Getting contextual suggestions for user {UserId} and content {ContentId}", userId, currentContentId);

        var suggestions = new List<SmartSuggestion>
        {
            new SmartSuggestion
            {
                Text = "View similar content",
                Type = SuggestionType.Query,
                Confidence = 0.7,
                RelatedContentId = currentContentId,
                GeneratedAt = DateTime.UtcNow
            }
        };

        return suggestions.Take(limit);
    }

    public async Task<IEnumerable<SmartSuggestion>> GetTrendingSuggestionsAsync(int limit = 10)
    {
        _logger.LogInformation("Getting trending suggestions");

        var suggestions = new List<SmartSuggestion>
        {
            new SmartSuggestion
            {
                Text = "Check out trending libraries",
                Type = SuggestionType.Query,
                Confidence = 0.9,
                Category = "Trending",
                GeneratedAt = DateTime.UtcNow
            }
        };

        return suggestions.Take(limit);
    }

    #endregion

    #region Helper Methods

    private bool ShouldIncludeContent(object content, DiscoveryRequest request, UserDiscoveryPreferences preferences)
    {
        // Basic filtering logic
        if (content is Library library)
        {
            if (!request.IncludeInactive && !library.IsActive)
                return false;

            if (request.CreatedAfter.HasValue && library.CreatedAt < request.CreatedAfter.Value)
                return false;

            if (request.CreatedBefore.HasValue && library.CreatedAt > request.CreatedBefore.Value)
                return false;
        }
        else if (content is Collection collection)
        {
            if (!request.IncludeInactive && !collection.IsActive)
                return false;

            if (request.CreatedAfter.HasValue && collection.CreatedAt < request.CreatedAfter.Value)
                return false;

            if (request.CreatedBefore.HasValue && collection.CreatedAt > request.CreatedBefore.Value)
                return false;
        }
        else if (content is MediaItem mediaItem)
        {
            if (!request.IncludeInactive && !mediaItem.IsActive)
                return false;

            if (request.CreatedAfter.HasValue && mediaItem.CreatedAt < request.CreatedAfter.Value)
                return false;

            if (request.CreatedBefore.HasValue && mediaItem.CreatedAt > request.CreatedBefore.Value)
                return false;
        }

        return true;
    }

    private ContentRecommendation CreateContentRecommendation(object content, ContentType type, RecommendationReason reason)
    {
        var recommendation = new ContentRecommendation
        {
            Type = type,
            RelevanceScore = 0.8,
            ConfidenceScore = 0.7,
            Reason = reason,
            ReasonDescription = GetReasonDescription(reason),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            AverageRating = 0.0,
            RatingCount = 0
        };

        if (content is Library library)
        {
            recommendation.Id = library.Id;
            recommendation.Title = library.Name;
            recommendation.Description = library.Description ?? "";
            recommendation.Path = library.Path;
        }
        else if (content is Collection collection)
        {
            recommendation.Id = collection.Id;
            recommendation.Title = collection.Name;
            recommendation.Description = "";
            recommendation.Path = collection.Path;
        }
        else if (content is MediaItem mediaItem)
        {
            recommendation.Id = mediaItem.Id;
            recommendation.Title = mediaItem.Name;
            recommendation.Description = "";
            recommendation.Path = mediaItem.Path;
        }

        return recommendation;
    }

    private string GetReasonDescription(RecommendationReason reason)
    {
        return reason switch
        {
            RecommendationReason.Popular => "Popular content",
            RecommendationReason.Trending => "Trending content",
            RecommendationReason.Similar => "Similar content",
            RecommendationReason.Personalized => "Personalized recommendation",
            RecommendationReason.Category => "Category-based recommendation",
            RecommendationReason.Tag => "Tag-based recommendation",
            RecommendationReason.History => "Based on your history",
            RecommendationReason.Random => "Random recommendation",
            _ => "Content recommendation"
        };
    }

    private IEnumerable<ContentRecommendation> SortAndLimitRecommendations(List<ContentRecommendation> recommendations, DiscoveryRequest request)
    {
        var sorted = request.SortBy switch
        {
            DiscoverySortBy.Relevance => recommendations.OrderByDescending(r => r.RelevanceScore),
            DiscoverySortBy.Date => recommendations.OrderByDescending(r => r.CreatedAt),
            DiscoverySortBy.Name => recommendations.OrderBy(r => r.Title),
            DiscoverySortBy.Views => recommendations.OrderByDescending(r => r.ViewCount),
            DiscoverySortBy.Rating => recommendations.OrderByDescending(r => r.AverageRating),
            DiscoverySortBy.Popularity => recommendations.OrderByDescending(r => r.ViewCount),
            DiscoverySortBy.Trending => recommendations.OrderByDescending(r => r.CreatedAt),
            _ => recommendations.OrderByDescending(r => r.RelevanceScore)
        };

        if (request.SortOrder == DiscoverySortOrder.Ascending)
        {
            return sorted.Reverse().Take(request.Limit);
        }

        return sorted.Take(request.Limit);
    }

    #endregion
}
