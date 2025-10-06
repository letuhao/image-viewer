using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageViewer.Test.Features.SearchAndDiscovery.Unit;

/// <summary>
/// Unit tests for SearchService - Search and Discovery features
/// </summary>
public class SearchServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILibraryRepository> _mockLibraryRepository;
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<IMediaItemRepository> _mockMediaItemRepository;
    private readonly Mock<ILogger<SearchService>> _mockLogger;
    private readonly SearchService _searchService;

    public SearchServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLibraryRepository = new Mock<ILibraryRepository>();
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockMediaItemRepository = new Mock<IMediaItemRepository>();
        _mockLogger = new Mock<ILogger<SearchService>>();

        _searchService = new SearchService(
            _mockUserRepository.Object,
            _mockLibraryRepository.Object,
            _mockCollectionRepository.Object,
            _mockMediaItemRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ShouldReturnSearchResults()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test query",
            Type = SearchType.All,
            Page = 1,
            PageSize = 10
        };

        var libraries = new List<Library>();
        var collections = new List<Collection>();
        var mediaItems = new List<MediaItem>();

        _mockLibraryRepository.Setup(x => x.SearchLibrariesAsync(request.Query))
            .ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.SearchCollectionsAsync(request.Query))
            .ReturnsAsync(collections);
        _mockMediaItemRepository.Setup(x => x.SearchMediaItemsAsync(request.Query))
            .ReturnsAsync(mediaItems);

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.SearchTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ShouldThrowValidationException()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "",
            Type = SearchType.All
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _searchService.SearchAsync(request));
    }

    [Fact]
    public async Task SearchAsync_WithNullQuery_ShouldThrowValidationException()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = null!,
            Type = SearchType.All
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _searchService.SearchAsync(request));
    }

    [Fact]
    public async Task SearchAsync_WithWhitespaceQuery_ShouldThrowValidationException()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "   ",
            Type = SearchType.All
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _searchService.SearchAsync(request));
    }

    [Fact]
    public async Task SearchAsync_WithLibrariesType_ShouldSearchOnlyLibraries()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test",
            Type = SearchType.Libraries,
            Page = 1,
            PageSize = 10
        };

        var libraries = new List<Library>();
        _mockLibraryRepository.Setup(x => x.SearchLibrariesAsync(request.Query))
            .ReturnsAsync(libraries);

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        _mockLibraryRepository.Verify(x => x.SearchLibrariesAsync(request.Query), Times.Once);
        _mockCollectionRepository.Verify(x => x.SearchCollectionsAsync(It.IsAny<string>()), Times.Never);
        _mockMediaItemRepository.Verify(x => x.SearchMediaItemsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithCollectionsType_ShouldSearchOnlyCollections()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test",
            Type = SearchType.Collections,
            Page = 1,
            PageSize = 10
        };

        var collections = new List<Collection>();
        _mockCollectionRepository.Setup(x => x.SearchCollectionsAsync(request.Query))
            .ReturnsAsync(collections);

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        _mockCollectionRepository.Verify(x => x.SearchCollectionsAsync(request.Query), Times.Once);
        _mockLibraryRepository.Verify(x => x.SearchLibrariesAsync(It.IsAny<string>()), Times.Never);
        _mockMediaItemRepository.Verify(x => x.SearchMediaItemsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithMediaItemsType_ShouldSearchOnlyMediaItems()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test",
            Type = SearchType.MediaItems,
            Page = 1,
            PageSize = 10
        };

        var mediaItems = new List<MediaItem>();
        _mockMediaItemRepository.Setup(x => x.SearchMediaItemsAsync(request.Query))
            .ReturnsAsync(mediaItems);

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        _mockMediaItemRepository.Verify(x => x.SearchMediaItemsAsync(request.Query), Times.Once);
        _mockLibraryRepository.Verify(x => x.SearchLibrariesAsync(It.IsAny<string>()), Times.Never);
        _mockCollectionRepository.Verify(x => x.SearchCollectionsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithInvalidSearchType_ShouldThrowValidationException()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test",
            Type = (SearchType)999, // Invalid enum value
            Page = 1,
            PageSize = 10
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _searchService.SearchAsync(request));
    }

    [Fact]
    public async Task SearchLibrariesAsync_WithValidRequest_ShouldReturnLibraryResults()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test library",
            Page = 1,
            PageSize = 10
        };

        var libraries = new List<Library>();
        _mockLibraryRepository.Setup(x => x.SearchLibrariesAsync(request.Query))
            .ReturnsAsync(libraries);

        // Act
        var result = await _searchService.SearchLibrariesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.SearchTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task SearchCollectionsAsync_WithValidRequest_ShouldReturnCollectionResults()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test collection",
            Page = 1,
            PageSize = 10
        };

        var collections = new List<Collection>();
        _mockCollectionRepository.Setup(x => x.SearchCollectionsAsync(request.Query))
            .ReturnsAsync(collections);

        // Act
        var result = await _searchService.SearchCollectionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.SearchTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task SearchMediaItemsAsync_WithValidRequest_ShouldReturnMediaItemResults()
    {
        // Arrange
        var request = new SearchRequest
        {
            Query = "test media",
            Page = 1,
            PageSize = 10
        };

        var mediaItems = new List<MediaItem>();
        _mockMediaItemRepository.Setup(x => x.SearchMediaItemsAsync(request.Query))
            .ReturnsAsync(mediaItems);

        // Act
        var result = await _searchService.SearchMediaItemsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.SearchTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_WithValidQuery_ShouldReturnSuggestions()
    {
        // Arrange
        var query = "test";
        var limit = 5;

        // Act
        var result = await _searchService.GetSearchSuggestionsAsync(query, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(limit);
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_WithEmptyQuery_ShouldReturnEmptyList()
    {
        // Arrange
        var query = "";
        var limit = 5;

        // Act
        var result = await _searchService.GetSearchSuggestionsAsync(query, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_WithNullQuery_ShouldReturnEmptyList()
    {
        // Arrange
        string query = null!;
        var limit = 5;

        // Act
        var result = await _searchService.GetSearchSuggestionsAsync(query, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAutoCompleteAsync_WithValidPartialQuery_ShouldReturnCompletions()
    {
        // Arrange
        var partialQuery = "test";
        var limit = 5;

        // Act
        var result = await _searchService.GetAutoCompleteAsync(partialQuery, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(limit);
    }

    [Fact]
    public async Task GetAutoCompleteAsync_WithEmptyPartialQuery_ShouldReturnEmptyList()
    {
        // Arrange
        var partialQuery = "";
        var limit = 5;

        // Act
        var result = await _searchService.GetAutoCompleteAsync(partialQuery, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAutoCompleteAsync_WithNullPartialQuery_ShouldReturnEmptyList()
    {
        // Arrange
        string partialQuery = null!;
        var limit = 5;

        // Act
        var result = await _searchService.GetAutoCompleteAsync(partialQuery, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSmartSuggestionsAsync_WithValidQuery_ShouldReturnSuggestions()
    {
        // Arrange
        var query = "test";
        var userId = ObjectId.GenerateNewId();

        // Act
        var result = await _searchService.GetSmartSuggestionsAsync(query, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSearchAnalyticsAsync_WithValidParameters_ShouldReturnAnalytics()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _searchService.GetSearchAnalyticsAsync(userId, fromDate, toDate);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.FromDate.Should().Be(fromDate);
        result.ToDate.Should().Be(toDate);
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_WithValidUserId_ShouldReturnRecommendations()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var limit = 5;

        // Act
        var result = await _searchService.GetPersonalizedRecommendationsAsync(userId, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(limit);
    }

    [Fact]
    public async Task SaveSearchHistoryAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var query = "test query";
        var searchResult = new SearchResult
        {
            Items = new List<SearchResultItem>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10,
            TotalPages = 0,
            SearchTime = TimeSpan.FromMilliseconds(100)
        };

        // Act
        await _searchService.SaveSearchHistoryAsync(userId, query, searchResult);

        // Assert
        // Should complete without throwing exception
        true.Should().BeTrue();
    }

    [Fact]
    public async Task ClearSearchHistoryAsync_WithValidUserId_ShouldCompleteSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act
        await _searchService.ClearSearchHistoryAsync(userId);

        // Assert
        // Should complete without throwing exception
        true.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSearchHistoryItemAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var historyId = ObjectId.GenerateNewId();

        // Act
        await _searchService.DeleteSearchHistoryItemAsync(userId, historyId);

        // Assert
        // Should complete without throwing exception
        true.Should().BeTrue();
    }
}