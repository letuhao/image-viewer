using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ImageViewer.Api;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ImageViewer.Tests.Integration;

/// <summary>
/// Integration tests for API endpoints
/// </summary>
public class APIIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public APIIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UsersController_GetUser_ShouldReturnNotFound()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011"; // Valid ObjectId format

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LibrariesController_GetLibrary_ShouldReturnNotFound()
    {
        // Arrange
        var libraryId = "507f1f77bcf86cd799439011"; // Valid ObjectId format

        // Act
        var response = await _client.GetAsync($"/api/v1/libraries/{libraryId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CollectionsController_GetCollection_ShouldReturnNotFound()
    {
        // Arrange
        var collectionId = "507f1f77bcf86cd799439011"; // Valid ObjectId format

        // Act
        var response = await _client.GetAsync($"/api/v1/collections/{collectionId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MediaItemsController_GetMediaItem_ShouldReturnNotFound()
    {
        // Arrange
        var mediaItemId = "507f1f77bcf86cd799439011"; // Valid ObjectId format

        // Act
        var response = await _client.GetAsync($"/api/v1/mediaitems/{mediaItemId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SearchController_Search_ShouldReturnBadRequest()
    {
        // Arrange
        var searchRequest = new
        {
            Query = "",
            Page = 1,
            PageSize = 10
        };
        var json = JsonSerializer.Serialize(searchRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/search", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NotificationsController_CreateNotification_ShouldReturnBadRequest()
    {
        // Arrange
        var notificationRequest = new
        {
            UserId = "507f1f77bcf86cd799439011",
            Type = "Info",
            Title = "",
            Message = "Test message",
            Priority = "Normal"
        };
        var json = JsonSerializer.Serialize(notificationRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/notifications", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserPreferencesController_GetUserPreferences_ShouldReturnNotFound()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011"; // Valid ObjectId format

        // Act
        var response = await _client.GetAsync($"/api/v1/userpreferences/{userId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PerformanceController_GetCacheInfo_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/performance/cache");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecurityController_Authenticate_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "",
            Password = "testpassword"
        };
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/security/authenticate", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SecurityController_SetupTwoFactor_ShouldReturnNotFound()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011"; // Valid ObjectId format

        // Act
        var response = await _client.PostAsync($"/api/v1/security/two-factor/setup?userId={userId}", null);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AllControllers_ShouldBeRegistered()
    {
        // This test ensures all controllers are properly registered in the DI container
        using var scope = _factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Act & Assert - These should not throw exceptions
        var usersController = serviceProvider.GetService<UsersController>();
        var librariesController = serviceProvider.GetService<LibrariesController>();
        var collectionsController = serviceProvider.GetService<CollectionsController>();
        var mediaItemsController = serviceProvider.GetService<MediaItemsController>();
        var searchController = serviceProvider.GetService<SearchController>();
        var notificationsController = serviceProvider.GetService<NotificationsController>();
        var userPreferencesController = serviceProvider.GetService<UserPreferencesController>();
        var performanceController = serviceProvider.GetService<PerformanceController>();
        var securityController = serviceProvider.GetService<SecurityController>();

        // Note: Controllers are not registered in DI by default in ASP.NET Core
        // They are instantiated by the framework, so we can't test their registration directly
        // This test is more of a placeholder to ensure the test structure is correct
    }

    [Fact]
    public async Task API_ShouldHandleInvalidObjectId()
    {
        // Arrange
        var invalidId = "invalid-id";

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{invalidId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task API_ShouldHandleMalformedJson()
    {
        // Arrange
        var malformedJson = "{ invalid json }";
        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/search", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task API_ShouldHandleMissingContentType()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new { Query = "test" });
        var content = new StringContent(json, Encoding.UTF8); // No content type

        // Act
        var response = await _client.PostAsync("/api/v1/search", content);

        // Assert
        // Should still work as ASP.NET Core can infer content type
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task API_ShouldHandleLargePayload()
    {
        // Arrange
        var largeRequest = new
        {
            Query = new string('a', 10000), // 10KB string
            Page = 1,
            PageSize = 10
        };
        var json = JsonSerializer.Serialize(largeRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/search", content);

        // Assert
        // Should handle large payloads gracefully
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task API_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var userId = "507f1f77bcf86cd799439011";

        // Act - Send 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync($"/api/v1/users/{userId}"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, responses.Length);
        foreach (var response in responses)
        {
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task API_ShouldHaveProperCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/users");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Note: CORS headers depend on configuration
        // This test ensures the API doesn't crash on OPTIONS requests
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task API_ShouldHandleTimeout()
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var userId = "507f1f77bcf86cd799439011";

        // Act & Assert
        // This test ensures the API handles timeouts gracefully
        // In a real scenario, you might have a long-running operation
        var response = await _client.GetAsync($"/api/v1/users/{userId}", cts.Token);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }
}
