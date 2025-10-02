using ImageViewer.Api;
using ImageViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests.Common;

/// <summary>
/// Integration Test Fixture with real database and services
/// </summary>
public class IntegrationTestFixture : WebApplicationFactory<Program>, IDisposable
{
    private const string REAL_DATABASE_CONNECTION = "Host=localhost;Port=5433;Database=imageviewer_integration;Username=postgres;Password=123456";

    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public ImageViewerDbContext DbContext { get; private set; } = null!;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the default database context
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ImageViewerDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add real database context
                services.AddDbContext<ImageViewerDbContext>(options =>
                {
                    options.UseNpgsql(REAL_DATABASE_CONNECTION, npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(3);
                        npgsqlOptions.CommandTimeout(30);
                    });
                });

                // Add logging for debugging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
        }

        public new WebApplicationFactory<Program> WithWebHostBuilder(Action<IWebHostBuilder> configuration)
        {
            var factory = base.WithWebHostBuilder(configuration);
            
            // Initialize ServiceProvider after WebApplicationFactory is created
            ServiceProvider = factory.Services;
            
            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ImageViewerDbContext>();

            try
            {
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationTestFixture>>();
                logger.LogError(ex, "Failed to create database");
                throw;
            }

            DbContext = context;
            
            return factory;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (ServiceProvider != null)
                    {
                        // Clean up test database
                        using var scope = ServiceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<ImageViewerDbContext>();

                        // Remove test data
                        var testCollections = context.Collections
                            .Where(c => c.Name.Contains("TEST_") || c.Name.Contains("IntegrationTest"))
                            .ToList();

                        if (testCollections.Any())
                        {
                            context.Collections.RemoveRange(testCollections);
                            context.SaveChanges();
                        }

                        // Drop the database if it was created by the test fixture
                        context.Database.EnsureDeleted();
                    }
                }
                catch (Exception ex)
                {
                    // Log error without using ServiceProvider to avoid null reference
                    Console.WriteLine($"Failed to clean up test database: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }
}
