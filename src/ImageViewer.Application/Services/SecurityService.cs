using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Application.DTOs.Auth;
using ImageViewer.Application.DTOs.Security;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Logging;
// IPasswordService is now in Application layer

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for security and authentication operations
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IPasswordService passwordService,
        ILogger<SecurityService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Authentication result</returns>
    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        try
        {
            // Validate input
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                throw new ValidationException("Username and password are required");

            // Get user by username
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed for username {Username}: User not found", request.Username);
                throw new AuthenticationException("Invalid username or password");
            }

            // Check if account is locked
            if (user.IsAccountLocked())
            {
                _logger.LogWarning("Authentication failed for user {UserId}: Account is locked", user.Id);
                throw new AuthenticationException("Account is locked. Please contact administrator");
            }

            // Verify password
            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                // Log failed login attempt
                await _userRepository.LogFailedLoginAttemptAsync(user.Id);
                _logger.LogWarning("Authentication failed for user {UserId}: Invalid password", user.Id);
                throw new AuthenticationException("Invalid username or password");
            }

            // Check if 2FA is required
            if (user.TwoFactorEnabled && string.IsNullOrWhiteSpace(request.TwoFactorCode))
            {
                _logger.LogInformation("2FA required for user {UserId}", user.Id);
                return new LoginResult
                {
                    RequiresTwoFactor = true,
                    TempToken = GenerateTempToken(user.Id)
                };
            }

            // Verify 2FA code if provided
            if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(request.TwoFactorCode))
            {
                // TODO: Implement 2FA verification
                // if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, request.TwoFactorCode))
                //     throw new AuthenticationException("Invalid two-factor authentication code");
            }

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store refresh token
            await _userRepository.StoreRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(30));

            // Log successful login
            await _userRepository.LogSuccessfulLoginAsync(user.Id, request.IpAddress ?? "", request.UserAgent ?? "");

            // Clear failed login attempts
            await _userRepository.ClearFailedLoginAttemptsAsync(user.Id);

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return new LoginResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserInfo
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role ?? "User",
                    IsEmailVerified = user.IsEmailVerified,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Login validation failed: {Message}", ex.Message);
            throw;
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Authentication failed for user {Username}: {Message}", request.Username, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user {Username}", request.Username);
            throw new BusinessRuleException("Login failed due to an unexpected error", ex);
        }
    }

    /// <summary>
    /// Register new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Registration result</returns>
    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validate input
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new ValidationException("Username, email, and password are required");

            // Check if username already exists
            var existingUserByUsername = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUserByUsername != null)
            {
                _logger.LogWarning("Registration failed: Username {Username} already exists", request.Username);
                return new RegisterResult
                {
                    Success = false,
                    ErrorMessage = "Username already exists"
                };
            }

            // Check if email already exists
            var existingUserByEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                return new RegisterResult
                {
                    Success = false,
                    ErrorMessage = "Email already exists"
                };
            }

            // Validate password strength
            if (!_passwordService.IsStrongPassword(request.Password))
            {
                _logger.LogWarning("Registration failed: Password does not meet strength requirements");
                return new RegisterResult
                {
                    Success = false,
                    ErrorMessage = "Password does not meet strength requirements"
                };
            }

            // Hash password
            var passwordHash = _passwordService.HashPassword(request.Password);

            // Create user
            var user = new User(request.Username, request.Email, passwordHash, "User");
            
            // Update profile if names provided
            if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
            {
                var profile = new UserProfile();
                if (!string.IsNullOrWhiteSpace(request.FirstName))
                    profile.UpdateFirstName(request.FirstName);
                if (!string.IsNullOrWhiteSpace(request.LastName))
                    profile.UpdateLastName(request.LastName);
                user.UpdateProfile(profile);
            }

            // Save user
            await _userRepository.CreateAsync(user);

            _logger.LogInformation("User {UserId} registered successfully", user.Id);

            return new RegisterResult
            {
                Success = true,
                UserId = user.Id.ToString(),
                RequiresEmailVerification = true
            };
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Registration validation failed: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for user {Username}", request.Username);
            throw new BusinessRuleException("Registration failed due to an unexpected error", ex);
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>New authentication result</returns>
    public async Task<LoginResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ValidationException("Refresh token is required");

            // Get user by refresh token
            var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
            if (user == null)
            {
                _logger.LogWarning("Token refresh failed: Invalid refresh token");
                throw new AuthenticationException("Invalid refresh token");
            }

            // Generate new tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Update refresh token
            await _userRepository.StoreRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(30));

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            return new LoginResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserInfo
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role ?? "User",
                    IsEmailVerified = user.IsEmailVerified,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Token refresh validation failed: {Message}", ex.Message);
            throw;
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Token refresh authentication failed: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            throw new BusinessRuleException("Token refresh failed due to an unexpected error", ex);
        }
    }

    /// <summary>
    /// Logout user and invalidate tokens
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="refreshToken">Refresh token to invalidate</param>
    public async Task LogoutAsync(ObjectId userId, string? refreshToken = null)
    {
        try
        {
            // Invalidate refresh token if provided
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _userRepository.InvalidateRefreshTokenAsync(userId, refreshToken);
            }

            _logger.LogInformation("User {UserId} logged out successfully", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout for user {UserId}", userId);
            throw new BusinessRuleException("Logout failed due to an unexpected error", ex);
        }
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var principal = _jwtService.ValidateToken(token);
            return principal != null && !_jwtService.IsTokenExpired(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password</param>
    /// <param name="newPassword">New password</param>
    public async Task ChangePasswordAsync(ObjectId userId, string currentPassword, string newPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                throw new ValidationException("Current password and new password are required");

            // Get user
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Verify current password
            if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash))
                throw new AuthenticationException("Current password is incorrect");

            // Validate new password strength
            if (!_passwordService.IsStrongPassword(newPassword))
                throw new ValidationException("New password does not meet strength requirements");

            // Hash new password
            var newPasswordHash = _passwordService.HashPassword(newPassword);

            // Update password
            user.UpdatePasswordHash(newPasswordHash);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Password change validation failed for user {UserId}: {Message}", userId, ex.Message);
            throw;
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Password change authentication failed for user {UserId}: {Message}", userId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during password change for user {UserId}", userId);
            throw new BusinessRuleException($"Password change failed for user '{userId}'", ex);
        }
    }

    /// <summary>
    /// Generate temporary token for 2FA flow
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Temporary token</returns>
    private string GenerateTempToken(ObjectId userId)
    {
        // TODO: Implement proper temporary token generation
        // For now, return a simple base64 encoded string
        var tokenData = $"{userId}:{DateTime.UtcNow:O}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenData));
    }

    #region Two-Factor Authentication Methods

    public async Task<TwoFactorSetupResult> SetupTwoFactorAsync(ObjectId userId)
    {
        // TODO: Implement 2FA setup
        await Task.CompletedTask;
        throw new NotImplementedException("Two-factor authentication setup not yet implemented");
    }

    public async Task<bool> VerifyTwoFactorAsync(ObjectId userId, string code)
    {
        // TODO: Implement 2FA verification
        await Task.CompletedTask;
        throw new NotImplementedException("Two-factor authentication verification not yet implemented");
    }

    public async Task<bool> DisableTwoFactorAsync(ObjectId userId, string code)
    {
        // TODO: Implement 2FA disable
        await Task.CompletedTask;
        throw new NotImplementedException("Two-factor authentication disable not yet implemented");
    }

    public async Task<TwoFactorStatus> GetTwoFactorStatusAsync(ObjectId userId)
    {
        // TODO: Implement 2FA status check
        await Task.CompletedTask;
        throw new NotImplementedException("Two-factor authentication status check not yet implemented");
    }

    #endregion

    #region Device Management Methods

    public async Task<DeviceInfo> RegisterDeviceAsync(ObjectId userId, RegisterDeviceRequest request)
    {
        // TODO: Implement device registration
        await Task.CompletedTask;
        throw new NotImplementedException("Device registration not yet implemented");
    }

    public async Task<IEnumerable<DeviceInfo>> GetUserDevicesAsync(ObjectId userId)
    {
        // TODO: Implement device listing
        await Task.CompletedTask;
        throw new NotImplementedException("Device listing not yet implemented");
    }

    public async Task<DeviceInfo> UpdateDeviceAsync(ObjectId userId, UpdateDeviceRequest request)
    {
        // TODO: Implement device update
        await Task.CompletedTask;
        throw new NotImplementedException("Device update not yet implemented");
    }

    public async Task<bool> RevokeDeviceAsync(ObjectId userId)
    {
        // TODO: Implement device revocation
        await Task.CompletedTask;
        throw new NotImplementedException("Device revocation not yet implemented");
    }

    public async Task<bool> RevokeAllDevicesAsync(ObjectId userId)
    {
        // TODO: Implement all devices revocation
        await Task.CompletedTask;
        throw new NotImplementedException("All devices revocation not yet implemented");
    }

    #endregion

    #region Session Management Methods

    public async Task<SessionInfo> CreateSessionAsync(ObjectId userId, CreateSessionRequest request)
    {
        // TODO: Implement session creation
        await Task.CompletedTask;
        throw new NotImplementedException("Session creation not yet implemented");
    }

    public async Task<IEnumerable<SessionInfo>> GetUserSessionsAsync(ObjectId userId)
    {
        // TODO: Implement session listing
        await Task.CompletedTask;
        throw new NotImplementedException("Session listing not yet implemented");
    }

    public async Task<SessionInfo> UpdateSessionAsync(ObjectId userId, UpdateSessionRequest request)
    {
        // TODO: Implement session update
        await Task.CompletedTask;
        throw new NotImplementedException("Session update not yet implemented");
    }

    public async Task<bool> TerminateSessionAsync(ObjectId userId)
    {
        // TODO: Implement session termination
        await Task.CompletedTask;
        throw new NotImplementedException("Session termination not yet implemented");
    }

    public async Task<bool> TerminateAllSessionsAsync(ObjectId userId)
    {
        // TODO: Implement all sessions termination
        await Task.CompletedTask;
        throw new NotImplementedException("All sessions termination not yet implemented");
    }

    #endregion

    #region IP Whitelist Methods

    public async Task<IPWhitelistEntry> AddIPToWhitelistAsync(ObjectId userId, string ipAddress)
    {
        // TODO: Implement IP whitelist addition
        await Task.CompletedTask;
        throw new NotImplementedException("IP whitelist addition not yet implemented");
    }

    public async Task<IEnumerable<IPWhitelistEntry>> GetUserIPWhitelistAsync(ObjectId userId)
    {
        // TODO: Implement IP whitelist retrieval
        await Task.CompletedTask;
        throw new NotImplementedException("IP whitelist retrieval not yet implemented");
    }

    public async Task<bool> RemoveIPFromWhitelistAsync(ObjectId userId, string ipAddress)
    {
        // TODO: Implement IP whitelist removal
        await Task.CompletedTask;
        throw new NotImplementedException("IP whitelist removal not yet implemented");
    }

    public async Task<bool> IsIPWhitelistedAsync(ObjectId userId, string ipAddress)
    {
        // TODO: Implement IP whitelist check
        await Task.CompletedTask;
        throw new NotImplementedException("IP whitelist check not yet implemented");
    }

    #endregion

    #region Geolocation Methods

    public async Task<GeolocationInfo> GetGeolocationInfoAsync(string ipAddress)
    {
        // TODO: Implement geolocation info retrieval
        await Task.CompletedTask;
        throw new NotImplementedException("Geolocation info retrieval not yet implemented");
    }

    public async Task<GeolocationSecurityResult> CheckGeolocationSecurityAsync(ObjectId userId, string ipAddress)
    {
        // TODO: Implement geolocation security check
        await Task.CompletedTask;
        throw new NotImplementedException("Geolocation security check not yet implemented");
    }

    public async Task<GeolocationAlert> CreateGeolocationAlertAsync(ObjectId userId, string ipAddress, string location)
    {
        // TODO: Implement geolocation alert creation
        await Task.CompletedTask;
        throw new NotImplementedException("Geolocation alert creation not yet implemented");
    }

    #endregion

    #region Security Alert Methods

    public async Task<SecurityAlert> CreateSecurityAlertAsync(ObjectId userId, SecurityAlertType alertType, string message)
    {
        // TODO: Implement security alert creation
        await Task.CompletedTask;
        throw new NotImplementedException("Security alert creation not yet implemented");
    }

    public async Task<IEnumerable<SecurityAlert>> GetUserSecurityAlertsAsync(ObjectId userId, int page, int pageSize)
    {
        // TODO: Implement security alerts retrieval
        await Task.CompletedTask;
        throw new NotImplementedException("Security alerts retrieval not yet implemented");
    }

    public async Task<SecurityAlert> MarkAlertAsReadAsync(ObjectId alertId)
    {
        // TODO: Implement alert marking as read
        await Task.CompletedTask;
        throw new NotImplementedException("Alert marking as read not yet implemented");
    }

    public async Task<bool> DeleteSecurityAlertAsync(ObjectId alertId)
    {
        // TODO: Implement security alert deletion
        await Task.CompletedTask;
        throw new NotImplementedException("Security alert deletion not yet implemented");
    }

    #endregion

    #region Risk Assessment Methods

    public async Task<RiskAssessment> AssessUserRiskAsync(ObjectId userId)
    {
        // TODO: Implement user risk assessment
        await Task.CompletedTask;
        throw new NotImplementedException("User risk assessment not yet implemented");
    }

    public async Task<RiskAssessment> AssessLoginRiskAsync(ObjectId userId, string ipAddress, string userAgent)
    {
        // TODO: Implement login risk assessment
        await Task.CompletedTask;
        throw new NotImplementedException("Login risk assessment not yet implemented");
    }

    public async Task<RiskAssessment> AssessActionRiskAsync(ObjectId userId, string action, string? context)
    {
        // TODO: Implement action risk assessment
        await Task.CompletedTask;
        throw new NotImplementedException("Action risk assessment not yet implemented");
    }

    #endregion

    #region Security Metrics and Reports

    public async Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime? startDate, DateTime? endDate)
    {
        // TODO: Implement security metrics retrieval
        await Task.CompletedTask;
        throw new NotImplementedException("Security metrics retrieval not yet implemented");
    }

    public async Task<SecurityReport> GenerateSecurityReportAsync(DateTime? startDate, DateTime? endDate)
    {
        // TODO: Implement security report generation
        await Task.CompletedTask;
        throw new NotImplementedException("Security report generation not yet implemented");
    }

    public async Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(DateTime? startDate, DateTime? endDate)
    {
        // TODO: Implement security events retrieval
        await Task.CompletedTask;
        throw new NotImplementedException("Security events retrieval not yet implemented");
    }

    #endregion
}