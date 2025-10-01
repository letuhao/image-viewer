using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Compressed files controller for archive operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CompressedFilesController : ControllerBase
{
    private readonly ICompressedFileService _compressedFileService;
    private readonly ILogger<CompressedFilesController> _logger;

    public CompressedFilesController(ICompressedFileService compressedFileService, ILogger<CompressedFilesController> logger)
    {
        _compressedFileService = compressedFileService;
        _logger = logger;
    }

    /// <summary>
    /// Get supported compressed file extensions
    /// </summary>
    [HttpGet("supported-extensions")]
    public ActionResult<SupportedExtensionsResponse> GetSupportedExtensions()
    {
        try
        {
            var extensions = _compressedFileService.GetSupportedExtensions();
            
            var response = new SupportedExtensionsResponse
            {
                Extensions = extensions,
                Count = extensions.Length
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported extensions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if file is a supported compressed format
    /// </summary>
    [HttpPost("check")]
    public async Task<ActionResult<CompressedFileCheckResponse>> CheckCompressedFile([FromBody] CompressedFileCheckRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(new { error = "File path is required" });
            }

            var isCompressed = await _compressedFileService.IsCompressedFileAsync(request.FilePath);
            
            var response = new CompressedFileCheckResponse
            {
                FilePath = request.FilePath,
                IsCompressed = isCompressed,
                CheckedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking compressed file: {FilePath}", request.FilePath);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get compressed file information
    /// </summary>
    [HttpGet("info")]
    public async Task<ActionResult<CompressedFileInfo>> GetFileInfo([FromQuery] string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return BadRequest(new { error = "File path is required" });
            }

            var info = await _compressedFileService.GetFileInfoAsync(filePath);
            
            if (info == null)
            {
                return NotFound("File not found or not a supported compressed format");
            }

            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info: {FilePath}", filePath);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if compressed file contains images
    /// </summary>
    [HttpPost("contains-images")]
    public async Task<ActionResult<ContainsImagesResponse>> CheckContainsImages([FromBody] ContainsImagesRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(new { error = "File path is required" });
            }

            var containsImages = await _compressedFileService.ContainsImagesAsync(request.FilePath);
            
            var response = new ContainsImagesResponse
            {
                FilePath = request.FilePath,
                ContainsImages = containsImages,
                CheckedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file contains images: {FilePath}", request.FilePath);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Extract images from compressed file
    /// </summary>
    [HttpPost("extract-images")]
    public async Task<ActionResult<ExtractImagesResponse>> ExtractImages([FromBody] ExtractImagesRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(new { error = "File path is required" });
            }

            _logger.LogInformation("Extracting images from compressed file: {FilePath}", request.FilePath);

            var images = await _compressedFileService.ExtractImagesAsync(request.FilePath);
            var imageList = images.ToList();

            var response = new ExtractImagesResponse
            {
                FilePath = request.FilePath,
                ExtractedImages = imageList,
                TotalImages = imageList.Count,
                ExtractedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully extracted {Count} images from {FilePath}", 
                imageList.Count, request.FilePath);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting images from: {FilePath}", request.FilePath);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class SupportedExtensionsResponse
{
    public string[] Extensions { get; set; } = Array.Empty<string>();
    public int Count { get; set; }
}

public class CompressedFileCheckRequest
{
    public string FilePath { get; set; } = string.Empty;
}

public class CompressedFileCheckResponse
{
    public string FilePath { get; set; } = string.Empty;
    public bool IsCompressed { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class ContainsImagesRequest
{
    public string FilePath { get; set; } = string.Empty;
}

public class ContainsImagesResponse
{
    public string FilePath { get; set; } = string.Empty;
    public bool ContainsImages { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class ExtractImagesRequest
{
    public string FilePath { get; set; } = string.Empty;
}

public class ExtractImagesResponse
{
    public string FilePath { get; set; } = string.Empty;
    public IEnumerable<CompressedFileImage> ExtractedImages { get; set; } = Enumerable.Empty<CompressedFileImage>();
    public int TotalImages { get; set; }
    public DateTime ExtractedAt { get; set; }
}
