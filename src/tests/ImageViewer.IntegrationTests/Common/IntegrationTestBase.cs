using ImageViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImageViewer.IntegrationTests.Common;

/// <summary>
/// Base class for integration tests, providing access to the test fixture and common services.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFixture>, IDisposable
{
    protected readonly IntegrationTestFixture Fixture;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ImageViewerDbContext DbContext;
    protected readonly ILogger Logger;

    // Real file system path
    protected const string REAL_IMAGE_FOLDER = @"L:\EMedia\AI_Generated\AiASAG";

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
        ServiceProvider = fixture.ServiceProvider;
        DbContext = ServiceProvider.GetRequiredService<ImageViewerDbContext>();
        Logger = ServiceProvider.GetRequiredService<ILogger<IntegrationTestBase>>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Clean up resources if needed
    }
}
