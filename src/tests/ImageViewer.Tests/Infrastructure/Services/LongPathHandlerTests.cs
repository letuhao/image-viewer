using FluentAssertions;
using ImageViewer.Infrastructure.Services;

namespace ImageViewer.Tests.Infrastructure.Services;

public class LongPathHandlerTests
{
    [Fact]
    public void PathExistsSafe_WithNullPath_ShouldReturnFalse()
    {
        // Act
        var result = LongPathHandler.PathExistsSafe(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PathExistsSafe_WithEmptyPath_ShouldReturnFalse()
    {
        // Act
        var result = LongPathHandler.PathExistsSafe(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PathExistsSafe_WithNonExistentPath_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Path\\That\\Does\\Not\\Exist";

        // Act
        var result = LongPathHandler.PathExistsSafe(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PathExistsSafe_WithVeryLongPath_ShouldReturnFalse()
    {
        // Arrange
        var veryLongPath = "C:\\" + new string('A', 300) + "\\Path";

        // Act
        var result = LongPathHandler.PathExistsSafe(veryLongPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PathExistsSafe_WithInvalidCharacters_ShouldReturnFalse()
    {
        // Arrange
        var invalidPath = "C:\\Test\\Path<>|*?";

        // Act
        var result = LongPathHandler.PathExistsSafe(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PathExistsSafe_WithWhitespacePath_ShouldReturnFalse()
    {
        // Arrange
        var whitespacePath = "   ";

        // Act
        var result = LongPathHandler.PathExistsSafe(whitespacePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PathExistsSafe_WithRootPath_ShouldReturnTrue()
    {
        // Arrange
        var rootPath = "C:\\";

        // Act
        var result = LongPathHandler.PathExistsSafe(rootPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithCurrentDirectory_ShouldReturnTrue()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();

        // Act
        var result = LongPathHandler.PathExistsSafe(currentDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithTempDirectory_ShouldReturnTrue()
    {
        // Arrange
        var tempDir = Path.GetTempPath();

        // Act
        var result = LongPathHandler.PathExistsSafe(tempDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithSystemDirectory_ShouldReturnTrue()
    {
        // Arrange
        var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);

        // Act
        var result = LongPathHandler.PathExistsSafe(systemDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithProgramFilesDirectory_ShouldReturnTrue()
    {
        // Arrange
        var programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        // Act
        var result = LongPathHandler.PathExistsSafe(programFilesDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithUserProfileDirectory_ShouldReturnTrue()
    {
        // Arrange
        var userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var result = LongPathHandler.PathExistsSafe(userProfileDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithDesktopDirectory_ShouldReturnTrue()
    {
        // Arrange
        var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // Act
        var result = LongPathHandler.PathExistsSafe(desktopDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithDocumentsDirectory_ShouldReturnTrue()
    {
        // Arrange
        var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Act
        var result = LongPathHandler.PathExistsSafe(documentsDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithPicturesDirectory_ShouldReturnTrue()
    {
        // Arrange
        var picturesDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        // Act
        var result = LongPathHandler.PathExistsSafe(picturesDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithMusicDirectory_ShouldReturnTrue()
    {
        // Arrange
        var musicDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        // Act
        var result = LongPathHandler.PathExistsSafe(musicDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PathExistsSafe_WithVideosDirectory_ShouldReturnTrue()
    {
        // Arrange
        var videosDir = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        // Act
        var result = LongPathHandler.PathExistsSafe(videosDir);

        // Assert
        result.Should().BeTrue();
    }
}
