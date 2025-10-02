using FluentAssertions;
using ImageViewer.Api.Controllers;
using ImageViewer.Application.Services;
using ImageViewer.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ImageViewer.Tests.Api.Controllers;

public class AuthControllerTests : TestBase
{
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _jwtServiceMock = CreateMock<IJwtService>();
        _loggerMock = CreateMock<ILogger<AuthController>>();
        _controller = new AuthController(_jwtServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkResult()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "testpass"
        };

        var expectedToken = "mock-jwt-token";
        _jwtServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<string>(), request.Username, It.IsAny<string[]>()))
            .Returns(expectedToken);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<ActionResult<LoginResponseDto>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var response = okResult!.Value as LoginResponseDto;
        response.Should().NotBeNull();
        response!.Token.Should().Be(expectedToken);
        response.Username.Should().Be(request.Username);
        response.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task Login_WithEmptyUsername_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "",
            Password = "testpass"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<ActionResult<LoginResponseDto>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = ""
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<ActionResult<LoginResponseDto>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_WhenJwtServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "testpass"
        };

        _jwtServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Throws(new Exception("JWT generation failed"));

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<ActionResult<LoginResponseDto>>();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public void GetCurrentUser_WithValidClaims_ShouldReturnOkResult()
    {
        // Arrange
        var userId = "test-user-id";
        var username = "testuser";
        var roles = new[] { "User" };

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId),
            new(System.Security.Claims.ClaimTypes.Name, username),
            new(System.Security.Claims.ClaimTypes.Role, string.Join(",", roles))
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        result.Should().BeOfType<ActionResult<UserInfoDto>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var userInfo = okResult!.Value as UserInfoDto;
        userInfo.Should().NotBeNull();
        userInfo!.UserId.Should().Be(userId);
        userInfo.Username.Should().Be(username);
        userInfo.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void GetCurrentUser_WithoutClaims_ShouldReturnOkResultWithEmptyValues()
    {
        // Arrange
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal()
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        result.Should().BeOfType<ActionResult<UserInfoDto>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var userInfo = okResult!.Value as UserInfoDto;
        userInfo.Should().NotBeNull();
        userInfo!.UserId.Should().Be("");
        userInfo.Username.Should().Be("");
        userInfo.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Logout_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }
}
