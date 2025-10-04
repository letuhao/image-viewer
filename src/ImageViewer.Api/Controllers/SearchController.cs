using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Exceptions;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for advanced search and discovery operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Perform a general search across all content types
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _searchService.SearchAsync(request);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform search");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Search libraries
    /// </summary>
    [HttpPost("libraries")]
    public async Task<IActionResult> SearchLibraries([FromBody] SearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _searchService.SearchLibrariesAsync(request);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search libraries");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Search collections
    /// </summary>
    [HttpPost("collections")]
    public async Task<IActionResult> SearchCollections([FromBody] SearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _searchService.SearchCollectionsAsync(request);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search collections");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Search media items
    /// </summary>
    [HttpPost("media-items")]
    public async Task<IActionResult> SearchMediaItems([FromBody] SearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _searchService.SearchMediaItemsAsync(request);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search media items");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Perform semantic search
    /// </summary>
    [HttpPost("semantic")]
    public async Task<IActionResult> SemanticSearch([FromBody] SemanticSearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _searchService.SemanticSearchAsync(request);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Perform visual search
    /// </summary>
    [HttpPost("visual")]
    public async Task<IActionResult> VisualSearch([FromBody] VisualSearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _searchService.VisualSearchAsync(request);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform visual search");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Find similar content
    /// </summary>
    [HttpPost("similar")]
    public async Task<IActionResult> FindSimilarContent([FromBody] SimilarContentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _searchService.SimilarContentSearchAsync(request);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar content");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Perform advanced filter search
    /// </summary>
    [HttpPost("advanced-filter")]
    public async Task<IActionResult> AdvancedFilterSearch([FromBody] AdvancedFilterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _searchService.AdvancedFilterSearchAsync(request);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform advanced filter search");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get search suggestions
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSearchSuggestions([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            var suggestions = await _searchService.GetSearchSuggestionsAsync(query, limit);
            return Ok(suggestions);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get search suggestions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get auto-complete suggestions
    /// </summary>
    [HttpGet("auto-complete")]
    public async Task<IActionResult> GetAutoComplete([FromQuery] string partialQuery, [FromQuery] int limit = 10)
    {
        try
        {
            var completions = await _searchService.GetAutoCompleteAsync(partialQuery, limit);
            return Ok(completions);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto-complete suggestions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get smart suggestions
    /// </summary>
    [HttpGet("smart-suggestions")]
    public async Task<IActionResult> GetSmartSuggestions([FromQuery] string query, [FromQuery] string? userId = null)
    {
        try
        {
            ObjectId? userObjectId = null;
            if (!string.IsNullOrEmpty(userId) && ObjectId.TryParse(userId, out var parsedUserId))
            {
                userObjectId = parsedUserId;
            }

            var suggestions = await _searchService.GetSmartSuggestionsAsync(query, userObjectId);
            return Ok(suggestions);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get smart suggestions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get search analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetSearchAnalytics([FromQuery] string? userId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            ObjectId? userObjectId = null;
            if (!string.IsNullOrEmpty(userId) && ObjectId.TryParse(userId, out var parsedUserId))
            {
                userObjectId = parsedUserId;
            }

            var analytics = await _searchService.GetSearchAnalyticsAsync(userObjectId, fromDate, toDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get search analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get personalized recommendations
    /// </summary>
    [HttpGet("recommendations/{userId}")]
    public async Task<IActionResult> GetPersonalizedRecommendations(string userId, [FromQuery] int limit = 10)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var recommendations = await _searchService.GetPersonalizedRecommendationsAsync(userObjectId, limit);
            return Ok(recommendations);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get personalized recommendations for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get search trends
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetSearchTrends([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var trends = await _searchService.GetSearchTrendsAsync(fromDate, toDate);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get search trends");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get search history for a user
    /// </summary>
    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetSearchHistory(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var history = await _searchService.GetSearchHistoryAsync(userObjectId, page, pageSize);
            return Ok(history);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get search history for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Clear search history for a user
    /// </summary>
    [HttpDelete("history/{userId}")]
    public async Task<IActionResult> ClearSearchHistory(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            await _searchService.ClearSearchHistoryAsync(userObjectId);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear search history for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a specific search history item
    /// </summary>
    [HttpDelete("history/{userId}/{historyId}")]
    public async Task<IActionResult> DeleteSearchHistoryItem(string userId, string historyId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ObjectId.TryParse(historyId, out var historyObjectId))
                return BadRequest(new { message = "Invalid history ID format" });

            await _searchService.DeleteSearchHistoryItemAsync(userObjectId, historyObjectId);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete search history item {HistoryId} for user {UserId}", historyId, userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
