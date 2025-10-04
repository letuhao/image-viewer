using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for advanced search and discovery operations
/// </summary>
public class SearchService : ISearchService
{
    private readonly IUserRepository _userRepository;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IUserRepository userRepository,
        ILibraryRepository libraryRepository,
        ICollectionRepository collectionRepository,
        IMediaItemRepository mediaItemRepository,
        ILogger<SearchService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _mediaItemRepository = mediaItemRepository ?? throw new ArgumentNullException(nameof(mediaItemRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SearchResult> SearchAsync(SearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                throw new ValidationException("Search query cannot be null or empty");

            var results = new List<SearchResultItem>();

            switch (request.Type)
            {
                case SearchType.All:
                    results.AddRange(await SearchLibrariesAsync(request).ContinueWith(t => t.Result.Items));
                    results.AddRange(await SearchCollectionsAsync(request).ContinueWith(t => t.Result.Items));
                    results.AddRange(await SearchMediaItemsAsync(request).ContinueWith(t => t.Result.Items));
                    break;
                case SearchType.Libraries:
                    results.AddRange((await SearchLibrariesAsync(request)).Items);
                    break;
                case SearchType.Collections:
                    results.AddRange((await SearchCollectionsAsync(request)).Items);
                    break;
                case SearchType.MediaItems:
                    results.AddRange((await SearchMediaItemsAsync(request)).Items);
                    break;
                default:
                    throw new ValidationException($"Unsupported search type: {request.Type}");
            }

            // Sort results by relevance score
            results = results.OrderByDescending(r => r.RelevanceScore).ToList();

            // Apply pagination
            var totalCount = results.Count;
            var skip = (request.Page - 1) * request.PageSize;
            var pagedResults = results.Skip(skip).Take(request.PageSize).ToList();

            stopwatch.Stop();

            return new SearchResult
            {
                Items = pagedResults,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (long)Math.Ceiling((double)totalCount / request.PageSize),
                SearchTime = stopwatch.Elapsed,
                Metadata = new SearchMetadata
                {
                    Query = request.Query,
                    Type = request.Type,
                    TotalResults = totalCount,
                    SearchTime = stopwatch.Elapsed
                }
            };
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to perform search with query {Query}", request.Query);
            throw new BusinessRuleException($"Failed to perform search with query '{request.Query}'", ex);
        }
    }

    public async Task<SearchResult> SearchLibrariesAsync(SearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var libraries = await _libraryRepository.SearchLibrariesAsync(request.Query);
            var results = libraries.Select(l => new SearchResultItem
            {
                Id = l.Id,
                Title = l.Name,
                Description = l.Description,
                Type = "Library",
                Path = l.Path,
                RelevanceScore = CalculateRelevanceScore(request.Query, l.Name, l.Description),
                Tags = l.Metadata.Tags,
                Categories = l.Metadata.Categories,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                Metadata = new Dictionary<string, object>
                {
                    ["OwnerId"] = l.OwnerId,
                    ["IsPublic"] = l.IsPublic,
                    ["IsActive"] = l.IsActive,
                    ["CollectionCount"] = l.Statistics.TotalCollections,
                    ["TotalSize"] = l.Statistics.TotalSize
                }
            }).ToList();

            stopwatch.Stop();

            return new SearchResult
            {
                Items = results,
                TotalCount = results.Count,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (long)Math.Ceiling((double)results.Count / request.PageSize),
                SearchTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search libraries with query {Query}", request.Query);
            throw new BusinessRuleException($"Failed to search libraries with query '{request.Query}'", ex);
        }
    }

    public async Task<SearchResult> SearchCollectionsAsync(SearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var collections = await _collectionRepository.SearchCollectionsAsync(request.Query);
            var results = collections.Select(c => new SearchResultItem
            {
                Id = c.Id,
                Title = c.Name,
                Description = c.Metadata.Description,
                Type = "Collection",
                Path = c.Path,
                RelevanceScore = CalculateRelevanceScore(request.Query, c.Name, c.Metadata.Description),
                Tags = c.Metadata.Tags,
                Categories = c.Metadata.Categories,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Metadata = new Dictionary<string, object>
                {
                    ["LibraryId"] = c.LibraryId,
                    ["Type"] = c.Type.ToString(),
                    ["IsActive"] = c.IsActive,
                    ["TotalItems"] = c.Statistics.TotalItems,
                    ["TotalSize"] = c.Statistics.TotalSize
                }
            }).ToList();

            stopwatch.Stop();

            return new SearchResult
            {
                Items = results,
                TotalCount = results.Count,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (long)Math.Ceiling((double)results.Count / request.PageSize),
                SearchTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search collections with query {Query}", request.Query);
            throw new BusinessRuleException($"Failed to search collections with query '{request.Query}'", ex);
        }
    }

    public async Task<SearchResult> SearchMediaItemsAsync(SearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var mediaItems = await _mediaItemRepository.SearchMediaItemsAsync(request.Query);
            var results = mediaItems.Select(m => new SearchResultItem
            {
                Id = m.Id,
                Title = m.Name,
                Description = m.Metadata.Description,
                Type = m.Type,
                Path = m.Path,
                RelevanceScore = CalculateRelevanceScore(request.Query, m.Name, m.Metadata.Description),
                Tags = m.Metadata.Tags,
                Categories = m.Metadata.Categories,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                Metadata = new Dictionary<string, object>
                {
                    ["CollectionId"] = m.CollectionId,
                    ["Format"] = m.Format,
                    ["FileSize"] = m.FileSize,
                    ["Width"] = m.Width,
                    ["Height"] = m.Height,
                    ["Duration"] = m.Duration?.TotalSeconds
                }
            }).ToList();

            stopwatch.Stop();

            return new SearchResult
            {
                Items = results,
                TotalCount = results.Count,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (long)Math.Ceiling((double)results.Count / request.PageSize),
                SearchTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search media items with query {Query}", request.Query);
            throw new BusinessRuleException($"Failed to search media items with query '{request.Query}'", ex);
        }
    }

    public async Task<SearchResult> SemanticSearchAsync(SemanticSearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // For now, implement basic semantic search using text similarity
            // In a real implementation, this would use AI/ML models for semantic understanding
            var expandedQuery = ExpandQueryWithSemantics(request.Query, request.RelatedTerms);
            
            var searchRequest = new SearchRequest
            {
                Query = expandedQuery,
                Type = request.Type,
                Tags = request.Tags,
                Categories = request.Categories,
                LibraryId = request.LibraryId,
                CollectionId = request.CollectionId,
                UserId = request.UserId,
                CreatedAfter = request.CreatedAfter,
                CreatedBefore = request.CreatedBefore,
                Page = request.Page,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder,
                IncludeInactive = request.IncludeInactive
            };

            var result = await SearchAsync(searchRequest);
            
            // Filter by similarity threshold
            result.Items = result.Items.Where(i => i.RelevanceScore >= request.SimilarityThreshold).ToList();
            result.TotalCount = result.Items.Count;

            stopwatch.Stop();
            result.SearchTime = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search with query {Query}", request.Query);
            throw new BusinessRuleException($"Failed to perform semantic search with query '{request.Query}'", ex);
        }
    }

    public Task<SearchResult> VisualSearchAsync(VisualSearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // For now, implement basic visual search using metadata comparison
            // In a real implementation, this would use computer vision and AI models
            var results = new List<SearchResultItem>();

            // This is a placeholder implementation
            // Real visual search would:
            // 1. Extract features from the input image
            // 2. Compare with features of stored images
            // 3. Return similar images based on visual similarity

            stopwatch.Stop();

            var result = new SearchResult
            {
                Items = results,
                TotalCount = results.Count,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0,
                SearchTime = stopwatch.Elapsed,
                Metadata = new SearchMetadata
                {
                    Query = "Visual Search",
                    Type = SearchType.MediaItems,
                    TotalResults = results.Count,
                    SearchTime = stopwatch.Elapsed
                }
            };
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform visual search");
            throw new BusinessRuleException("Failed to perform visual search", ex);
        }
    }

    public async Task<SearchResult> SimilarContentSearchAsync(SimilarContentRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var results = new List<SearchResultItem>();

            // Get the source content
            switch (request.ContentType)
            {
                case ContentType.Library:
                    var library = await _libraryRepository.GetByIdAsync(request.ContentId);
                    if (library != null)
                    {
                        // Find similar libraries based on tags, categories, and metadata
                        var similarLibraries = await FindSimilarLibraries(library, request.SimilarityThreshold, request.Limit);
                        results.AddRange(similarLibraries.Select(l => new SearchResultItem
                        {
                            Id = l.Id,
                            Title = l.Name,
                            Description = l.Description,
                            Type = "Library",
                            Path = l.Path,
                            RelevanceScore = CalculateSimilarityScore(library, l),
                            Tags = l.Metadata.Tags,
                            Categories = l.Metadata.Categories,
                            CreatedAt = l.CreatedAt,
                            UpdatedAt = l.UpdatedAt
                        }));
                    }
                    break;

                case ContentType.Collection:
                    var collection = await _collectionRepository.GetByIdAsync(request.ContentId);
                    if (collection != null)
                    {
                        // Find similar collections
                        var similarCollections = await FindSimilarCollections(collection, request.SimilarityThreshold, request.Limit);
                        results.AddRange(similarCollections.Select(c => new SearchResultItem
                        {
                            Id = c.Id,
                            Title = c.Name,
                            Description = c.Metadata.Description,
                            Type = "Collection",
                            Path = c.Path,
                            RelevanceScore = CalculateSimilarityScore(collection, c),
                            Tags = c.Metadata.Tags,
                            Categories = c.Metadata.Categories,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt
                        }));
                    }
                    break;

                case ContentType.MediaItem:
                    var mediaItem = await _mediaItemRepository.GetByIdAsync(request.ContentId);
                    if (mediaItem != null)
                    {
                        // Find similar media items
                        var similarMediaItems = await FindSimilarMediaItems(mediaItem, request.SimilarityThreshold, request.Limit);
                        results.AddRange(similarMediaItems.Select(m => new SearchResultItem
                        {
                            Id = m.Id,
                            Title = m.Name,
                            Description = m.Metadata.Description,
                            Type = m.Type,
                            Path = m.Path,
                            RelevanceScore = CalculateSimilarityScore(mediaItem, m),
                            Tags = m.Metadata.Tags,
                            Categories = m.Metadata.Categories,
                            CreatedAt = m.CreatedAt,
                            UpdatedAt = m.UpdatedAt
                        }));
                    }
                    break;
            }

            stopwatch.Stop();

            return new SearchResult
            {
                Items = results,
                TotalCount = results.Count,
                Page = 1,
                PageSize = request.Limit,
                TotalPages = 1,
                SearchTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar content for {ContentType} with ID {ContentId}", request.ContentType, request.ContentId);
            throw new BusinessRuleException($"Failed to find similar content for {request.ContentType} with ID '{request.ContentId}'", ex);
        }
    }

    public async Task<SearchResult> AdvancedFilterSearchAsync(AdvancedFilterRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Apply advanced filters
            var results = new List<SearchResultItem>();

            // This is a simplified implementation
            // Real implementation would use MongoDB aggregation pipelines for complex filtering

            var baseResults = await SearchAsync(request);
            results.AddRange(baseResults.Items);

            // Apply additional filters
            foreach (var filter in request.Filters)
            {
                results = ApplyFilter(results, filter);
            }

            stopwatch.Stop();

            return new SearchResult
            {
                Items = results,
                TotalCount = results.Count,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (long)Math.Ceiling((double)results.Count / request.PageSize),
                SearchTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform advanced filter search");
            throw new BusinessRuleException("Failed to perform advanced filter search", ex);
        }
    }

    public Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult<IEnumerable<string>>(new List<string>());

            // This is a simplified implementation
            // Real implementation would use search analytics and popular queries
            var suggestions = new List<string>
            {
                query + " images",
                query + " videos",
                query + " collections",
                query + " libraries"
            };

            return suggestions.Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get search suggestions for query {Query}", query);
            throw new BusinessRuleException($"Failed to get search suggestions for query '{query}'", ex);
        }
    }

    public Task<IEnumerable<string>> GetAutoCompleteAsync(string partialQuery, int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(partialQuery))
                return Task.FromResult<IEnumerable<string>>(new List<string>());

            // This is a simplified implementation
            // Real implementation would use indexed search terms
            var completions = new List<string>
            {
                partialQuery + "a",
                partialQuery + "b",
                partialQuery + "c"
            };

            return completions.Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto-complete for partial query {PartialQuery}", partialQuery);
            throw new BusinessRuleException($"Failed to get auto-complete for partial query '{partialQuery}'", ex);
        }
    }

    public Task<IEnumerable<SearchSuggestion>> GetSmartSuggestionsAsync(string query, ObjectId? userId = null)
    {
        try
        {
            var suggestions = new List<SearchSuggestion>();

            // This is a simplified implementation
            // Real implementation would use AI/ML for smart suggestions
            suggestions.Add(new SearchSuggestion
            {
                Text = query + " - Popular",
                Type = SuggestionType.Query,
                Confidence = 0.8,
                Category = "Popular"
            });

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get smart suggestions for query {Query}", query);
            throw new BusinessRuleException($"Failed to get smart suggestions for query '{query}'", ex);
        }
    }

    public Task<SearchAnalytics> GetSearchAnalyticsAsync(ObjectId? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            // This is a simplified implementation
            // Real implementation would query search history and analytics collections
            return new SearchAnalytics
            {
                UserId = userId,
                FromDate = fromDate ?? DateTime.UtcNow.AddDays(-30),
                ToDate = toDate ?? DateTime.UtcNow,
                TotalSearches = 0,
                UniqueQueries = 0,
                PopularQueries = new List<PopularQuery>(),
                Trends = new List<SearchTrend>(),
                SearchByType = new Dictionary<string, long>(),
                AverageSearchTime = 0,
                ClickThroughRate = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get search analytics");
            throw new BusinessRuleException("Failed to get search analytics", ex);
        }
    }

    public Task<IEnumerable<SearchRecommendation>> GetPersonalizedRecommendationsAsync(ObjectId userId, int limit = 10)
    {
        try
        {
            // This is a simplified implementation
            // Real implementation would use user behavior and preferences
            var recommendations = new List<SearchRecommendation>();

            recommendations.Add(new SearchRecommendation
            {
                Query = "Popular this week",
                Type = RecommendationType.Popular,
                Confidence = 0.9,
                Reason = "Based on trending searches"
            });

            return recommendations.Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get personalized recommendations for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get personalized recommendations for user '{userId}'", ex);
        }
    }

    public Task<IEnumerable<SearchTrend>> GetSearchTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            // This is a simplified implementation
            // Real implementation would analyze search patterns over time
            return Task.FromResult<IEnumerable<SearchTrend>>(new List<SearchTrend>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get search trends");
            throw new BusinessRuleException("Failed to get search trends", ex);
        }
    }

    public Task<IEnumerable<SearchHistory>> GetSearchHistoryAsync(ObjectId userId, int page = 1, int pageSize = 20)
    {
        try
        {
            // This is a simplified implementation
            // Real implementation would query search history collection
            return Task.FromResult<IEnumerable<SearchHistory>>(new List<SearchHistory>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get search history for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get search history for user '{userId}'", ex);
        }
    }

    public Task SaveSearchHistoryAsync(ObjectId userId, string query, SearchResult result)
    {
        try
        {
            // This is a simplified implementation
            // Real implementation would save to search history collection
            _logger.LogInformation("Saving search history for user {UserId} with query {Query}", userId, query);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save search history for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to save search history for user '{userId}'", ex);
        }
    }

    public Task ClearSearchHistoryAsync(ObjectId userId)
    {
        try
        {
            // This is a simplified implementation
            // Real implementation would clear search history collection
            _logger.LogInformation("Clearing search history for user {UserId}", userId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear search history for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to clear search history for user '{userId}'", ex);
        }
    }

    public Task DeleteSearchHistoryItemAsync(ObjectId userId, ObjectId historyId)
    {
        try
        {
            // This is a simplified implementation
            // Real implementation would delete from search history collection
            _logger.LogInformation("Deleting search history item {HistoryId} for user {UserId}", historyId, userId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete search history item {HistoryId} for user {UserId}", historyId, userId);
            throw new BusinessRuleException($"Failed to delete search history item '{historyId}' for user '{userId}'", ex);
        }
    }

    #region Private Helper Methods

    private double CalculateRelevanceScore(string query, string title, string description)
    {
        // Simple relevance scoring based on text matching
        var queryLower = query.ToLower();
        var titleLower = title.ToLower();
        var descriptionLower = description.ToLower();

        double score = 0;

        // Exact match in title gets highest score
        if (titleLower.Contains(queryLower))
            score += 1.0;

        // Partial match in title
        var titleWords = titleLower.Split(' ');
        var queryWords = queryLower.Split(' ');
        var titleMatches = titleWords.Count(w => queryWords.Any(qw => w.Contains(qw)));
        score += titleMatches * 0.5 / titleWords.Length;

        // Match in description
        if (descriptionLower.Contains(queryLower))
            score += 0.3;

        return Math.Min(score, 1.0);
    }

    private string ExpandQueryWithSemantics(string query, List<string>? relatedTerms)
    {
        var expandedQuery = query;
        
        if (relatedTerms != null && relatedTerms.Any())
        {
            expandedQuery += " " + string.Join(" ", relatedTerms);
        }

        return expandedQuery;
    }

    private List<SearchResultItem> ApplyFilter(List<SearchResultItem> results, FilterCriteria filter)
    {
        // Simplified filter implementation
        // Real implementation would use proper type checking and comparison
        return results.Where(r =>
        {
            if (r.Metadata.TryGetValue(filter.Field, out var value))
            {
                return CompareValues(value, filter.Operator, filter.Value);
            }
            return false;
        }).ToList();
    }

    private bool CompareValues(object value, FilterOperator op, object filterValue)
    {
        // Simplified comparison logic
        // Real implementation would handle different data types properly
        return value?.ToString()?.Contains(filterValue.ToString() ?? "") == true;
    }

    private async Task<IEnumerable<Library>> FindSimilarLibraries(Library source, double threshold, int limit)
    {
        // Simplified similarity search
        // Real implementation would use proper similarity algorithms
        var allLibraries = await _libraryRepository.FindAsync(
            Builders<Library>.Filter.Empty,
            Builders<Library>.Sort.Descending(l => l.CreatedAt),
            limit,
            0
        );

        return allLibraries.Where(l => l.Id != source.Id).Take(limit);
    }

    private async Task<IEnumerable<Collection>> FindSimilarCollections(Collection source, double threshold, int limit)
    {
        // Simplified similarity search
        var allCollections = await _collectionRepository.FindAsync(
            Builders<Collection>.Filter.Empty,
            Builders<Collection>.Sort.Descending(c => c.CreatedAt),
            limit,
            0
        );

        return allCollections.Where(c => c.Id != source.Id).Take(limit);
    }

    private async Task<IEnumerable<MediaItem>> FindSimilarMediaItems(MediaItem source, double threshold, int limit)
    {
        // Simplified similarity search
        var allMediaItems = await _mediaItemRepository.FindAsync(
            Builders<MediaItem>.Filter.Empty,
            Builders<MediaItem>.Sort.Descending(m => m.CreatedAt),
            limit,
            0
        );

        return allMediaItems.Where(m => m.Id != source.Id).Take(limit);
    }

    private double CalculateSimilarityScore<T>(T source, T target) where T : class
    {
        // Simplified similarity scoring
        // Real implementation would use proper similarity algorithms
        return 0.8; // Placeholder score
    }

    #endregion
}
