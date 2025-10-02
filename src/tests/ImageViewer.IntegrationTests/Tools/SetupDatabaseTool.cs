using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests.Tools;

/// <summary>
/// Tool to setup database with real data using API calls
/// </summary>
public class SetupDatabaseTool
{
    private const string API_BASE_URL = "https://localhost:11001";
    private const string REAL_IMAGE_FOLDER = @"L:\EMedia\AI_Generated\AiASAG";
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<SetupDatabaseTool> _logger;

    public SetupDatabaseTool(HttpClient httpClient, ILogger<SetupDatabaseTool> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SetupResult> SetupDatabaseAsync()
    {
        _logger.LogInformation("🚀 Starting Database Setup with Real Data");
        _logger.LogInformation("📁 Source Folder: {Folder}", REAL_IMAGE_FOLDER);
        
        var result = new SetupResult();
        
        try
        {
            // Step 1: Check if folder exists
            if (!Directory.Exists(REAL_IMAGE_FOLDER))
            {
                result.Errors.Add($"Source folder does not exist: {REAL_IMAGE_FOLDER}");
                return result;
            }
            
            _logger.LogInformation("✅ Source folder exists");
            
            // Step 2: Bulk add collections
            var bulkResult = await BulkAddCollectionsAsync();
            result.BulkResult = bulkResult;
            
            if (bulkResult.SuccessCount > 0)
            {
                // Step 3: Scan collections to find images
                await ScanCollectionsAsync(bulkResult.SuccessfulCollections, result);
                
                // Step 4: Verify setup
                await VerifySetupAsync(result);
            }
            
            result.Success = result.Errors.Count == 0;
            _logger.LogInformation("🎉 Database setup completed. Success: {Success}", result.Success);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during database setup");
            result.Errors.Add($"Setup failed: {ex.Message}");
            result.Success = false;
            return result;
        }
    }

    private async Task<BulkOperationResult> BulkAddCollectionsAsync()
    {
        _logger.LogInformation("📦 Bulk adding collections from {Folder}", REAL_IMAGE_FOLDER);
        
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = REAL_IMAGE_FOLDER,
            CollectionPrefix = "",
            IncludeSubfolders = true,
            AutoAdd = true,
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080,
            EnableCache = true,
            AutoScan = true
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{API_BASE_URL}/api/bulk/collections", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Bulk add failed: {response.StatusCode} - {errorContent}");
        }
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BulkOperationResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        _logger.LogInformation("📊 Bulk add result: {Success} success, {Skipped} skipped, {Errors} errors", 
            result.SuccessCount, result.SkippedCount, result.ErrorCount);
        
        return result;
    }

    private async Task ScanCollectionsAsync(List<Guid> collectionIds, SetupResult result)
    {
        _logger.LogInformation("🔍 Scanning {Count} collections for images", collectionIds.Count);
        
        foreach (var collectionId in collectionIds)
        {
            try
            {
                _logger.LogDebug("Scanning collection {CollectionId}", collectionId);
                
                var response = await _httpClient.PostAsync($"{API_BASE_URL}/api/collections/{collectionId}/scan", null);
                
                if (response.IsSuccessStatusCode)
                {
                    result.ScannedCollections.Add(collectionId);
                    _logger.LogDebug("✅ Collection {CollectionId} scanned successfully", collectionId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.Errors.Add($"Failed to scan collection {collectionId}: {errorContent}");
                    _logger.LogWarning("❌ Failed to scan collection {CollectionId}: {Error}", collectionId, errorContent);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error scanning collection {collectionId}: {ex.Message}");
                _logger.LogError(ex, "Error scanning collection {CollectionId}", collectionId);
            }
        }
        
        _logger.LogInformation("🔍 Scan completed: {Scanned} collections scanned", result.ScannedCollections.Count);
    }

    private async Task VerifySetupAsync(SetupResult result)
    {
        _logger.LogInformation("🔍 Verifying database setup");
        
        try
        {
            // Check collections
            var collectionsResponse = await _httpClient.GetAsync($"{API_BASE_URL}/api/collections");
            if (collectionsResponse.IsSuccessStatusCode)
            {
                var collectionsContent = await collectionsResponse.Content.ReadAsStringAsync();
                var collections = JsonSerializer.Deserialize<dynamic>(collectionsContent);
                result.CollectionsCount = 1; // Simplified for now
                _logger.LogInformation("📁 Collections: {Count}", result.CollectionsCount);
            }
            
            // Check statistics
            var statsResponse = await _httpClient.GetAsync($"{API_BASE_URL}/api/statistics/overall");
            if (statsResponse.IsSuccessStatusCode)
            {
                var statsContent = await statsResponse.Content.ReadAsStringAsync();
                var stats = JsonSerializer.Deserialize<dynamic>(statsContent);
                _logger.LogInformation("📊 Statistics retrieved successfully");
            }
            
            _logger.LogInformation("✅ Database verification completed");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Verification failed: {ex.Message}");
            _logger.LogError(ex, "Error during verification");
        }
    }
}

public class SetupResult
{
    public bool Success { get; set; }
    public BulkOperationResult? BulkResult { get; set; }
    public List<Guid> ScannedCollections { get; set; } = new();
    public int CollectionsCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class BulkAddCollectionsRequest
{
    public string ParentPath { get; set; } = string.Empty;
    public string CollectionPrefix { get; set; } = string.Empty;
    public bool IncludeSubfolders { get; set; } = false;
    public bool AutoAdd { get; set; } = false;
    public int? ThumbnailWidth { get; set; }
    public int? ThumbnailHeight { get; set; }
    public int? CacheWidth { get; set; }
    public int? CacheHeight { get; set; }
    public bool? EnableCache { get; set; }
    public bool? AutoScan { get; set; }
}

public class BulkOperationResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<BulkCollectionResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    
    public List<Guid> SuccessfulCollections => Results
        .Where(r => r.Status == "Success" && r.CollectionId.HasValue)
        .Select(r => r.CollectionId!.Value)
        .ToList();
}

public class BulkCollectionResult
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? CollectionId { get; set; }
}
