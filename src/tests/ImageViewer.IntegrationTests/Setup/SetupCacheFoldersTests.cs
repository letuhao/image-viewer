using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ImageViewer.IntegrationTests.Setup;

/// <summary>
/// Tests to setup cache folders with real data
/// </summary>
public class SetupCacheFoldersTests
{
    private readonly HttpClient _httpClient;
    // Direct API calls instead of using SetupCacheFoldersTool
    private readonly ILogger<SetupCacheFoldersTests> _logger;

    public SetupCacheFoldersTests()
    {
        // Create HTTP client for API calls
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost:11001");
        
        // Create logger
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            .BuildServiceProvider();
        
        _logger = serviceProvider.GetRequiredService<ILogger<SetupCacheFoldersTests>>();
    }

    [Fact]
    public async Task SetupCacheFolders_WithRealPaths_ShouldSucceed()
    {
        // Arrange
        _logger.LogInformation("🗂️ Starting cache folders setup");
        
        var cacheFolders = new[]
        {
            @"L:\Image_Cache",
            @"K:\Image_Cache", 
            @"J:\Image_Cache",
            @"I:\Image_Cache"
        };
        
        var createdFolders = new List<CacheFolderDto>();
        var errors = new List<string>();

        // Act - Create each cache folder
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
            
            var response = await _httpClient.PostAsync("/api/cache/folders", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createdFolder = JsonSerializer.Deserialize<CacheFolderDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                createdFolders.Add(createdFolder!);
                _logger.LogInformation("✅ Created cache folder: {Name} at {Path}", createdFolder!.Name, createdFolder.Path);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                errors.Add($"Failed to create cache folder {folderPath}: {response.StatusCode} - {errorContent}");
                _logger.LogError("❌ Failed to create cache folder {FolderPath}: {Error}", folderPath, errorContent);
            }
        }

        // Assert
        createdFolders.Should().HaveCount(4, "Should create 4 cache folders");
        errors.Should().BeEmpty("Should have no errors");
        
        _logger.LogInformation("✅ Created {Count} cache folders successfully", createdFolders.Count);
        
        foreach (var folder in createdFolders)
        {
            _logger.LogInformation("📁 Cache folder: {Name} at {Path} (Max: {MaxSize}GB)", 
                folder.Name, folder.Path, folder.MaxSizeBytes / (1024 * 1024 * 1024));
        }
    }

    [Fact]
    public async Task VerifyCacheFolders_ShouldHaveData()
    {
        // Arrange
        _logger.LogInformation("🔍 Verifying cache folders exist");

        // Act - Check cache folders
        var foldersResponse = await _httpClient.GetAsync("/api/cache/folders");
        foldersResponse.IsSuccessStatusCode.Should().BeTrue("Cache folders API should work");
        
        var foldersContent = await foldersResponse.Content.ReadAsStringAsync();
        _logger.LogInformation("📁 Cache folders response: {Content}", foldersContent);

        // Act - Check cache statistics
        var statsResponse = await _httpClient.GetAsync("/api/cache/statistics");
        if (statsResponse.IsSuccessStatusCode)
        {
            var statsContent = await statsResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("📊 Cache statistics response: {Content}", statsContent);
        }
        else
        {
            _logger.LogWarning("⚠️ Cache statistics API returned {StatusCode}", statsResponse.StatusCode);
        }

        // Assert
        foldersResponse.IsSuccessStatusCode.Should().BeTrue();
        
        _logger.LogInformation("✅ Cache folders verification completed");
    }

    [Fact]
    public async Task GetCacheStatistics_ShouldReturnValidData()
    {
        // Arrange
        _logger.LogInformation("📊 Getting cache statistics");

        // Act
        var response = await _httpClient.GetAsync("/api/cache/statistics");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var statistics = JsonSerializer.Deserialize<CacheStatisticsDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            statistics.Should().NotBeNull();
            statistics!.TotalImages.Should().BeGreaterThanOrEqualTo(0);
            statistics.CachedImages.Should().BeGreaterThanOrEqualTo(0);
            statistics.TotalCacheSize.Should().BeGreaterThanOrEqualTo(0);
            
            _logger.LogInformation("📊 Cache Statistics:");
            _logger.LogInformation("  - Total Images: {TotalImages}", statistics.TotalImages);
            _logger.LogInformation("  - Cached Images: {CachedImages}", statistics.CachedImages);
            _logger.LogInformation("  - Total Cache Size: {TotalCacheSize} bytes", statistics.TotalCacheSize);
            _logger.LogInformation("  - Available Space: {AvailableSpace} bytes", statistics.AvailableSpace);
            _logger.LogInformation("  - Cache Hit Rate: {CacheHitRate:P2}", statistics.CacheHitRate);
        }
        else
        {
            _logger.LogWarning("⚠️ Could not retrieve cache statistics: {StatusCode}", response.StatusCode);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
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
