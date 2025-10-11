using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Collections;
using ImageViewer.Application.Mappings;
using ImageViewer.Domain.Entities;

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
    /// Get random collection
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CollectionOverviewDto>> GetRandomCollection()
    {
        try
        {
            _logger.LogInformation("Getting random collection");
            
            // Get all collections using the available service method
            var collections = await _collectionService.GetCollectionsAsync(1, 1000); // Get up to 1000 collections
            var activeCollections = collections.Where(c => !c.IsDeleted).ToList();
            
            if (!activeCollections.Any())
            {
                _logger.LogWarning("No active collections found");
                return NotFound(new { error = "No collections found" });
            }
            
            // Pick random collection
            var random = new Random();
            var randomIndex = random.Next(0, activeCollections.Count);
            var randomCollection = activeCollections[randomIndex];
            
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
