using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ImageViewer.Infrastructure.Services;

namespace ImageViewer.UnitTests.Services;

/// <summary>
/// Unit tests for PasswordService
/// </summary>
public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHash()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
        Assert.True(hash.Length > 50); // BCrypt hashes are typically 60 characters
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(null!));
    }

    [Fact]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(string.Empty));
    }

    [Fact]
    public void VerifyPassword_ValidPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStrongPassword_StrongPassword_ReturnsTrue()
    {
        // Arrange
        var strongPassword = "StrongPass123!";

        // Act
        var result = _passwordService.IsStrongPassword(strongPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsStrongPassword_WeakPassword_ReturnsFalse()
    {
        // Arrange
        var weakPasswords = new[]
        {
            "weak",           // Too short
            "weakpassword",   // No uppercase, numbers, or special chars
            "WEAKPASSWORD",   // No lowercase, numbers, or special chars
            "WeakPassword",   // No numbers or special chars
            "WeakPass123",    // No special chars
            "WeakPass!"       // No numbers
        };

        foreach (var password in weakPasswords)
        {
            // Act
            var result = _passwordService.IsStrongPassword(password);

            // Assert
            Assert.False(result);
        }
    }

    [Fact]
    public void GetPasswordStrengthScore_StrongPassword_ReturnsHighScore()
    {
        // Arrange
        var strongPassword = "VeryStrongPassword123!@#";

        // Act
        var score = _passwordService.GetPasswordStrengthScore(strongPassword);

        // Assert
        Assert.True(score >= 80);
    }

    [Fact]
    public void GetPasswordStrengthScore_WeakPassword_ReturnsLowScore()
    {
        // Arrange
        var weakPassword = "weak";

        // Act
        var score = _passwordService.GetPasswordStrengthScore(weakPassword);

        // Assert
        Assert.True(score < 50);
    }

    [Fact]
    public void GenerateRandomPassword_DefaultLength_ReturnsValidPassword()
    {
        // Act
        var password = _passwordService.GenerateRandomPassword();

        // Assert
        Assert.NotNull(password);
        Assert.Equal(12, password.Length);
        Assert.True(_passwordService.IsStrongPassword(password));
    }

    [Fact]
    public void GenerateRandomPassword_CustomLength_ReturnsValidPassword()
    {
        // Arrange
        var length = 16;

        // Act
        var password = _passwordService.GenerateRandomPassword(length);

        // Assert
        Assert.NotNull(password);
        Assert.Equal(length, password.Length);
        Assert.True(_passwordService.IsStrongPassword(password));
    }

    [Fact]
    public void GenerateRandomPassword_WithoutSpecialChars_ReturnsValidPassword()
    {
        // Act
        var password = _passwordService.GenerateRandomPassword(12, false);

        // Assert
        Assert.NotNull(password);
        Assert.Equal(12, password.Length);
        // Should still be strong even without special characters
        Assert.True(_passwordService.GetPasswordStrengthScore(password) >= 60);
    }
}
