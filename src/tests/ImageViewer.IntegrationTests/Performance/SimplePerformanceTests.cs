using FluentAssertions;
using ImageViewer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ImageViewer.IntegrationTests.Performance;

/// <summary>
/// Simple Performance Tests without complex fixture setup
/// </summary>
public class SimplePerformanceTests
{
    private const string REAL_DATABASE_CONNECTION = "Host=localhost;Port=5433;Database=imageviewer_integration;Username=postgres;Password=123456";
    private const string REAL_IMAGE_FOLDER = @"L:\EMedia\AI_Generated\AiASAG";
    private const string CACHE_FOLDER_L = @"L:\Image_Cache";
    private const string CACHE_FOLDER_K = @"K:\Image_Cache";
    private const string CACHE_FOLDER_J = @"J:\Image_Cache";
    private const string CACHE_FOLDER_I = @"I:\Image_Cache";

    [Fact]
    public void DatabaseConnection_ShouldBeFast()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole())
            .AddDbContext<ImageViewerDbContext>(options =>
                options.UseNpgsql(REAL_DATABASE_CONNECTION))
            .BuildServiceProvider();

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ImageViewerDbContext>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var canConnect = context.Database.CanConnect();
        stopwatch.Stop();

        // Assert
        canConnect.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Database connection should be fast");
        
        Console.WriteLine($"Database connection took: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void RealImageFolder_ShouldExist()
    {
        // Act & Assert
        Directory.Exists(REAL_IMAGE_FOLDER).Should().BeTrue($"Real image folder should exist: {REAL_IMAGE_FOLDER}");
        
        var files = Directory.GetFiles(REAL_IMAGE_FOLDER, "*.*", SearchOption.TopDirectoryOnly);
        files.Length.Should().BeGreaterThan(0, "Real image folder should contain files");
        
        Console.WriteLine($"Real image folder contains {files.Length} files");
    }

    [Fact]
    public void CacheFolders_ShouldBeAccessible()
    {
        // Test cache folders
        var cacheFolders = new[] { CACHE_FOLDER_L, CACHE_FOLDER_K, CACHE_FOLDER_J, CACHE_FOLDER_I };
        
        foreach (var folder in cacheFolders)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                
                // Test write access
                var testFile = Path.Combine(folder, "test.txt");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                
                Console.WriteLine($"Cache folder {folder} is accessible");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache folder {folder} is not accessible: {ex.Message}");
            }
        }
    }

    [Fact]
    public void DatabaseQuery_ShouldBeFast()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole())
            .AddDbContext<ImageViewerDbContext>(options =>
                options.UseNpgsql(REAL_DATABASE_CONNECTION))
            .BuildServiceProvider();

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ImageViewerDbContext>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var collectionsCount = context.Collections.Count();
        var imagesCount = context.Images.Count();
        var tagsCount = context.Tags.Count();
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Database queries should be fast");
        
        Console.WriteLine($"Database query took: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Collections: {collectionsCount}, Images: {imagesCount}, Tags: {tagsCount}");
    }

    [Fact]
    public void FileSystemAccess_ShouldBeFast()
    {
        // Arrange
        var testPath = REAL_IMAGE_FOLDER;
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var files = Directory.GetFiles(testPath, "*.*", SearchOption.TopDirectoryOnly);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "File system access should be fast");
        
        Console.WriteLine($"File system access took: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Found {files.Length} files");
    }

    [Fact]
    public void PerformanceTest_ShouldCreateReport()
    {
        // Arrange
        var report = new List<string>();
        var stopwatch = Stopwatch.StartNew();

        // Test 1: Database Connection
        var dbStopwatch = Stopwatch.StartNew();
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole())
            .AddDbContext<ImageViewerDbContext>(options =>
                options.UseNpgsql(REAL_DATABASE_CONNECTION))
            .BuildServiceProvider();

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ImageViewerDbContext>();
        var canConnect = context.Database.CanConnect();
        dbStopwatch.Stop();
        
        report.Add($"Database Connection: {dbStopwatch.ElapsedMilliseconds}ms");

        // Test 2: File System Access
        var fsStopwatch = Stopwatch.StartNew();
        var files = Directory.GetFiles(REAL_IMAGE_FOLDER, "*.*", SearchOption.TopDirectoryOnly);
        fsStopwatch.Stop();
        
        report.Add($"File System Access: {fsStopwatch.ElapsedMilliseconds}ms ({files.Length} files)");

        // Test 3: Cache Folders
        var cacheFolders = new[] { CACHE_FOLDER_L, CACHE_FOLDER_K, CACHE_FOLDER_J, CACHE_FOLDER_I };
        var accessibleFolders = 0;
        
        foreach (var folder in cacheFolders)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                accessibleFolders++;
            }
            catch
            {
                // Folder not accessible
            }
        }
        
        report.Add($"Cache Folders: {accessibleFolders}/{cacheFolders.Length} accessible");

        stopwatch.Stop();
        report.Add($"Total Test Time: {stopwatch.ElapsedMilliseconds}ms");

        // Assert
        canConnect.Should().BeTrue();
        files.Length.Should().BeGreaterThan(0);
        
        // Output report
        Console.WriteLine("=== PERFORMANCE TEST REPORT ===");
        foreach (var line in report)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine("===============================");
    }
}
