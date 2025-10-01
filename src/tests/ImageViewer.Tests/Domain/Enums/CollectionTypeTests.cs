using ImageViewer.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ImageViewer.Tests.Domain.Enums;

public class CollectionTypeTests
{
    [Theory]
    [InlineData(CollectionType.Folder, 0)]
    [InlineData(CollectionType.Zip, 1)]
    [InlineData(CollectionType.SevenZip, 2)]
    [InlineData(CollectionType.Rar, 3)]
    [InlineData(CollectionType.Tar, 4)]
    public void CollectionType_ShouldHaveCorrectValues(CollectionType collectionType, int expectedValue)
    {
        // Act
        var actualValue = (int)collectionType;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void CollectionType_ShouldHaveAllExpectedValues()
    {
        // Arrange
        var expectedValues = new[] { 0, 1, 2, 3, 4 };
        var actualValues = Enum.GetValues<CollectionType>().Select(x => (int)x).ToArray();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Fact]
    public void CollectionType_ShouldHaveCorrectCount()
    {
        // Act
        var count = Enum.GetValues<CollectionType>().Length;

        // Assert
        count.Should().Be(5);
    }

    [Theory]
    [InlineData(CollectionType.Folder, "Folder")]
    [InlineData(CollectionType.Zip, "Zip")]
    [InlineData(CollectionType.SevenZip, "SevenZip")]
    [InlineData(CollectionType.Rar, "Rar")]
    [InlineData(CollectionType.Tar, "Tar")]
    public void CollectionType_ShouldHaveCorrectStringRepresentation(CollectionType collectionType, string expectedString)
    {
        // Act
        var actualString = collectionType.ToString();

        // Assert
        actualString.Should().Be(expectedString);
    }

    [Fact]
    public void CollectionType_ShouldBeParseableFromString()
    {
        // Arrange
        var stringValues = new[] { "Folder", "Zip", "SevenZip", "Rar", "Tar" };
        var expectedValues = new[] { CollectionType.Folder, CollectionType.Zip, CollectionType.SevenZip, CollectionType.Rar, CollectionType.Tar };

        // Act & Assert
        for (int i = 0; i < stringValues.Length; i++)
        {
            var parsed = Enum.Parse<CollectionType>(stringValues[i]);
            parsed.Should().Be(expectedValues[i]);
        }
    }

    [Fact]
    public void CollectionType_ShouldBeParseableFromInt()
    {
        // Arrange
        var intValues = new[] { 0, 1, 2, 3, 4 };
        var expectedValues = new[] { CollectionType.Folder, CollectionType.Zip, CollectionType.SevenZip, CollectionType.Rar, CollectionType.Tar };

        // Act & Assert
        for (int i = 0; i < intValues.Length; i++)
        {
            var parsed = (CollectionType)intValues[i];
            parsed.Should().Be(expectedValues[i]);
        }
    }
}
