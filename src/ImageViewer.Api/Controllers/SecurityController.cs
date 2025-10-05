using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Auth;
using ImageViewer.Domain.Exceptions;
using System.Security.Authentication;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for security and authentication operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly ISecurityService _securityService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(ISecurityService securityService, IJwtService jwtService, ILogger<SecurityController> logger)
    {
        _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticate user
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] ImageViewer.Application.DTOs.Auth.LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Implement login functionality using SecurityService
            var serviceRequest = new ImageViewer.Application.Services.LoginRequest
            {
                Username = request.Username,
                Password = request.Password,
                RememberMe = request.RememberMe
            };
            var result = await _securityService.LoginAsync(serviceRequest);
            
            if (result.Success)
            {
                return Ok(new { 
                    message = "Login successful", 
                    token = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt,
                    user = result.User != null ? new { 
                        id = result.User.Id,
                        username = result.User.Username,
                        email = result.User.Email
                    } : new { 
                        id = "",
                        username = request.Username,
                        email = ""
                    }
                });
            }
            else
            {
                return Unauthorized(new { message = result.ErrorMessage });
            }
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _securityService.RegisterAsync(request);
            
            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage, validationErrors = result.ValidationErrors });

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] ImageViewer.Application.DTOs.Auth.RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _securityService.RefreshTokenAsync(request.RefreshToken);
            
            if (!result.Success)
                return Unauthorized(new { message = result.ErrorMessage });

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            // Extract user ID from token claims
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim == null || !ObjectId.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }
            
            await _securityService.LogoutAsync(userId, request.RefreshToken);
            
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Extract user ID from token claims
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim == null || !ObjectId.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }
            
            await _securityService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            
            return Ok(new { message = "Password changed successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password change failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    /// <summary>
    /// Logout user
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] string? token = null)
    {
        try
        {
            // Extract user ID from token if provided
            ObjectId userId = ObjectId.Empty;
            if (!string.IsNullOrEmpty(token))
            {
                var userIdString = _jwtService.GetUserIdFromToken(token);
                if (!string.IsNullOrEmpty(userIdString) && ObjectId.TryParse(userIdString, out var parsedUserId))
                {
                    userId = parsedUserId;
                }
                else
                {
                    _logger.LogWarning("Invalid token provided for logout - could not extract user ID");
                    return BadRequest(new { message = "Invalid token" });
                }
            }
            
            await _securityService.LogoutAsync(userId, token);
            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate authentication token
    /// </summary>
    [HttpPost("validate-token")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { message = "Token cannot be null or empty" });

            // Validate token using JWT service
            var principal = _jwtService.ValidateToken(request.Token);
            var isValid = principal != null;
            
            if (isValid)
            {
                var userId = _jwtService.GetUserIdFromToken(request.Token);
                var username = _jwtService.GetUsernameFromToken(request.Token);
                var role = _jwtService.GetRoleFromToken(request.Token);
                
                return Ok(new { 
                    valid = true,
                    userId = userId,
                    username = username,
                    role = role
                });
            }
            else
            {
                return Ok(new { valid = false });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Setup two-factor authentication
    /// </summary>
    [HttpPost("two-factor/setup")]
    public async Task<IActionResult> SetupTwoFactor([FromQuery] string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var result = await _securityService.SetupTwoFactorAsync(userObjectId);
            return Ok(result);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Two-factor setup failed for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Verify two-factor authentication code
    /// </summary>
    [HttpPost("two-factor/verify")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isValid = await _securityService.VerifyTwoFactorAsync(userObjectId, request.Code);
            return Ok(new { valid = isValid });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Two-factor verification failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    [HttpPost("two-factor/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _securityService.DisableTwoFactorAsync(userObjectId, request.Code);
            return Ok(new { success = success });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Two-factor disable failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get two-factor authentication status
    /// </summary>
    [HttpGet("two-factor/status")]
    public async Task<IActionResult> GetTwoFactorStatus([FromQuery] string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var status = await _securityService.GetTwoFactorStatusAsync(userObjectId);
            return Ok(status);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get two-factor status for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Register device
    /// </summary>
    [HttpPost("devices/register")]
    public async Task<IActionResult> RegisterDevice([FromQuery] string userId, [FromBody] RegisterDeviceRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deviceInfo = await _securityService.RegisterDeviceAsync(userObjectId, request);
            return CreatedAtAction(nameof(GetUserDevices), new { userId = userId }, deviceInfo);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device registration failed for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user devices
    /// </summary>
    [HttpGet("devices")]
    public async Task<IActionResult> GetUserDevices([FromQuery] string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var devices = await _securityService.GetUserDevicesAsync(userObjectId);
            return Ok(devices);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get devices for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update device
    /// </summary>
    [HttpPut("devices/{deviceId}")]
    public async Task<IActionResult> UpdateDevice(string deviceId, [FromBody] UpdateDeviceRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(deviceId, out var deviceObjectId))
                return BadRequest(new { message = "Invalid device ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deviceInfo = await _securityService.UpdateDeviceAsync(deviceObjectId, request);
            return Ok(deviceInfo);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device update failed for device {DeviceId}", deviceId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Revoke device
    /// </summary>
    [HttpDelete("devices/{deviceId}")]
    public async Task<IActionResult> RevokeDevice(string deviceId)
    {
        try
        {
            if (!ObjectId.TryParse(deviceId, out var deviceObjectId))
                return BadRequest(new { message = "Invalid device ID format" });

            var success = await _securityService.RevokeDeviceAsync(deviceObjectId);
            return Ok(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device revocation failed for device {DeviceId}", deviceId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Revoke all devices for user
    /// </summary>
    [HttpDelete("devices/user/{userId}")]
    public async Task<IActionResult> RevokeAllDevices(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var success = await _securityService.RevokeAllDevicesAsync(userObjectId);
            return Ok(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "All devices revocation failed for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create session
    /// </summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromQuery] string userId, [FromBody] CreateSessionRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var sessionInfo = await _securityService.CreateSessionAsync(userObjectId, request);
            return CreatedAtAction(nameof(GetUserSessions), new { userId = userId }, sessionInfo);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session creation failed for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetUserSessions([FromQuery] string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var sessions = await _securityService.GetUserSessionsAsync(userObjectId);
            return Ok(sessions);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update session
    /// </summary>
    [HttpPut("sessions/{sessionId}")]
    public async Task<IActionResult> UpdateSession(string sessionId, [FromBody] UpdateSessionRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(sessionId, out var sessionObjectId))
                return BadRequest(new { message = "Invalid session ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var sessionInfo = await _securityService.UpdateSessionAsync(sessionObjectId, request);
            return Ok(sessionInfo);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session update failed for session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Terminate session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> TerminateSession(string sessionId)
    {
        try
        {
            if (!ObjectId.TryParse(sessionId, out var sessionObjectId))
                return BadRequest(new { message = "Invalid session ID format" });

            var success = await _securityService.TerminateSessionAsync(sessionObjectId);
            return Ok(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session termination failed for session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Terminate all sessions for user
    /// </summary>
    [HttpDelete("sessions/user/{userId}")]
    public async Task<IActionResult> TerminateAllSessions(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var success = await _securityService.TerminateAllSessionsAsync(userObjectId);
            return Ok(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "All sessions termination failed for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Add IP to whitelist
    /// </summary>
    [HttpPost("ip-whitelist")]
    public async Task<IActionResult> AddIPToWhitelist([FromQuery] string userId, [FromQuery] string ipAddress)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (string.IsNullOrWhiteSpace(ipAddress))
                return BadRequest(new { message = "IP address cannot be null or empty" });

            var entry = await _securityService.AddIPToWhitelistAsync(userObjectId, ipAddress);
            return CreatedAtAction(nameof(GetUserIPWhitelist), new { userId = userId }, entry);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add IP {IpAddress} to whitelist for user {UserId}", ipAddress, userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user IP whitelist
    /// </summary>
    [HttpGet("ip-whitelist")]
    public async Task<IActionResult> GetUserIPWhitelist([FromQuery] string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var whitelist = await _securityService.GetUserIPWhitelistAsync(userObjectId);
            return Ok(whitelist);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get IP whitelist for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove IP from whitelist
    /// </summary>
    [HttpDelete("ip-whitelist")]
    public async Task<IActionResult> RemoveIPFromWhitelist([FromQuery] string userId, [FromQuery] string ipAddress)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (string.IsNullOrWhiteSpace(ipAddress))
                return BadRequest(new { message = "IP address cannot be null or empty" });

            var success = await _securityService.RemoveIPFromWhitelistAsync(userObjectId, ipAddress);
            return Ok(new { success = success });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove IP {IpAddress} from whitelist for user {UserId}", ipAddress, userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if IP is whitelisted
    /// </summary>
    [HttpGet("ip-whitelist/check")]
    public async Task<IActionResult> IsIPWhitelisted([FromQuery] string userId, [FromQuery] string ipAddress)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (string.IsNullOrWhiteSpace(ipAddress))
                return BadRequest(new { message = "IP address cannot be null or empty" });

            var isWhitelisted = await _securityService.IsIPWhitelistedAsync(userObjectId, ipAddress);
            return Ok(new { whitelisted = isWhitelisted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check IP whitelist for user {UserId} and IP {IpAddress}", userId, ipAddress);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get geolocation information
    /// </summary>
    [HttpGet("geolocation")]
    public async Task<IActionResult> GetGeolocationInfo([FromQuery] string ipAddress)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return BadRequest(new { message = "IP address cannot be null or empty" });

            var geolocationInfo = await _securityService.GetGeolocationInfoAsync(ipAddress);
            return Ok(geolocationInfo);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get geolocation info for IP {IpAddress}", ipAddress);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Check geolocation security
    /// </summary>
    [HttpPost("geolocation/check")]
    public async Task<IActionResult> CheckGeolocationSecurity([FromBody] CheckGeolocationSecurityRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _securityService.CheckGeolocationSecurityAsync(userObjectId, request.IpAddress);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check geolocation security");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create geolocation alert
    /// </summary>
    [HttpPost("geolocation/alert")]
    public async Task<IActionResult> CreateGeolocationAlert([FromBody] CreateGeolocationAlertRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var alert = await _securityService.CreateGeolocationAlertAsync(userObjectId, request.IpAddress, request.Location);
            return CreatedAtAction(nameof(GetUserSecurityAlerts), new { userId = request.UserId }, alert);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create geolocation alert");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create security alert
    /// </summary>
    [HttpPost("alerts")]
    public async Task<IActionResult> CreateSecurityAlert([FromBody] CreateSecurityAlertRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var alert = await _securityService.CreateSecurityAlertAsync(userObjectId, request.Type, request.Description);
            return CreatedAtAction(nameof(GetUserSecurityAlerts), new { userId = request.UserId }, alert);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create security alert");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user security alerts
    /// </summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetUserSecurityAlerts([FromQuery] string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var alerts = await _securityService.GetUserSecurityAlertsAsync(userObjectId, page, pageSize);
            return Ok(alerts);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security alerts for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark security alert as read
    /// </summary>
    [HttpPost("alerts/{alertId}/read")]
    public async Task<IActionResult> MarkAlertAsRead(string alertId)
    {
        try
        {
            if (!ObjectId.TryParse(alertId, out var alertObjectId))
                return BadRequest(new { message = "Invalid alert ID format" });

            var alert = await _securityService.MarkAlertAsReadAsync(alertObjectId);
            return Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark alert {AlertId} as read", alertId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete security alert
    /// </summary>
    [HttpDelete("alerts/{alertId}")]
    public async Task<IActionResult> DeleteSecurityAlert(string alertId)
    {
        try
        {
            if (!ObjectId.TryParse(alertId, out var alertObjectId))
                return BadRequest(new { message = "Invalid alert ID format" });

            var success = await _securityService.DeleteSecurityAlertAsync(alertObjectId);
            return Ok(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete security alert {AlertId}", alertId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Assess user risk
    /// </summary>
    [HttpPost("risk-assessment/user")]
    public async Task<IActionResult> AssessUserRisk([FromQuery] string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var assessment = await _securityService.AssessUserRiskAsync(userObjectId);
            return Ok(assessment);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess user risk for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Assess login risk
    /// </summary>
    [HttpPost("risk-assessment/login")]
    public async Task<IActionResult> AssessLoginRisk([FromBody] AssessLoginRiskRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var assessment = await _securityService.AssessLoginRiskAsync(userObjectId, request.IpAddress, request.UserAgent);
            return Ok(assessment);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess login risk");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Assess action risk
    /// </summary>
    [HttpPost("risk-assessment/action")]
    public async Task<IActionResult> AssessActionRisk([FromBody] AssessActionRiskRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var assessment = await _securityService.AssessActionRiskAsync(userObjectId, request.Action, request.IpAddress);
            return Ok(assessment);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess action risk");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get security metrics
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetSecurityMetrics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var metrics = await _securityService.GetSecurityMetricsAsync(fromDate, toDate);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate security report
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> GenerateSecurityReport([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var report = await _securityService.GenerateSecurityReportAsync(fromDate, toDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate security report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get security events
    /// </summary>
    [HttpGet("events")]
    public async Task<IActionResult> GetSecurityEvents([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var events = await _securityService.GetSecurityEventsAsync(fromDate, toDate);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for token validation
/// </summary>
public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Request model for two-factor verification
/// </summary>
public class VerifyTwoFactorRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Request model for two-factor disable
/// </summary>
public class DisableTwoFactorRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Request model for geolocation security check
/// </summary>
public class CheckGeolocationSecurityRequest
{
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

/// <summary>
/// Request model for geolocation alert creation
/// </summary>
public class CreateGeolocationAlertRequest
{
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Request model for security alert creation
/// </summary>
public class CreateSecurityAlertRequest
{
    public string UserId { get; set; } = string.Empty;
    public SecurityAlertType Type { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request model for login risk assessment
/// </summary>
public class AssessLoginRiskRequest
{
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

/// <summary>
/// Request model for action risk assessment
/// </summary>
public class AssessActionRiskRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}
