using ImageViewer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests.Tools;

/// <summary>
/// Tool to clear database data for testing purposes
/// </summary>
public class ClearDatabaseTool
{
    private const string REAL_DATABASE_CONNECTION = "Host=localhost;Port=5433;Database=imageviewer_integration;Username=postgres;Password=123456";
    
    public static async Task ClearDatabaseAsync()
    {
        Console.WriteLine("🗑️  Database Clear Tool");
        Console.WriteLine("=====================");
        Console.WriteLine($"Database: {REAL_DATABASE_CONNECTION}");
        Console.WriteLine();
        
        Console.WriteLine("⚠️  WARNING: This will delete ALL data in the integration test database!");
        Console.WriteLine("This includes:");
        Console.WriteLine("- All collections");
        Console.WriteLine("- All images");
        Console.WriteLine("- All tags");
        Console.WriteLine("- All view sessions");
        Console.WriteLine("- All background jobs");
        Console.WriteLine();
        
        Console.Write("Are you sure you want to proceed? (yes/no): ");
        var response = Console.ReadLine();
        
        if (response?.ToLowerInvariant() != "yes")
        {
            Console.WriteLine("❌ Operation cancelled by user.");
            return;
        }
        
        try
        {
            // Setup service provider
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add database context
            services.AddDbContext<ImageViewerDbContext>(options =>
            {
                options.UseNpgsql(REAL_DATABASE_CONNECTION, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(3);
                    npgsqlOptions.CommandTimeout(30);
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ImageViewerDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ClearDatabaseTool>>();

            Console.WriteLine("🔍 Checking database connection...");
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                Console.WriteLine("❌ Cannot connect to database. Please check your connection string.");
                return;
            }
            Console.WriteLine("✅ Database connection successful.");

            Console.WriteLine("🗑️  Clearing database data...");
            
            // Delete all data in reverse dependency order
            var deletedCounts = new Dictionary<string, int>();
            
            // Delete view sessions first (depends on collections and images)
            var viewSessionsCount = await context.ViewSessions.CountAsync();
            if (viewSessionsCount > 0)
            {
                await context.Database.ExecuteSqlRawAsync("DELETE FROM \"ViewSessions\"");
                deletedCounts["ViewSessions"] = viewSessionsCount;
            }
            
            // Delete images (depends on collections)
            var imagesCount = await context.Images.CountAsync();
            if (imagesCount > 0)
            {
                await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Images\"");
                deletedCounts["Images"] = imagesCount;
            }
            
            // Delete collections
            var collectionsCount = await context.Collections.CountAsync();
            if (collectionsCount > 0)
            {
                await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Collections\"");
                deletedCounts["Collections"] = collectionsCount;
            }
            
            // Delete tags
            var tagsCount = await context.Tags.CountAsync();
            if (tagsCount > 0)
            {
                await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Tags\"");
                deletedCounts["Tags"] = tagsCount;
            }
            
            // Delete background jobs
            var backgroundJobsCount = await context.BackgroundJobs.CountAsync();
            if (backgroundJobsCount > 0)
            {
                await context.Database.ExecuteSqlRawAsync("DELETE FROM \"BackgroundJobs\"");
                deletedCounts["BackgroundJobs"] = backgroundJobsCount;
            }
            
            // Save changes
            await context.SaveChangesAsync();
            
            Console.WriteLine("✅ Database cleared successfully!");
            Console.WriteLine();
            Console.WriteLine("📊 Deleted records:");
            foreach (var kvp in deletedCounts)
            {
                Console.WriteLine($"  - {kvp.Key}: {kvp.Value} records");
            }
            
            if (deletedCounts.Count == 0)
            {
                Console.WriteLine("  - No data found to delete");
            }
            
            Console.WriteLine();
            Console.WriteLine("🎯 Database is now ready for integration tests.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error clearing database: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
    
    public static async Task ShowDatabaseStatusAsync()
    {
        Console.WriteLine("📊 Database Status");
        Console.WriteLine("=================");
        Console.WriteLine($"Database: {REAL_DATABASE_CONNECTION}");
        Console.WriteLine();
        
        try
        {
            // Setup service provider
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning); // Reduce logging
            });

            // Add database context
            services.AddDbContext<ImageViewerDbContext>(options =>
            {
                options.UseNpgsql(REAL_DATABASE_CONNECTION, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(3);
                    npgsqlOptions.CommandTimeout(30);
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ImageViewerDbContext>();

            Console.WriteLine("🔍 Checking database connection...");
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                Console.WriteLine("❌ Cannot connect to database.");
                return;
            }
            Console.WriteLine("✅ Database connection successful.");
            Console.WriteLine();

            // Get counts
            var collectionsCount = await context.Collections.CountAsync();
            var imagesCount = await context.Images.CountAsync();
            var tagsCount = await context.Tags.CountAsync();
            var viewSessionsCount = await context.ViewSessions.CountAsync();
            var backgroundJobsCount = await context.BackgroundJobs.CountAsync();
            
            Console.WriteLine("📈 Current data:");
            Console.WriteLine($"  - Collections: {collectionsCount}");
            Console.WriteLine($"  - Images: {imagesCount}");
            Console.WriteLine($"  - Tags: {tagsCount}");
            Console.WriteLine($"  - View Sessions: {viewSessionsCount}");
            Console.WriteLine($"  - Background Jobs: {backgroundJobsCount}");
            
            var totalRecords = collectionsCount + imagesCount + tagsCount + viewSessionsCount + backgroundJobsCount;
            Console.WriteLine($"  - Total Records: {totalRecords}");
            
            if (totalRecords > 0)
            {
                Console.WriteLine();
                Console.WriteLine("💡 Use ClearDatabaseTool.ClearDatabaseAsync() to clear all data.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("✅ Database is empty and ready for tests.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking database status: {ex.Message}");
        }
    }
}
