using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for advanced search and discovery operations
/// </summary>
public interface ISearchService
{
    #region Basic Search Operations
    
    Task<SearchResult> SearchAsync(SearchRequest request);
    Task<SearchResult> SearchLibrariesAsync(SearchRequest request);
    Task<SearchResult> SearchCollectionsAsync(SearchRequest request);
    Task<SearchResult> SearchMediaItemsAsync(SearchRequest request);
    
    #endregion
    
    #region Advanced Search Operations
    
    Task<SearchResult> SemanticSearchAsync(SemanticSearchRequest request);
    Task<SearchResult> VisualSearchAsync(VisualSearchRequest request);
    Task<SearchResult> SimilarContentSearchAsync(SimilarContentRequest request);
    Task<SearchResult> AdvancedFilterSearchAsync(AdvancedFilterRequest request);
    
    #endregion
    
    #region Search Suggestions & Auto-complete
    
    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int limit = 10);
    Task<IEnumerable<string>> GetAutoCompleteAsync(string partialQuery, int limit = 10);
    Task<IEnumerable<SearchSuggestion>> GetSmartSuggestionsAsync(string query, ObjectId? userId = null);
    
    #endregion
    
    #region Search Analytics & Personalization
    
    Task<SearchAnalytics> GetSearchAnalyticsAsync(ObjectId? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<SearchRecommendation>> GetPersonalizedRecommendationsAsync(ObjectId userId, int limit = 10);
    Task<IEnumerable<SearchTrend>> GetSearchTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    
    #endregion
    
    #region Search History & Management
    
    Task<IEnumerable<SearchHistory>> GetSearchHistoryAsync(ObjectId userId, int page = 1, int pageSize = 20);
    Task SaveSearchHistoryAsync(ObjectId userId, string query, SearchResult result);
    Task ClearSearchHistoryAsync(ObjectId userId);
    Task DeleteSearchHistoryItemAsync(ObjectId userId, ObjectId historyId);
    
    #endregion
}

/// <summary>
/// Search request model
/// </summary>
public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public SearchType Type { get; set; } = SearchType.All;
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }
    public ObjectId? LibraryId { get; set; }
    public ObjectId? CollectionId { get; set; }
    public ObjectId? UserId { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public SearchSortBy SortBy { get; set; } = SearchSortBy.Relevance;
    public SearchSortOrder SortOrder { get; set; } = SearchSortOrder.Descending;
    public bool IncludeInactive { get; set; } = false;
}

/// <summary>
/// Semantic search request model
/// </summary>
public class SemanticSearchRequest : SearchRequest
{
    public string? Context { get; set; }
    public List<string>? RelatedTerms { get; set; }
    public double SimilarityThreshold { get; set; } = 0.7;
    public bool UseAI { get; set; } = true;
}

/// <summary>
/// Visual search request model
/// </summary>
public class VisualSearchRequest
{
    public byte[]? ImageData { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImagePath { get; set; }
    public VisualSearchType Type { get; set; } = VisualSearchType.SimilarImages;
    public double SimilarityThreshold { get; set; } = 0.8;
    public List<string>? ColorFilters { get; set; }
    public List<string>? StyleFilters { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Similar content request model
/// </summary>
public class SimilarContentRequest
{
    public ObjectId ContentId { get; set; }
    public ContentType ContentType { get; set; }
    public double SimilarityThreshold { get; set; } = 0.7;
    public int Limit { get; set; } = 10;
    public bool IncludeMetadata { get; set; } = true;
}

/// <summary>
/// Advanced filter request model
/// </summary>
public class AdvancedFilterRequest : SearchRequest
{
    public List<FilterCriteria> Filters { get; set; } = new();
    public List<AggregationCriteria> Aggregations { get; set; } = new();
    public bool UseFacetedSearch { get; set; } = true;
    public bool IncludeFacets { get; set; } = true;
}

/// <summary>
/// Search result model
/// </summary>
public class SearchResult
{
    public List<SearchResultItem> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalPages { get; set; }
    public TimeSpan SearchTime { get; set; }
    public List<SearchFacet> Facets { get; set; } = new();
    public List<SearchSuggestion> Suggestions { get; set; } = new();
    public SearchMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Search result item
/// </summary>
public class SearchResultItem
{
    public ObjectId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Search facet
/// </summary>
public class SearchFacet
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<FacetValue> Values { get; set; } = new();
}

/// <summary>
/// Facet value
/// </summary>
public class FacetValue
{
    public string Value { get; set; } = string.Empty;
    public long Count { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// Search suggestion
/// </summary>
public class SearchSuggestion
{
    public string Text { get; set; } = string.Empty;
    public SuggestionType Type { get; set; }
    public double Confidence { get; set; }
    public string? Category { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Search metadata
/// </summary>
public class SearchMetadata
{
    public string Query { get; set; } = string.Empty;
    public SearchType Type { get; set; }
    public int TotalResults { get; set; }
    public TimeSpan SearchTime { get; set; }
    public List<string> AppliedFilters { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public Dictionary<string, object> Analytics { get; set; } = new();
}

/// <summary>
/// Search analytics
/// </summary>
public class SearchAnalytics
{
    public ObjectId? UserId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public long TotalSearches { get; set; }
    public long UniqueQueries { get; set; }
    public List<PopularQuery> PopularQueries { get; set; } = new();
    public List<SearchTrend> Trends { get; set; } = new();
    public Dictionary<string, long> SearchByType { get; set; } = new();
    public double AverageSearchTime { get; set; }
    public double ClickThroughRate { get; set; }
}

/// <summary>
/// Popular query
/// </summary>
public class PopularQuery
{
    public string Query { get; set; } = string.Empty;
    public long Count { get; set; }
    public double ClickThroughRate { get; set; }
    public DateTime LastSearched { get; set; }
}

/// <summary>
/// Search trend
/// </summary>
public class SearchTrend
{
    public string Query { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public long Count { get; set; }
    public double Trend { get; set; }
}

/// <summary>
/// Search recommendation
/// </summary>
public class SearchRecommendation
{
    public string Query { get; set; } = string.Empty;
    public RecommendationType Type { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Search history
/// </summary>
public class SearchHistory
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string Query { get; set; } = string.Empty;
    public SearchType Type { get; set; }
    public long ResultCount { get; set; }
    public DateTime SearchedAt { get; set; }
    public TimeSpan SearchTime { get; set; }
    public bool ClickedResult { get; set; }
    public ObjectId? ClickedResultId { get; set; }
}

/// <summary>
/// Filter criteria
/// </summary>
public class FilterCriteria
{
    public string Field { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public object Value { get; set; } = string.Empty;
    public List<object>? Values { get; set; }
}

/// <summary>
/// Aggregation criteria
/// </summary>
public class AggregationCriteria
{
    public string Field { get; set; } = string.Empty;
    public AggregationType Type { get; set; }
    public string? Alias { get; set; }
}

/// <summary>
/// Enums
/// </summary>
public enum SearchType
{
    All,
    Libraries,
    Collections,
    MediaItems,
    Users,
    Tags
}

public enum SearchSortBy
{
    Relevance,
    Date,
    Name,
    Size,
    Views,
    Downloads,
    Rating
}

public enum SearchSortOrder
{
    Ascending,
    Descending
}

public enum VisualSearchType
{
    SimilarImages,
    SimilarColors,
    SimilarStyles,
    ObjectDetection,
    FaceRecognition
}

public enum ContentType
{
    Library,
    Collection,
    MediaItem
}

public enum SuggestionType
{
    Query,
    Tag,
    Category,
    User,
    Collection
}

public enum RecommendationType
{
    Popular,
    Trending,
    Personalized,
    Similar,
    Related
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    In,
    NotIn,
    Between,
    IsNull,
    IsNotNull
}

public enum AggregationType
{
    Count,
    Sum,
    Average,
    Min,
    Max,
    GroupBy
}
