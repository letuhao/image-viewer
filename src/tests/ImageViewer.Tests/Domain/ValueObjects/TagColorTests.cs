using ImageViewer.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ImageViewer.Tests.Domain.ValueObjects;

public class TagColorTests
{
    [Fact]
    public void Constructor_WithRgbValues_ShouldCreateTagColor()
    {
        // Arrange
        byte r = 255, g = 128, b = 64;
        string name = "Custom Color";

        // Act
        var color = new TagColor(r, g, b, name);

        // Assert
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
        color.Hex.Should().Be("#FF8040");
        color.Name.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithRgbValuesAndEmptyName_ShouldUseDefaultName()
    {
        // Arrange
        byte r = 255, g = 128, b = 64;

        // Act
        var color = new TagColor(r, g, b);

        // Assert
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
        color.Hex.Should().Be("#FF8040");
        color.Name.Should().Be($"RGB({r},{g},{b})");
    }

    [Fact]
    public void Constructor_WithHexAndName_ShouldCreateTagColor()
    {
        // Arrange
        string hex = "#FF8040";
        string name = "Custom Color";

        // Act
        var color = new TagColor(hex, name);

        // Assert
        color.R.Should().Be(255);
        color.G.Should().Be(128);
        color.B.Should().Be(64);
        color.Hex.Should().Be("#FF8040");
        color.Name.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithHexAndName_ShouldConvertToUpperCase()
    {
        // Arrange
        string hex = "#ff8040";
        string name = "Custom Color";

        // Act
        var color = new TagColor(hex, name);

        // Assert
        color.Hex.Should().Be("#FF8040");
    }

    [Fact]
    public void Constructor_WithNullHex_ShouldThrowArgumentException()
    {
        // Arrange
        string hex = null!;
        string name = "Custom Color";

        // Act & Assert
        var action = () => new TagColor(hex, name);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Hex color cannot be null or empty*")
            .WithParameterName("hex");
    }

    [Fact]
    public void Constructor_WithEmptyHex_ShouldThrowArgumentException()
    {
        // Arrange
        string hex = "";
        string name = "Custom Color";

        // Act & Assert
        var action = () => new TagColor(hex, name);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Hex color cannot be null or empty*")
            .WithParameterName("hex");
    }

    [Fact]
    public void Constructor_WithWhitespaceHex_ShouldThrowArgumentException()
    {
        // Arrange
        string hex = "   ";
        string name = "Custom Color";

        // Act & Assert
        var action = () => new TagColor(hex, name);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Hex color cannot be null or empty*")
            .WithParameterName("hex");
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        string hex = "#FF8040";
        string name = null!;

        // Act & Assert
        var action = () => new TagColor(hex, name);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Color name cannot be null or empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        string hex = "#FF8040";
        string name = "";

        // Act & Assert
        var action = () => new TagColor(hex, name);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Color name cannot be null or empty*")
            .WithParameterName("name");
    }

    [Theory]
    [InlineData("#GGGGGG")]
    [InlineData("#12345")]
    [InlineData("123456")]
    [InlineData("#1234567")]
    [InlineData("#")]
    public void Constructor_WithInvalidHexFormat_ShouldThrowArgumentException(string invalidHex)
    {
        // Arrange
        string name = "Custom Color";

        // Act & Assert
        var action = () => new TagColor(invalidHex, name);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid hex color format*")
            .WithParameterName("hex");
    }

    [Fact]
    public void Default_ShouldReturnBlueColor()
    {
        // Act
        var defaultColor = TagColor.Default;

        // Assert
        defaultColor.Hex.Should().Be("#3B82F6");
        defaultColor.Name.Should().Be("Blue");
    }

    [Fact]
    public void Red_ShouldReturnRedColor()
    {
        // Act
        var redColor = TagColor.Red;

        // Assert
        redColor.Hex.Should().Be("#EF4444");
        redColor.Name.Should().Be("Red");
    }

    [Fact]
    public void Green_ShouldReturnGreenColor()
    {
        // Act
        var greenColor = TagColor.Green;

        // Assert
        greenColor.Hex.Should().Be("#10B981");
        greenColor.Name.Should().Be("Green");
    }

    [Fact]
    public void Yellow_ShouldReturnYellowColor()
    {
        // Act
        var yellowColor = TagColor.Yellow;

        // Assert
        yellowColor.Hex.Should().Be("#F59E0B");
        yellowColor.Name.Should().Be("Yellow");
    }

    [Fact]
    public void Purple_ShouldReturnPurpleColor()
    {
        // Act
        var purpleColor = TagColor.Purple;

        // Assert
        purpleColor.Hex.Should().Be("#8B5CF6");
        purpleColor.Name.Should().Be("Purple");
    }

    [Fact]
    public void Pink_ShouldReturnPinkColor()
    {
        // Act
        var pinkColor = TagColor.Pink;

        // Assert
        pinkColor.Hex.Should().Be("#EC4899");
        pinkColor.Name.Should().Be("Pink");
    }

    [Fact]
    public void Orange_ShouldReturnOrangeColor()
    {
        // Act
        var orangeColor = TagColor.Orange;

        // Assert
        orangeColor.Hex.Should().Be("#F97316");
        orangeColor.Name.Should().Be("Orange");
    }

    [Fact]
    public void Gray_ShouldReturnGrayColor()
    {
        // Act
        var grayColor = TagColor.Gray;

        // Assert
        grayColor.Hex.Should().Be("#6B7280");
        grayColor.Name.Should().Be("Gray");
    }

    [Fact]
    public void AllColors_ShouldReturnAllPredefinedColors()
    {
        // Act
        var allColors = TagColor.AllColors;

        // Assert
        allColors.Should().HaveCount(8);
        allColors.Should().Contain(TagColor.Default);
        allColors.Should().Contain(TagColor.Red);
        allColors.Should().Contain(TagColor.Green);
        allColors.Should().Contain(TagColor.Yellow);
        allColors.Should().Contain(TagColor.Purple);
        allColors.Should().Contain(TagColor.Pink);
        allColors.Should().Contain(TagColor.Orange);
        allColors.Should().Contain(TagColor.Gray);
    }

    [Fact]
    public void Equals_WithSameColor_ShouldReturnTrue()
    {
        // Arrange
        var color1 = new TagColor("#FF8040", "Custom Color");
        var color2 = new TagColor("#FF8040", "Custom Color");

        // Act
        var result = color1.Equals(color2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentHex_ShouldReturnFalse()
    {
        // Arrange
        var color1 = new TagColor("#FF8040", "Custom Color");
        var color2 = new TagColor("#FF8041", "Custom Color");

        // Act
        var result = color1.Equals(color2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentName_ShouldReturnFalse()
    {
        // Arrange
        var color1 = new TagColor("#FF8040", "Custom Color");
        var color2 = new TagColor("#FF8040", "Different Name");

        // Act
        var result = color1.Equals(color2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var color = new TagColor("#FF8040", "Custom Color");

        // Act
        var result = color.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNonTagColor_ShouldReturnFalse()
    {
        // Arrange
        var color = new TagColor("#FF8040", "Custom Color");
        var other = "Not a TagColor";

        // Act
        var result = color.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameColor_ShouldReturnSameHashCode()
    {
        // Arrange
        var color1 = new TagColor("#FF8040", "Custom Color");
        var color2 = new TagColor("#FF8040", "Custom Color");

        // Act
        var hashCode1 = color1.GetHashCode();
        var hashCode2 = color2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentColor_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var color1 = new TagColor("#FF8040", "Custom Color");
        var color2 = new TagColor("#FF8041", "Custom Color");

        // Act
        var hashCode1 = color1.GetHashCode();
        var hashCode2 = color2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var color = new TagColor("#FF8040", "Custom Color");

        // Act
        var result = color.ToString();

        // Assert
        result.Should().Be("Custom Color (#FF8040)");
    }

    [Fact]
    public void ToString_WithDefaultName_ShouldReturnFormattedString()
    {
        // Arrange
        var color = new TagColor(255, 128, 64);

        // Act
        var result = color.ToString();

        // Assert
        result.Should().Be("RGB(255,128,64) (#FF8040)");
    }
}
