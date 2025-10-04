using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using ImageViewer.Domain.Entities;
using ImageViewer.Infrastructure.Services;
using MongoDB.Bson;

namespace ImageViewer.UnitTests.Services;

/// <summary>
/// Unit tests for JwtService
/// </summary>
public class JwtServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        var configData = new Dictionary<string, string>
        {
            ["Jwt:Key"] = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpiryHours"] = "24"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _jwtService = new JwtService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "hashedpassword", "User");

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT tokens have dots
    }

    [Fact]
    public void GenerateAccessToken_NullUser_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _jwtService.GenerateAccessToken(null!));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsToken()
    {
        // Act
        var token = _jwtService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "hashedpassword", "User");
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.True(principal.Identity!.IsAuthenticated);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_EmptyToken_ReturnsNull()
    {
        // Act
        var principal = _jwtService.ValidateToken(string.Empty);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void IsTokenExpired_ValidToken_ReturnsFalse()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "hashedpassword", "User");
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var isExpired = _jwtService.IsTokenExpired(token);

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void IsTokenExpired_InvalidToken_ReturnsTrue()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var isExpired = _jwtService.IsTokenExpired(invalidToken);

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void GetUserIdFromToken_ValidToken_ReturnsUserId()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "hashedpassword", "User");
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var userId = _jwtService.GetUserIdFromToken(token);

        // Assert
        Assert.Equal(user.Id.ToString(), userId);
    }

    [Fact]
    public void GetUserIdFromToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var userId = _jwtService.GetUserIdFromToken(invalidToken);

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public void GetUsernameFromToken_ValidToken_ReturnsUsername()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "hashedpassword", "User");
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var username = _jwtService.GetUsernameFromToken(token);

        // Assert
        Assert.Equal("testuser", username);
    }

    [Fact]
    public void GetRoleFromToken_ValidToken_ReturnsRole()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "hashedpassword", "Admin");
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var role = _jwtService.GetRoleFromToken(token);

        // Assert
        Assert.Equal("Admin", role);
    }
}
