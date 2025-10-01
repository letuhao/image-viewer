using FluentAssertions;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Tests.Common;

namespace ImageViewer.Tests.Domain.ValueObjects;

public class CollectionSettingsTests : TestBase
{
    [Fact]
    public void Constructor_WithDefaultParameters_ShouldCreateWithDefaults()
    {
        // Act
        var settings = new CollectionSettings();

        // Assert
        settings.TotalImages.Should().Be(0);
        settings.TotalSizeBytes.Should().Be(0);
        settings.ThumbnailWidth.Should().Be(300);
        settings.ThumbnailHeight.Should().Be(300);
        settings.CacheWidth.Should().Be(1920);
        settings.CacheHeight.Should().Be(1080);
        settings.AutoGenerateThumbnails.Should().BeTrue();
        settings.AutoGenerateCache.Should().BeTrue();
        settings.CacheExpiration.Should().Be(TimeSpan.FromDays(30));
        settings.AdditionalSettingsJson.Should().NotBeNull();
        settings.AdditionalSettingsJson.Should().Be("{}");
    }

    [Fact]
    public void Constructor_WithCustomParameters_ShouldCreateWithCustomValues()
    {
        // Arrange
        var totalImages = 100;
        var totalSizeBytes = 1024L * 1024 * 50; // 50MB
        var thumbnailWidth = 200;
        var thumbnailHeight = 200;
        var cacheWidth = 1280;
        var cacheHeight = 720;
        var autoGenerateThumbnails = false;
        var autoGenerateCache = false;
        var cacheExpiration = TimeSpan.FromDays(7);
        var additionalSettings = new Dictionary<string, object> { { "test", "value" } };

        // Act
        var settings = new CollectionSettings(
            totalImages,
            totalSizeBytes,
            thumbnailWidth,
            thumbnailHeight,
            cacheWidth,
            cacheHeight,
            autoGenerateThumbnails,
            autoGenerateCache,
            cacheExpiration,
            additionalSettings
        );

        // Assert
        settings.TotalImages.Should().Be(totalImages);
        settings.TotalSizeBytes.Should().Be(totalSizeBytes);
        settings.ThumbnailWidth.Should().Be(thumbnailWidth);
        settings.ThumbnailHeight.Should().Be(thumbnailHeight);
        settings.CacheWidth.Should().Be(cacheWidth);
        settings.CacheHeight.Should().Be(cacheHeight);
        settings.AutoGenerateThumbnails.Should().Be(autoGenerateThumbnails);
        settings.AutoGenerateCache.Should().Be(autoGenerateCache);
        settings.CacheExpiration.Should().Be(cacheExpiration);
        var deserializedSettings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(settings.AdditionalSettingsJson);
        deserializedSettings.Should().ContainKey("test");
        deserializedSettings["test"].Should().BeOfType<System.Text.Json.JsonElement>();
        ((System.Text.Json.JsonElement)deserializedSettings["test"]).GetString().Should().Be("value");
    }

    [Fact]
    public void UpdateTotalImages_WithValidCount_ShouldUpdateCount()
    {
        // Arrange
        var settings = new CollectionSettings();
        var newCount = 150;

        // Act
        settings.UpdateTotalImages(newCount);

        // Assert
        settings.TotalImages.Should().Be(newCount);
    }

    [Fact]
    public void UpdateTotalImages_WithNegativeCount_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.UpdateTotalImages(-1);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("totalImages");
    }

    [Fact]
    public void UpdateTotalSize_WithValidSize_ShouldUpdateSize()
    {
        // Arrange
        var settings = new CollectionSettings();
        var newSize = 1024L * 1024 * 100; // 100MB

        // Act
        settings.UpdateTotalSize(newSize);

        // Assert
        settings.TotalSizeBytes.Should().Be(newSize);
    }

    [Fact]
    public void UpdateTotalSize_WithNegativeSize_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.UpdateTotalSize(-1);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("totalSizeBytes");
    }

    [Fact]
    public void UpdateThumbnailSize_WithValidDimensions_ShouldUpdateDimensions()
    {
        // Arrange
        var settings = new CollectionSettings();
        var width = 250;
        var height = 250;

        // Act
        settings.UpdateThumbnailSize(width, height);

        // Assert
        settings.ThumbnailWidth.Should().Be(width);
        settings.ThumbnailHeight.Should().Be(height);
    }

    [Fact]
    public void UpdateThumbnailSize_WithZeroWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.UpdateThumbnailSize(0, 100);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("width");
    }

    [Fact]
    public void UpdateThumbnailSize_WithZeroHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.UpdateThumbnailSize(100, 0);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("height");
    }

    [Fact]
    public void UpdateCacheSize_WithValidDimensions_ShouldUpdateDimensions()
    {
        // Arrange
        var settings = new CollectionSettings();
        var width = 1600;
        var height = 900;

        // Act
        settings.UpdateCacheSize(width, height);

        // Assert
        settings.CacheWidth.Should().Be(width);
        settings.CacheHeight.Should().Be(height);
    }

    [Fact]
    public void UpdateCacheSize_WithZeroWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.UpdateCacheSize(0, 100);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("width");
    }

    [Fact]
    public void UpdateCacheSize_WithZeroHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.UpdateCacheSize(100, 0);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("height");
    }

    [Fact]
    public void SetAutoGenerateThumbnails_ShouldUpdateSetting()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act
        settings.SetAutoGenerateThumbnails(false);

        // Assert
        settings.AutoGenerateThumbnails.Should().BeFalse();
    }

    [Fact]
    public void SetAutoGenerateCache_ShouldUpdateSetting()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act
        settings.SetAutoGenerateCache(false);

        // Assert
        settings.AutoGenerateCache.Should().BeFalse();
    }

    [Fact]
    public void UpdateCacheExpiration_WithValidExpiration_ShouldUpdateExpiration()
    {
        // Arrange
        var settings = new CollectionSettings();
        var expiration = TimeSpan.FromDays(14);

        // Act
        settings.UpdateCacheExpiration(expiration);

        // Assert
        settings.CacheExpiration.Should().Be(expiration);
    }

    [Fact]
    public void UpdateCacheExpiration_WithZeroExpiration_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.UpdateCacheExpiration(TimeSpan.Zero);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("expiration");
    }

    [Fact]
    public void SetAdditionalSetting_WithValidKeyValue_ShouldAddSetting()
    {
        // Arrange
        var settings = new CollectionSettings();
        var key = "testKey";
        var value = "testValue";

        // Act
        settings.SetAdditionalSetting(key, value);

        // Assert
        var deserializedSettings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(settings.AdditionalSettingsJson);
        deserializedSettings.Should().ContainKey(key);
        deserializedSettings[key].Should().BeOfType<System.Text.Json.JsonElement>();
        ((System.Text.Json.JsonElement)deserializedSettings[key]).GetString().Should().Be(value);
    }

    [Fact]
    public void SetAdditionalSetting_WithNullKey_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.SetAdditionalSetting(null!, "value");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public void SetAdditionalSetting_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act & Assert
        var action = () => settings.SetAdditionalSetting("", "value");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public void GetAdditionalSetting_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var settings = new CollectionSettings();
        var key = "testKey";
        var value = "testValue";
        settings.SetAdditionalSetting(key, value);

        // Act
        var result = settings.GetAdditionalSetting<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void GetAdditionalSetting_WithNonExistingKey_ShouldReturnDefault()
    {
        // Arrange
        var settings = new CollectionSettings();

        // Act
        var result = settings.GetAdditionalSetting<string>("nonExistingKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToJson_ShouldReturnValidJson()
    {
        // Arrange
        var settings = new CollectionSettings();
        settings.SetAdditionalSetting("test", "value");

        // Act
        var json = settings.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("totalImages");
        json.Should().Contain("thumbnailWidth");
        json.Should().Contain("test");
    }

    [Fact]
    public void FromJson_WithValidJson_ShouldCreateSettings()
    {
        // Arrange
        var originalSettings = new CollectionSettings();
        var json = originalSettings.ToJson();

        // Act
        var settings = CollectionSettings.FromJson(json);

        // Assert
        settings.Should().NotBeNull();
        settings.TotalImages.Should().Be(originalSettings.TotalImages);
        settings.ThumbnailWidth.Should().Be(originalSettings.ThumbnailWidth);
        settings.CacheWidth.Should().Be(originalSettings.CacheWidth);
        settings.CacheHeight.Should().Be(originalSettings.CacheHeight);
        settings.AutoGenerateThumbnails.Should().Be(originalSettings.AutoGenerateThumbnails);
        settings.AutoGenerateCache.Should().Be(originalSettings.AutoGenerateCache);
        settings.CacheExpiration.Should().Be(originalSettings.CacheExpiration);
        settings.AdditionalSettingsJson.Should().Be(originalSettings.AdditionalSettingsJson);
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        var invalidJson = "invalid json";

        // Act & Assert
        var action = () => CollectionSettings.FromJson(invalidJson);
        action.Should().Throw<System.Text.Json.JsonException>();
    }
}
