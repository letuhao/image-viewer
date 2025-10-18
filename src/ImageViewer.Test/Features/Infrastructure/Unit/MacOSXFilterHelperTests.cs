using ImageViewer.Application.Helpers;

namespace ImageViewer.Test.Features.Infrastructure.Unit;

/// <summary>
/// Unit tests for MacOSXFilterHelper
/// Tests comprehensive __MACOSX filtering logic
/// </summary>
public class MacOSXFilterHelperTests
{
    [Theory]
    [InlineData("__MACOSX/image.jpg")]
    [InlineData("__macosx/image.jpg")]
    [InlineData("__MacOSX/image.jpg")]
    [InlineData("folder/__MACOSX/image.jpg")]
    [InlineData("folder\\__MACOSX\\image.jpg")]
    [InlineData("folder/__macosx/subfolder/image.jpg")]
    [InlineData("folder\\__macosx\\subfolder\\image.jpg")]
    [InlineData("__MACOSX_/image.jpg")] // Edge case: __MACOSX with underscore
    [InlineData("__MACOSX")] // Edge case: exact match
    [InlineData("__MACOSX/")] // Edge case: folder path
    [InlineData("prefix__MACOSX/image.jpg")] // Edge case: prefix
    public void IsMacOSXPath_WithMacOSXPaths_ShouldReturnTrue(string path)
    {
        // Act
        var result = MacOSXFilterHelper.IsMacOSXPath(path);

        // Assert
        result.Should().BeTrue($"Path '{path}' should be identified as __MACOSX metadata");
    }

    [Theory]
    [InlineData("image.jpg")]
    [InlineData("folder/image.jpg")]
    [InlineData("folder\\image.jpg")]
    [InlineData("folder/subfolder/image.jpg")]
    [InlineData("folder\\subfolder\\image.jpg")]
    [InlineData("MACOSX/image.jpg")] // Different case
    [InlineData("__MACOSX_OTHER/image.jpg")] // Different suffix
    [InlineData("OTHER__MACOSX/image.jpg")] // Different prefix
    [InlineData("__MACOSXOTHER/image.jpg")] // No slash separator
    [InlineData("folder__MACOSX/image.jpg")] // No slash before __MACOSX
    [InlineData("")] // Empty string
    [InlineData(null)] // Null string
    public void IsMacOSXPath_WithValidPaths_ShouldReturnFalse(string? path)
    {
        // Act
        var result = MacOSXFilterHelper.IsMacOSXPath(path);

        // Assert
        result.Should().BeFalse($"Path '{path}' should NOT be identified as __MACOSX metadata");
    }

    [Fact]
    public void IsSafeToProcess_WithMacOSXPath_ShouldReturnFalse()
    {
        // Arrange
        var macosxPath = "__MACOSX/image.jpg";

        // Act
        var result = MacOSXFilterHelper.IsSafeToProcess(macosxPath, "test operation");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSafeToProcess_WithValidPath_ShouldReturnTrue()
    {
        // Arrange
        var validPath = "folder/image.jpg";

        // Act
        var result = MacOSXFilterHelper.IsSafeToProcess(validPath, "test operation");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSafeToProcess_WithNullPath_ShouldReturnFalse()
    {
        // Act
        var result = MacOSXFilterHelper.IsSafeToProcess(null, "test operation");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FilterMacOSXEntries_WithMixedEntries_ShouldFilterCorrectly()
    {
        // Arrange
        var entries = new[]
        {
            "image1.jpg",
            "__MACOSX/image2.jpg",
            "folder/image3.jpg",
            "__macosx/image4.jpg",
            "normal/image5.jpg"
        };

        // Act
        var filtered = MacOSXFilterHelper.FilterMacOSXEntries(entries, path => path).ToList();

        // Assert
        filtered.Should().HaveCount(3);
        filtered.Should().Contain("image1.jpg");
        filtered.Should().Contain("folder/image3.jpg");
        filtered.Should().Contain("normal/image5.jpg");
        filtered.Should().NotContain("__MACOSX/image2.jpg");
        filtered.Should().NotContain("__macosx/image4.jpg");
    }

    [Fact]
    public void IsMacOSXEntry_WithVariousFormats_ShouldWorkConsistently()
    {
        // Test that both SharpCompress (entry.Key) and ZipFile (entry.FullName) formats work the same
        
        // Arrange
        var sharpCompressPath = "__MACOSX/image.jpg"; // entry.Key format
        var zipFilePath = "__MACOSX\\image.jpg"; // entry.FullName format (Windows)

        // Act
        var sharpCompressResult = MacOSXFilterHelper.IsMacOSXEntry(sharpCompressPath);
        var zipFileResult = MacOSXFilterHelper.IsMacOSXEntry(zipFilePath);

        // Assert
        sharpCompressResult.Should().BeTrue();
        zipFileResult.Should().BeTrue();
    }

    [Theory]
    [InlineData("__MACOSX", true)]
    [InlineData("__macosx", true)]
    [InlineData("__MacOSX", true)]
    [InlineData("__MACOSX/", true)]
    [InlineData("__MACOSX\\", true)]
    [InlineData("folder/__MACOSX/image.jpg", true)]
    [InlineData("folder\\__MACOSX\\image.jpg", true)]
    [InlineData("image.jpg", false)]
    [InlineData("folder/image.jpg", false)]
    [InlineData("MACOSX/image.jpg", false)]
    [InlineData("__MACOSXOTHER/image.jpg", false)]
    public void IsMacOSXPath_ComprehensiveTest_ShouldWorkCorrectly(string path, bool expected)
    {
        // Act
        var result = MacOSXFilterHelper.IsMacOSXPath(path);

        // Assert
        result.Should().Be(expected, $"Path '{path}' should {(expected ? "" : "NOT ")}be identified as __MACOSX metadata");
    }

    [Fact]
    public void FilterMacOSXEntries_WithEmptyCollection_ShouldReturnEmpty()
    {
        // Arrange
        var entries = new string[0];

        // Act
        var filtered = MacOSXFilterHelper.FilterMacOSXEntries(entries, path => path).ToList();

        // Assert
        filtered.Should().BeEmpty();
    }

    [Fact]
    public void FilterMacOSXEntries_WithAllMacOSXEntries_ShouldReturnEmpty()
    {
        // Arrange
        var entries = new[]
        {
            "__MACOSX/image1.jpg",
            "__macosx/image2.jpg",
            "__MACOSX/folder/image3.jpg"
        };

        // Act
        var filtered = MacOSXFilterHelper.FilterMacOSXEntries(entries, path => path).ToList();

        // Assert
        filtered.Should().BeEmpty();
    }

    [Fact]
    public void FilterMacOSXEntries_WithNoMacOSXEntries_ShouldReturnAll()
    {
        // Arrange
        var entries = new[]
        {
            "image1.jpg",
            "folder/image2.jpg",
            "folder/subfolder/image3.jpg"
        };

        // Act
        var filtered = MacOSXFilterHelper.FilterMacOSXEntries(entries, path => path).ToList();

        // Assert
        filtered.Should().HaveCount(3);
        filtered.Should().Contain("image1.jpg");
        filtered.Should().Contain("folder/image2.jpg");
        filtered.Should().Contain("folder/subfolder/image3.jpg");
    }
}
