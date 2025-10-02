using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Infrastructure.Services;
using System.Security.Claims;

namespace ImageViewer.Tests.Infrastructure.Services;

public class UserContextServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly UserContextService _service;

    public UserContextServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _service = new UserContextService(_httpContextAccessorMock.Object);
    }

    [Fact]
    public void UserContextService_ShouldBeCreated()
    {
        // Arrange & Act
        var service = new UserContextService(_httpContextAccessorMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new UserContextService(null!));
        
        exception.ParamName.Should().Be("httpContextAccessor");
    }

    [Fact]
    public void GetCurrentUserId_WithNullHttpContext_ShouldReturnAnonymous()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var userId = _service.GetCurrentUserId();

        // Assert
        userId.Should().Be("anonymous");
    }

    [Fact]
    public void GetCurrentUserId_WithNullUser_ShouldReturnAnonymous()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var userId = _service.GetCurrentUserId();

        // Assert
        userId.Should().Be("anonymous");
    }

    [Fact]
    public void GetCurrentUserId_WithNoClaims_ShouldReturnAnonymous()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var claimsPrincipal = new ClaimsPrincipal();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var userId = _service.GetCurrentUserId();

        // Assert
        userId.Should().Be("anonymous");
    }

    [Fact]
    public void GetCurrentUserId_WithValidUserIdClaim_ShouldReturnUserId()
    {
        // Arrange
        var userId = "test-user-id";
        var httpContextMock = new Mock<HttpContext>();
        var claims = new List<Claim>
        {
            new Claim("user_id", userId)
        };
        var claimsIdentity = new ClaimsIdentity(claims, "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithNameIdentifierClaim_ShouldReturnUserId()
    {
        // Arrange
        var userId = "test-user-id";
        var httpContextMock = new Mock<HttpContext>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var claimsIdentity = new ClaimsIdentity(claims, "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithSubClaim_ShouldReturnUserId()
    {
        // Arrange
        var userId = "test-user-id";
        var httpContextMock = new Mock<HttpContext>();
        var claims = new List<Claim>
        {
            new Claim("sub", userId)
        };
        var claimsIdentity = new ClaimsIdentity(claims, "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserName_WithValidUserNameClaim_ShouldReturnUserName()
    {
        // Arrange
        var userName = "test-user";
        var httpContextMock = new Mock<HttpContext>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userName)
        };
        var claimsIdentity = new ClaimsIdentity(claims, "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result = _service.GetCurrentUserName();

        // Assert
        result.Should().Be(userName);
    }

    [Fact]
    public void GetCurrentUserName_WithNullHttpContext_ShouldReturnAnonymous()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var userName = _service.GetCurrentUserName();

        // Assert
        userName.Should().Be("Anonymous User");
    }
}
