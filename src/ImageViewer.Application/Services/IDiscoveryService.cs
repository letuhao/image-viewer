using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for content discovery and recommendation operations
/// </summary>
public interface IDiscoveryService
{
    #region Content Discovery
    
    Task<IEnumerable<ContentRecommendation>> DiscoverContentAsync(ObjectId userId, DiscoveryRequest request);
    Task<IEnumerable<ContentRecommendation>> GetTrendingContentAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 20);
    Task<IEnumerable<ContentRecommendation>> GetPopularContentAsync(TimeSpan period, int limit = 20);
    Task<IEnumerable<ContentRecommendation>> GetSimilarContentAsync(ObjectId contentId, ContentType contentType, int limit = 10);
    
    #endregion
    
    #region Personalized Recommendations
    
    Task<IEnumerable<ContentRecommendation>> GetPersonalizedRecommendationsAsync(ObjectId userId, int limit = 10);
    Task<IEnumerable<ContentRecommendation>> GetRecommendationsByCategoryAsync(ObjectId userId, string category, int limit = 10);
    Task<IEnumerable<ContentRecommendation>> GetRecommendationsByTagsAsync(ObjectId userId, List<string> tags, int limit = 10);
    Task<IEnumerable<ContentRecommendation>> GetRecommendationsByHistoryAsync(ObjectId userId, int limit = 10);
    
    #endregion
    
    #region Content Analytics
    
    Task<ContentAnalytics> GetContentAnalyticsAsync(ObjectId? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<ContentTrend>> GetContentTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<PopularContent>> GetPopularContentAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 20);
    Task<IEnumerable<ContentInsight>> GetContentInsightsAsync(ObjectId? userId = null);
    
    #endregion
    
    #region User Preferences & Behavior
    
    Task UpdateUserPreferencesAsync(ObjectId userId, UserDiscoveryPreferences preferences);
    Task<UserDiscoveryPreferences> GetUserPreferencesAsync(ObjectId userId);
    Task RecordUserInteractionAsync(ObjectId userId, ObjectId contentId, InteractionType interactionType, double? rating = null);
    Task<IEnumerable<UserInteraction>> GetUserInteractionsAsync(ObjectId userId, int page = 1, int pageSize = 20);
    
    #endregion
    
    #region Content Categorization
    
    Task<IEnumerable<ContentCategory>> GetContentCategoriesAsync();
    Task<ContentCategory> CreateContentCategoryAsync(CreateContentCategoryRequest request);
    Task<ContentCategory> UpdateContentCategoryAsync(ObjectId categoryId, UpdateContentCategoryRequest request);
    Task DeleteContentCategoryAsync(ObjectId categoryId);
    Task<IEnumerable<ContentRecommendation>> GetContentByCategoryAsync(string category, int page = 1, int pageSize = 20);
    
    #endregion
    
    #region Smart Suggestions
    
    Task<IEnumerable<SmartSuggestion>> GetSmartSuggestionsAsync(ObjectId userId, string context = "");
    Task<IEnumerable<SmartSuggestion>> GetContextualSuggestionsAsync(ObjectId userId, ObjectId currentContentId, int limit = 5);
    Task<IEnumerable<SmartSuggestion>> GetTrendingSuggestionsAsync(int limit = 10);
    
    #endregion
}

/// <summary>
/// Discovery request model
/// </summary>
public class DiscoveryRequest
{
    public List<string>? Categories { get; set; }
    public List<string>? Tags { get; set; }
    public ObjectId? LibraryId { get; set; }
    public ObjectId? CollectionId { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int Limit { get; set; } = 20;
    public DiscoverySortBy SortBy { get; set; } = DiscoverySortBy.Relevance;
    public DiscoverySortOrder SortOrder { get; set; } = DiscoverySortOrder.Descending;
    public bool IncludeInactive { get; set; } = false;
    public double? MinRating { get; set; }
    public int? MinViews { get; set; }
}

/// <summary>
/// Content recommendation model
/// </summary>
public class ContentRecommendation
{
    public ObjectId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public double ConfidenceScore { get; set; }
    public RecommendationReason Reason { get; set; }
    public string? ReasonDescription { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ViewCount { get; set; }
    public double? AverageRating { get; set; }
    public long RatingCount { get; set; }
}

/// <summary>
/// Content analytics model
/// </summary>
public class ContentAnalytics
{
    public ObjectId? UserId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public long TotalContent { get; set; }
    public long TotalViews { get; set; }
    public long TotalInteractions { get; set; }
    public double AverageRating { get; set; }
    public List<PopularContent> TopContent { get; set; } = new();
    public List<ContentTrend> Trends { get; set; } = new();
    public Dictionary<string, long> ContentByCategory { get; set; } = new();
    public Dictionary<string, long> ContentByType { get; set; } = new();
    public double EngagementRate { get; set; }
    public double DiscoveryRate { get; set; }
}

/// <summary>
/// Content trend model
/// </summary>
public class ContentTrend
{
    public ObjectId ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public DateTime Date { get; set; }
    public long ViewCount { get; set; }
    public long InteractionCount { get; set; }
    public double TrendScore { get; set; }
    public TrendDirection Direction { get; set; }
}

/// <summary>
/// Popular content model
/// </summary>
public class PopularContent
{
    public ObjectId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public long ViewCount { get; set; }
    public long InteractionCount { get; set; }
    public double AverageRating { get; set; }
    public long RatingCount { get; set; }
    public double PopularityScore { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastViewed { get; set; }
}

/// <summary>
/// Content insight model
/// </summary>
public class ContentInsight
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public InsightSeverity Severity { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public ObjectId? RelatedContentId { get; set; }
}

/// <summary>
/// User discovery preferences model
/// </summary>
public class UserDiscoveryPreferences
{
    public ObjectId UserId { get; set; }
    public List<string> PreferredCategories { get; set; } = new();
    public List<string> PreferredTags { get; set; } = new();
    public List<string> ExcludedCategories { get; set; } = new();
    public List<string> ExcludedTags { get; set; } = new();
    public DiscoverySortBy DefaultSortBy { get; set; } = DiscoverySortBy.Relevance;
    public DiscoverySortOrder DefaultSortOrder { get; set; } = DiscoverySortOrder.Descending;
    public int DefaultPageSize { get; set; } = 20;
    public bool EnablePersonalizedRecommendations { get; set; } = true;
    public bool EnableTrendingContent { get; set; } = true;
    public bool EnableSimilarContent { get; set; } = true;
    public double MinRecommendationScore { get; set; } = 0.5;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// User interaction model
/// </summary>
public class UserInteraction
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public ObjectId ContentId { get; set; }
    public InteractionType Type { get; set; }
    public double? Rating { get; set; }
    public string? Comment { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Content category model
/// </summary>
public class ContentCategory
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ParentCategoryId { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create content category request model
/// </summary>
public class CreateContentCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ParentCategoryId { get; set; }
    public List<string> Tags { get; set; } = new();
    public int SortOrder { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Update content category request model
/// </summary>
public class UpdateContentCategoryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ParentCategoryId { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Smart suggestion model
/// </summary>
public class SmartSuggestion
{
    public string Text { get; set; } = string.Empty;
    public SuggestionType Type { get; set; }
    public double Confidence { get; set; }
    public string? Category { get; set; }
    public ObjectId? RelatedContentId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Enums
/// </summary>
public enum DiscoverySortBy
{
    Relevance,
    Date,
    Name,
    Views,
    Rating,
    Popularity,
    Trending
}

public enum DiscoverySortOrder
{
    Ascending,
    Descending
}

public enum RecommendationReason
{
    Popular,
    Trending,
    Similar,
    Personalized,
    Category,
    Tag,
    History,
    Random
}

public enum TrendDirection
{
    Up,
    Down,
    Stable
}

public enum InsightSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum InteractionType
{
    View,
    Like,
    Dislike,
    Share,
    Download,
    Bookmark,
    Rate,
    Comment,
    Search
}

