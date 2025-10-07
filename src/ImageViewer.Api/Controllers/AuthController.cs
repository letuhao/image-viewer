using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Authentication controller
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IJwtService jwtService, ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Login user
    /// </summary>
    [HttpPost("login")]
    public Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            // Simple authentication - in production, validate against database
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return Task.FromResult<ActionResult<LoginResponseDto>>(BadRequest(new { message = "Username and password are required" }));
            }

            // For demo purposes, accept any username/password
            // In production, validate against user database
            var userId = Guid.NewGuid().ToString();
            
            // Create a basic user entity for JWT token generation
            var user = new User(
                request.Username, 
                $"{request.Username}@example.com", // Demo email
                "demo_password_hash", // Demo password hash
                "User" // Role
            );

            // Generate JWT token using the service
            var token = _jwtService.GenerateAccessToken(user);

            var response = new LoginResponseDto
            {
                Token = token,
                UserId = user.Id.ToString(),
                Username = request.Username,
                Roles = new[] { user.Role },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            return Task.FromResult<ActionResult<LoginResponseDto>>(Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return Task.FromResult<ActionResult<LoginResponseDto>>(StatusCode(500, "Internal server error"));
        }
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserInfoDto> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            var response = new UserInfoDto
            {
                UserId = userId ?? "",
                Username = username ?? "",
                Roles = roles
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user info");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Logout user (client-side token removal)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        // JWT is stateless, so logout is handled client-side by removing the token
        return Ok(new { message = "Logged out successfully" });
    }
}

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public DateTime ExpiresAt { get; set; }
}

public class UserInfoDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
