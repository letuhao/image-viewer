using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Collections;
using ImageViewer.Application.Mappings;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Random collection controller
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class RandomController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly ILogger<RandomController> _logger;

    public RandomController(ICollectionService collectionService, ILogger<RandomController> logger)
    {
        _collectionService = collectionService;
        _logger = logger;
    }

    /// <summary>
    /// Get random collection (uses Redis index for O(1) random selection from all collections)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CollectionOverviewDto>> GetRandomCollection()
    {
        try
        {
            _logger.LogInformation("Getting random collection");
            
            // Get total count (fast O(1) with Redis)
            var totalCount = await _collectionService.GetTotalCollectionsCountAsync();
            
            if (totalCount == 0)
            {
                _logger.LogWarning("No collections found");
                return NotFound(new { error = "No collections found" });
            }
            
            // Pick random page (1 to totalCount)
            var random = new Random();
            var randomPosition = random.Next(1, (int)totalCount + 1);
            
            _logger.LogInformation("Selecting random collection at position {Position} of {Total}", 
                randomPosition, totalCount);
            
            // Get single collection at that position (page = position, pageSize = 1)
            var collections = await _collectionService.GetCollectionsAsync(randomPosition, 1, "updatedAt", "desc");
            var randomCollection = collections.FirstOrDefault();
            
            if (randomCollection == null)
            {
                _logger.LogWarning("Failed to get collection at position {Position}", randomPosition);
                return NotFound(new { error = "Collection not found" });
            }
            
            _logger.LogInformation("Selected random collection {CollectionId} with name {CollectionName}", 
                randomCollection.Id, randomCollection.Name);
            
            // Convert to DTO with proper serialization
            var dto = randomCollection.ToOverviewDto();
            
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random collection");
            return StatusCode(500, "Internal server error");
        }
    }
}
