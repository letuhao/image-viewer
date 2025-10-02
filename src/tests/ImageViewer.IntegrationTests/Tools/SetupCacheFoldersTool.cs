using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests.Tools;

/// <summary>
/// Tool to setup cache folders using API calls
/// </summary>
public class SetupCacheFoldersTool
{
    private const string API_BASE_URL = "https://localhost:11001";
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<SetupCacheFoldersTool> _logger;

    public SetupCacheFoldersTool(HttpClient httpClient, ILogger<SetupCacheFoldersTool> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SetupCacheResult> SetupCacheFoldersAsync()
    {
        _logger.LogInformation("🗂️ Starting Cache Folders Setup");
        
        var result = new SetupCacheResult();
        var cacheFolders = new[]
        {
            @"L:\Image_Cache",
            @"K:\Image_Cache", 
            @"J:\Image_Cache",
            @"I:\Image_Cache"
        };
        
        try
        {
            foreach (var folderPath in cacheFolders)
            {
                _logger.LogInformation("📁 Setting up cache folder: {FolderPath}", folderPath);
                
                var request = new CreateCacheFolderRequest
                {
                    Name = Path.GetFileName(folderPath),
                    Path = folderPath,
                    MaxSizeBytes = 100L * 1024 * 1024 * 1024 // 100GB
                };
                
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{API_BASE_URL}/api/cache/folders", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var createdFolder = JsonSerializer.Deserialize<CacheFolderDto>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    result.CreatedFolders.Add(createdFolder!);
                    _logger.LogInformation("✅ Created cache folder: {Name} at {Path}", createdFolder!.Name, createdFolder.Path);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.Errors.Add($"Failed to create cache folder {folderPath}: {response.StatusCode} - {errorContent}");
                    _logger.LogError("❌ Failed to create cache folder {FolderPath}: {Error}", folderPath, errorContent);
                }
            }
            
            result.Success = result.Errors.Count == 0;
            _logger.LogInformation("🎉 Cache folders setup completed. Created: {Count}, Errors: {ErrorCount}", 
                result.CreatedFolders.Count, result.Errors.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during cache folders setup");
            result.Errors.Add($"Setup failed: {ex.Message}");
            result.Success = false;
            return result;
        }
    }

    public async Task<CacheStatisticsDto?> GetCacheStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{API_BASE_URL}/api/cache/statistics");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CacheStatisticsDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            
            _logger.LogWarning("⚠️ Failed to get cache statistics: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return null;
        }
    }
}

public class SetupCacheResult
{
    public bool Success { get; set; }
    public List<CacheFolderDto> CreatedFolders { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class CreateCacheFolderRequest
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long MaxSizeBytes { get; set; }
}

public class CacheFolderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long MaxSizeBytes { get; set; }
    public long CurrentSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CacheStatisticsDto
{
    public int TotalImages { get; set; }
    public int CachedImages { get; set; }
    public int ExpiredImages { get; set; }
    public long TotalCacheSize { get; set; }
    public long AvailableSpace { get; set; }
    public double CacheHitRate { get; set; }
    public DateTime LastUpdated { get; set; }
}
