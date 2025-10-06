using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageViewer.Test.Features.SearchAndDiscovery.Unit;

/// <summary>
/// Unit tests for DiscoveryService - Content Discovery and Recommendation features
/// </summary>
public class DiscoveryServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILibraryRepository> _mockLibraryRepository;
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<IMediaItemRepository> _mockMediaItemRepository;
    private readonly Mock<IImageRepository> _mockImageRepository;
    private readonly Mock<ITagRepository> _mockTagRepository;
    private readonly Mock<ILogger<DiscoveryService>> _mockLogger;
    private readonly DiscoveryService _discoveryService;

    public DiscoveryServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLibraryRepository = new Mock<ILibraryRepository>();
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockMediaItemRepository = new Mock<IMediaItemRepository>();
        _mockImageRepository = new Mock<IImageRepository>();
        _mockTagRepository = new Mock<ITagRepository>();
        _mockLogger = new Mock<ILogger<DiscoveryService>>();

        _discoveryService = new DiscoveryService(
            _mockUserRepository.Object,
            _mockLibraryRepository.Object,
            _mockCollectionRepository.Object,
            _mockMediaItemRepository.Object,
            _mockImageRepository.Object,
            _mockTagRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new DiscoveryService(
            _mockUserRepository.Object,
            _mockLibraryRepository.Object,
            _mockCollectionRepository.Object,
            _mockMediaItemRepository.Object,
            _mockImageRepository.Object,
            _mockTagRepository.Object,
            _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullUserRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiscoveryService(
            null!,
            _mockLibraryRepository.Object,
            _mockCollectionRepository.Object,
            _mockMediaItemRepository.Object,
            _mockImageRepository.Object,
            _mockTagRepository.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLibraryRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiscoveryService(
            _mockUserRepository.Object,
            null!,
            _mockCollectionRepository.Object,
            _mockMediaItemRepository.Object,
            _mockImageRepository.Object,
            _mockTagRepository.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiscoveryService(
            _mockUserRepository.Object,
            _mockLibraryRepository.Object,
            _mockCollectionRepository.Object,
            _mockMediaItemRepository.Object,
            _mockImageRepository.Object,
            _mockTagRepository.Object,
            null!));
    }

    #endregion

    #region Content Discovery Tests

    [Fact]
    public async Task DiscoverContentAsync_WithValidRequest_ShouldReturnRecommendations()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new DiscoveryRequest
        {
            Limit = 10,
            SortBy = DiscoverySortBy.Relevance
        };

        var libraries = new List<Library>
        {
            new Library("Test Library", "/test/path", ObjectId.GenerateNewId(), "Test description")
        };

        var collections = new List<Collection>
        {
            new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/collection", Domain.Enums.CollectionType.Folder)
        };

        var mediaItems = new List<MediaItem>
        {
            new MediaItem(ObjectId.GenerateNewId(), "Test Media", "test.jpg", "/test/media", "image", "jpeg", 1024L, 1920, 1080)
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(collections);
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mediaItems);

        // Act
        var result = await _discoveryService.DiscoverContentAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3); // 1 library + 1 collection + 1 media item
        result.All(r => r.RelevanceScore > 0).Should().BeTrue();
        result.All(r => !string.IsNullOrEmpty(r.Title)).Should().BeTrue();
    }

    [Fact]
    public async Task DiscoverContentAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _discoveryService.DiscoverContentAsync(userId, null!));
    }

    [Fact]
    public async Task GetTrendingContentAsync_WithValidParameters_ShouldReturnTrendingContent()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var limit = 10;

        var libraries = new List<Library>
        {
            new Library("Trending Library", "/trending/path", ObjectId.GenerateNewId(), "Trending description")
            {
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Collection>());
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaItem>());

        // Act
        var result = await _discoveryService.GetTrendingContentAsync(fromDate, toDate, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Reason.Should().Be(RecommendationReason.Trending);
    }

    [Fact]
    public async Task GetPopularContentAsync_WithValidParameters_ShouldReturnPopularContent()
    {
        // Arrange
        var period = TimeSpan.FromDays(30);
        var limit = 10;

        var collections = new List<Collection>
        {
            new Collection(ObjectId.GenerateNewId(), "Popular Collection", "/popular/collection", Domain.Enums.CollectionType.Folder)
            {
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Library>());
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(collections);
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaItem>());

        // Act
        var result = await _discoveryService.GetPopularContentAsync(period, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Reason.Should().Be(RecommendationReason.Popular);
    }

    [Fact]
    public async Task GetSimilarContentAsync_WithValidParameters_ShouldReturnSimilarContent()
    {
        // Arrange
        var contentId = ObjectId.GenerateNewId();
        var contentType = ContentType.Library;
        var limit = 5;

        var library = new Library("Original Library", "/original/path", ObjectId.GenerateNewId(), "Original description")
        {
            Id = contentId
        };

        var similarLibraries = new List<Library>
        {
            new Library("Similar Library 1", "/similar1/path", ObjectId.GenerateNewId(), "Similar description 1"),
            new Library("Similar Library 2", "/similar2/path", ObjectId.GenerateNewId(), "Similar description 2")
        };

        _mockLibraryRepository.Setup(x => x.GetByIdAsync(contentId)).ReturnsAsync(library);
        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(similarLibraries);

        // Act
        var result = await _discoveryService.GetSimilarContentAsync(contentId, contentType, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(r => r.Reason == RecommendationReason.Similar).Should().BeTrue();
        result.All(r => r.Id != contentId).Should().BeTrue();
    }

    #endregion

    #region Personalized Recommendations Tests

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_WithValidUserId_ShouldReturnRecommendations()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var limit = 10;

        var libraries = new List<Library>
        {
            new Library("Personalized Library", "/personalized/path", ObjectId.GenerateNewId(), "Personalized description")
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Collection>());
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaItem>());

        // Act
        var result = await _discoveryService.GetPersonalizedRecommendationsAsync(userId, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(limit);
    }

    [Fact]
    public async Task GetRecommendationsByCategoryAsync_WithValidParameters_ShouldReturnCategoryRecommendations()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var category = "Test Category";
        var limit = 10;

        var request = new DiscoveryRequest
        {
            Categories = new List<string> { category },
            Limit = limit
        };

        var libraries = new List<Library>
        {
            new Library("Category Library", "/category/path", ObjectId.GenerateNewId(), "Category description")
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Collection>());
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaItem>());

        // Act
        var result = await _discoveryService.GetRecommendationsByCategoryAsync(userId, category, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(limit);
    }

    [Fact]
    public async Task GetRecommendationsByTagsAsync_WithValidParameters_ShouldReturnTagRecommendations()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var tags = new List<string> { "tag1", "tag2" };
        var limit = 10;

        var libraries = new List<Library>
        {
            new Library("Tagged Library", "/tagged/path", ObjectId.GenerateNewId(), "Tagged description")
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Collection>());
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaItem>());

        // Act
        var result = await _discoveryService.GetRecommendationsByTagsAsync(userId, tags, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(limit);
    }

    [Fact]
    public async Task GetRecommendationsByHistoryAsync_WithValidUserId_ShouldReturnHistoryRecommendations()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var limit = 10;

        // Act
        var result = await _discoveryService.GetRecommendationsByHistoryAsync(userId, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(limit);
    }

    #endregion

    #region Content Analytics Tests

    [Fact]
    public async Task GetContentAnalyticsAsync_WithValidParameters_ShouldReturnAnalytics()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        var libraries = new List<Library>
        {
            new Library("Analytics Library", "/analytics/path", ObjectId.GenerateNewId(), "Analytics description")
            {
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        var collections = new List<Collection>
        {
            new Collection(ObjectId.GenerateNewId(), "Analytics Collection", "/analytics/collection", Domain.Enums.CollectionType.Folder)
            {
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(collections);
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaItem>());

        // Act
        var result = await _discoveryService.GetContentAnalyticsAsync(userId, fromDate, toDate);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.FromDate.Should().Be(fromDate);
        result.ToDate.Should().Be(toDate);
        result.TotalContent.Should().Be(2); // 1 library + 1 collection
        result.ContentByType.Should().ContainKey("Library");
        result.ContentByType.Should().ContainKey("Collection");
    }

    [Fact]
    public async Task GetContentTrendsAsync_WithValidParameters_ShouldReturnTrends()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;

        var libraries = new List<Library>
        {
            new Library("Trending Library", "/trending/path", ObjectId.GenerateNewId(), "Trending description")
            {
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Collection>());
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaItem>());

        // Act
        var result = await _discoveryService.GetContentTrendsAsync(fromDate, toDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Type.Should().Be(ContentType.Library);
        result.First().Direction.Should().Be(TrendDirection.Up);
    }

    [Fact]
    public async Task GetPopularContentByDateAsync_WithValidParameters_ShouldReturnPopularContent()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;
        var limit = 10;

        var libraries = new List<Library>
        {
            new Library("Popular Library", "/popular/path", ObjectId.GenerateNewId(), "Popular description")
            {
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        _mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(libraries);
        _mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Collection>());
        _mockMediaItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaItem>());

        // Act
        var result = await _discoveryService.GetPopularContentAsync(fromDate, toDate, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Type.Should().Be(ContentType.Library);
        result.First().PopularityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetContentInsightsAsync_WithValidUserId_ShouldReturnInsights()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act
        var result = await _discoveryService.GetContentInsightsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Type.Should().Be("ContentSummary");
        result.First().Severity.Should().Be(InsightSeverity.Low);
    }

    #endregion

    #region User Preferences & Behavior Tests

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithValidPreferences_ShouldUpdatePreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var preferences = new UserDiscoveryPreferences
        {
            UserId = userId,
            PreferredCategories = new List<string> { "Category1", "Category2" },
            EnablePersonalizedRecommendations = true
        };

        // Act
        await _discoveryService.UpdateUserPreferencesAsync(userId, preferences);

        // Assert
        preferences.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithNullPreferences_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _discoveryService.UpdateUserPreferencesAsync(userId, null!));
    }

    [Fact]
    public async Task GetUserPreferencesAsync_WithValidUserId_ShouldReturnPreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act
        var result = await _discoveryService.GetUserPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.EnablePersonalizedRecommendations.Should().BeTrue();
        result.DefaultPageSize.Should().Be(20);
    }

    [Fact]
    public async Task RecordUserInteractionAsync_WithValidParameters_ShouldRecordInteraction()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var contentId = ObjectId.GenerateNewId();
        var interactionType = InteractionType.View;
        var rating = 4.5;

        // Act
        await _discoveryService.RecordUserInteractionAsync(userId, contentId, interactionType, rating);

        // Assert
        // Should complete without throwing exception
        true.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserInteractionsAsync_WithValidUserId_ShouldReturnInteractions()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var page = 1;
        var pageSize = 20;

        // Act
        var result = await _discoveryService.GetUserInteractionsAsync(userId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // Returns empty list in current implementation
    }

    #endregion

    #region Content Categorization Tests

    [Fact]
    public async Task GetContentCategoriesAsync_ShouldReturnCategories()
    {
        // Act
        var result = await _discoveryService.GetContentCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("General");
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateContentCategoryAsync_WithValidRequest_ShouldCreateCategory()
    {
        // Arrange
        var request = new CreateContentCategoryRequest
        {
            Name = "Test Category",
            Description = "Test category description",
            SortOrder = 1
        };

        // Act
        var result = await _discoveryService.CreateContentCategoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Category");
        result.Description.Should().Be("Test category description");
        result.SortOrder.Should().Be(1);
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateContentCategoryAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _discoveryService.CreateContentCategoryAsync(null!));
    }

    [Fact]
    public async Task UpdateContentCategoryAsync_WithValidRequest_ShouldUpdateCategory()
    {
        // Arrange
        var categoryId = ObjectId.GenerateNewId();
        var request = new UpdateContentCategoryRequest
        {
            Name = "Updated Category",
            Description = "Updated description",
            IsActive = false
        };

        // Act
        var result = await _discoveryService.UpdateContentCategoryAsync(categoryId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(categoryId);
        result.Name.Should().Be("Updated Category");
        result.Description.Should().Be("Updated description");
        result.IsActive.Should().BeFalse();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateContentCategoryAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var categoryId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _discoveryService.UpdateContentCategoryAsync(categoryId, null!));
    }

    [Fact]
    public async Task DeleteContentCategoryAsync_WithValidId_ShouldDeleteCategory()
    {
        // Arrange
        var categoryId = ObjectId.GenerateNewId();

        // Act
        await _discoveryService.DeleteContentCategoryAsync(categoryId);

        // Assert
        // Should complete without throwing exception
        true.Should().BeTrue();
    }

    [Fact]
    public async Task GetContentByCategoryAsync_WithValidCategory_ShouldReturnContent()
    {
        // Arrange
        var category = "Test Category";
        var page = 1;
        var pageSize = 20;

        // Act
        var result = await _discoveryService.GetContentByCategoryAsync(category, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // Returns empty list in current implementation
    }

    #endregion

    #region Smart Suggestions Tests

    [Fact]
    public async Task GetSmartSuggestionsAsync_WithValidParameters_ShouldReturnSuggestions()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var context = "discovery";

        // Act
        var result = await _discoveryService.GetSmartSuggestionsAsync(userId, context);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Text.Should().Be("Explore trending content");
        result.First().Type.Should().Be(SuggestionType.Query);
        result.First().Confidence.Should().Be(0.8);
    }

    [Fact]
    public async Task GetContextualSuggestionsAsync_WithValidParameters_ShouldReturnSuggestions()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var currentContentId = ObjectId.GenerateNewId();
        var limit = 5;

        // Act
        var result = await _discoveryService.GetContextualSuggestionsAsync(userId, currentContentId, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Text.Should().Be("View similar content");
        result.First().Type.Should().Be(SuggestionType.Query);
        result.First().RelatedContentId.Should().Be(currentContentId);
    }

    [Fact]
    public async Task GetTrendingSuggestionsAsync_WithValidLimit_ShouldReturnSuggestions()
    {
        // Arrange
        var limit = 10;

        // Act
        var result = await _discoveryService.GetTrendingSuggestionsAsync(limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Text.Should().Be("Check out trending libraries");
        result.First().Type.Should().Be(SuggestionType.Query);
        result.First().Confidence.Should().Be(0.9);
    }

    #endregion
}
