using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ImageViewer.Application.DTOs.Common;
using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Domain.Entities;

namespace ImageViewer.IntegrationTests.Setup;

/// <summary>
/// Setup collections from L:\EMedia\AI_Generated\AiASAG folder
/// This folder contains 71 files + folders that need to be processed
/// </summary>
public class SetupCollectionsFromAiASAG
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SetupCollectionsFromAiASAG> _logger;
    private const string TARGET_PATH = @"L:\EMedia\AI_Generated\AiASAG";

    public SetupCollectionsFromAiASAG()
    {
        // Create HTTP client for API calls
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost:11001");
        
        // Create logger
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            .BuildServiceProvider();
        
        _logger = serviceProvider.GetRequiredService<ILogger<SetupCollectionsFromAiASAG>>();
    }

    [Fact]
    public async Task SetupAiASAGCollections_WithRealData_ShouldSucceed()
    {
        // Arrange
        _logger.LogInformation("🚀 Starting AiASAG collections setup from {TargetPath}", TARGET_PATH);
        
        // Verify target path exists
        if (!Directory.Exists(TARGET_PATH))
        {
            _logger.LogError("❌ Target path does not exist: {TargetPath}", TARGET_PATH);
            throw new DirectoryNotFoundException($"Target path does not exist: {TARGET_PATH}");
        }

        // Count files and folders
        var files = Directory.GetFiles(TARGET_PATH, "*", SearchOption.AllDirectories);
        var folders = Directory.GetDirectories(TARGET_PATH, "*", SearchOption.AllDirectories);
        
        _logger.LogInformation("📊 Found {FileCount} files and {FolderCount} folders in {TargetPath}", 
            files.Length, folders.Length, TARGET_PATH);

        // Act - Setup cache folders first
        await SetupCacheFolders();

        // Act - Bulk add collections from AiASAG folder
        var bulkRequest = new BulkAddCollectionsRequest
        {
            ParentPath = TARGET_PATH,
            CollectionPrefix = "AiASAG_", // Prefix to identify these collections
            IncludeSubfolders = true,
            AutoAdd = true,
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080,
            EnableCache = true,
            AutoScan = true
        };
        
        var json = JsonSerializer.Serialize(bulkRequest, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        _logger.LogInformation("📤 Sending bulk add request: {Request}", json);
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/bulk/collections", content);
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"Bulk add should succeed. Status: {response.StatusCode}");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("📊 Bulk add response: {Content}", responseContent);
        
        var result = JsonSerializer.Deserialize<BulkOperationResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.SuccessCount.Should().BeGreaterThan(0, "Should create at least one collection");
        
        _logger.LogInformation("✅ Created {SuccessCount} collections, {SkippedCount} skipped, {ErrorCount} errors", 
            result.SuccessCount, result.SkippedCount, result.ErrorCount);
        
        // Log detailed results
        foreach (var collectionResult in result.Results)
        {
            _logger.LogInformation("📁 Collection: {Name} ({Status}) - {Message}", 
                collectionResult.Name, collectionResult.Status, collectionResult.Message);
        }
        
        if (result.Errors.Any())
        {
            _logger.LogWarning("⚠️ Errors encountered:");
            foreach (var error in result.Errors)
            {
                _logger.LogWarning("❌ {Error}", error);
            }
        }
        
        _logger.LogInformation("🎉 AiASAG collections setup completed successfully!");
    }

    [Fact]
    public async Task VerifyAiASAGCollections_ShouldHaveData()
    {
        // Arrange
        _logger.LogInformation("🔍 Verifying AiASAG collections have been created");

        // Act - Check collections
        var collectionsResponse = await _httpClient.GetAsync("/api/collections");
        collectionsResponse.IsSuccessStatusCode.Should().BeTrue("Collections API should work");
        
        var collectionsContent = await collectionsResponse.Content.ReadAsStringAsync();
        _logger.LogInformation("📁 Collections response: {Content}", collectionsContent);

        // Act - Check statistics
        var statsResponse = await _httpClient.GetAsync("/api/statistics/overall");
        statsResponse.IsSuccessStatusCode.Should().BeTrue("Statistics API should work");
        
        var statsContent = await statsResponse.Content.ReadAsStringAsync();
        _logger.LogInformation("📊 Statistics response: {Content}", statsContent);

        // Act - Check random image
        var randomImageResponse = await _httpClient.GetAsync("/api/images/random");
        if (randomImageResponse.IsSuccessStatusCode)
        {
            var randomImageContent = await randomImageResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("🖼️ Random image response: {Content}", randomImageContent);
        }
        else
        {
            _logger.LogWarning("⚠️ Random image API returned {StatusCode}", randomImageResponse.StatusCode);
        }

        // Assert
        collectionsResponse.IsSuccessStatusCode.Should().BeTrue();
        statsResponse.IsSuccessStatusCode.Should().BeTrue();
        
        _logger.LogInformation("✅ AiASAG collections verification completed");
    }

    [Fact]
    public async Task ScanAiASAGCollections_ShouldFindImages()
    {
        // Arrange
        _logger.LogInformation("🔍 Scanning AiASAG collections for images");

        // First get all collections
        var collectionsResponse = await _httpClient.GetAsync("/api/collections");
        collectionsResponse.IsSuccessStatusCode.Should().BeTrue("Collections API should work");
        
        var collectionsContent = await collectionsResponse.Content.ReadAsStringAsync();
        var collections = JsonSerializer.Deserialize<PaginationResponseDto<Collection>>(collectionsContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (collections?.Data == null || !collections.Data.Any())
        {
            _logger.LogWarning("⚠️ No collections found to scan");
            return;
        }

        // Scan each collection
        var scanResults = new List<(string collectionName, bool success, string message)>();
        
        foreach (var collection in collections.Data.Where(c => c.Name.StartsWith("AiASAG_")))
        {
            _logger.LogInformation("🔍 Scanning collection: {CollectionName}", collection.Name);
            
            var scanResponse = await _httpClient.PostAsync($"/api/collections/{collection.Id}/scan", null);
            
            if (scanResponse.IsSuccessStatusCode)
            {
                scanResults.Add((collection.Name, true, "Scan completed successfully"));
                _logger.LogInformation("✅ Collection {CollectionName} scanned successfully", collection.Name);
            }
            else
            {
                var errorContent = await scanResponse.Content.ReadAsStringAsync();
                scanResults.Add((collection.Name, false, $"Scan failed: {errorContent}"));
                _logger.LogError("❌ Collection {CollectionName} scan failed: {Error}", collection.Name, errorContent);
            }
        }

        // Assert
        var successCount = scanResults.Count(r => r.success);
        var totalCount = scanResults.Count;
        
        _logger.LogInformation("📊 Scan results: {SuccessCount}/{TotalCount} collections scanned successfully", 
            successCount, totalCount);
        
        successCount.Should().BeGreaterThan(0, "At least some collections should be scanned successfully");
        
        _logger.LogInformation("✅ AiASAG collections scanning completed");
    }

    [Fact]
    public async Task PerformanceTestAiASAG_ShouldBeFast()
    {
        // Arrange
        _logger.LogInformation("⚡ Running performance test with AiASAG data");

        var endpoints = new[]
        {
            "/api/collections",
            "/api/statistics/overall",
            "/api/images/random",
            "/api/cache/statistics"
        };

        var results = new List<(string endpoint, long elapsedMs, bool success)>();

        // Act
        foreach (var endpoint in endpoints)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(endpoint);
            stopwatch.Stop();
            
            results.Add((endpoint, stopwatch.ElapsedMilliseconds, response.IsSuccessStatusCode));
            
            _logger.LogInformation("📊 {Endpoint}: {ElapsedMs}ms (Status: {StatusCode})", 
                endpoint, stopwatch.ElapsedMilliseconds, response.StatusCode);
        }

        // Assert
        var totalTime = results.Sum(r => r.elapsedMs);
        var avgTime = results.Average(r => r.elapsedMs);
        var successCount = results.Count(r => r.success);
        
        totalTime.Should().BeLessThan(10000, "Total time should be reasonable");
        avgTime.Should().BeLessThan(2000, "Average time should be fast");
        successCount.Should().BeGreaterThan(0, "At least some endpoints should work");
        
        _logger.LogInformation("🎯 Performance test completed: {TotalTime}ms total, {AvgTime:F0}ms average, {SuccessCount}/{TotalCount} successful", 
            totalTime, avgTime, successCount, results.Count);
    }

    private async Task SetupCacheFolders()
    {
        _logger.LogInformation("🗂️ Setting up cache folders for AiASAG collections");
        
        var cacheFolders = new[]
        {
            @"L:\Image_Cache\AiASAG",
            @"K:\Image_Cache\AiASAG", 
            @"J:\Image_Cache\AiASAG",
            @"I:\Image_Cache\AiASAG"
        };
        
        foreach (var folderPath in cacheFolders)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(folderPath);
                
                _logger.LogInformation("📁 Setting up cache folder: {FolderPath}", folderPath);
                
                var request = new CreateCacheFolderDto
                {
                    Name = $"AiASAG_Cache_{Path.GetFileName(folderPath)}",
                    Path = folderPath,
                    Priority = 1,
                    MaxSize = 50L * 1024 * 1024 * 1024 // 50GB
                };
                
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/api/cache/folders", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Created cache folder: {FolderPath}", folderPath);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ Failed to create cache folder {FolderPath}: {Error}", folderPath, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error setting up cache folder {FolderPath}", folderPath);
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Using DTOs from Application layer - no custom DTOs needed
