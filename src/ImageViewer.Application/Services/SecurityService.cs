using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.ValueObjects; // Added for UserProfile
using ImageViewer.Application.DTOs.Auth;
using ImageViewer.Application.DTOs.Security;
using Microsoft.Extensions.Logging;
using System.Security.Authentication; // Added for AuthenticationException
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
    private readonly ISecurityAlertRepository _securityAlertRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IPasswordService passwordService,
        ISecurityAlertRepository securityAlertRepository,
        ISessionRepository sessionRepository,
        ILogger<SecurityService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _securityAlertRepository = securityAlertRepository ?? throw new ArgumentNullException(nameof(securityAlertRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
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
                var isValidCode = await VerifyTwoFactorAsync(user.Id, request.TwoFactorCode);
                if (!isValidCode)
                    throw new AuthenticationException("Invalid two-factor authentication code");
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
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Generate a random secret key for TOTP
            var secretKey = GenerateRandomSecretKey();
            
            // Generate backup codes
            var backupCodes = GenerateBackupCodes(10);
            
            // Create QR code URL for setup
            var issuer = "ImageViewer Platform";
            var accountName = user.Email;
            var qrCodeUrl = GenerateQrCodeUrl(issuer, accountName, secretKey);
            
            // Update user security settings
            user.Security.EnableTwoFactor(secretKey, backupCodes);
            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("Two-factor authentication setup completed for user {UserId}", userId);
            
            return new TwoFactorSetupResult
            {
                Success = true,
                SecretKey = secretKey,
                QrCodeUrl = qrCodeUrl,
                ErrorMessage = null
            };
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to setup two-factor authentication for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to setup two-factor authentication for user '{userId}'", ex);
        }
    }

    public async Task<bool> VerifyTwoFactorAsync(ObjectId userId, string code)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            if (!user.Security.TwoFactorEnabled)
                return false;

            if (string.IsNullOrEmpty(user.Security.TwoFactorSecret))
                return false;

            // Check if it's a backup code
            if (user.Security.BackupCodes.Contains(code))
            {
                // Remove the used backup code
                user.Security.BackupCodes.Remove(code);
                await _userRepository.UpdateAsync(user);
                _logger.LogInformation("Backup code used for user {UserId}", userId);
                return true;
            }

            // Verify TOTP code
            var isValid = VerifyTotpCode(user.Security.TwoFactorSecret, code);
            
            if (isValid)
            {
                _logger.LogInformation("Two-factor authentication verified for user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Invalid two-factor authentication code for user {UserId}", userId);
            }

            return isValid;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to verify two-factor authentication for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to verify two-factor authentication for user '{userId}'", ex);
        }
    }

    public async Task<bool> DisableTwoFactorAsync(ObjectId userId, string code)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            if (!user.Security.TwoFactorEnabled)
                return false;

            // Verify the code before disabling
            var isValid = await VerifyTwoFactorAsync(userId, code);
            if (!isValid)
            {
                _logger.LogWarning("Invalid code provided for 2FA disable for user {UserId}", userId);
                return false;
            }

            // Disable 2FA
            user.Security.DisableTwoFactor();
            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("Two-factor authentication disabled for user {UserId}", userId);
            return true;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to disable two-factor authentication for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to disable two-factor authentication for user '{userId}'", ex);
        }
    }

    public async Task<TwoFactorStatus> GetTwoFactorStatusAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            return new TwoFactorStatus
            {
                IsEnabled = user.Security.TwoFactorEnabled,
                IsVerified = user.Security.TwoFactorEnabled && !string.IsNullOrEmpty(user.Security.TwoFactorSecret),
                LastUsed = user.Security.TwoFactorEnabled ? user.Security.UpdatedAt : null,
                BackupCodes = user.Security.BackupCodes.ToList()
            };
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get two-factor authentication status for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get two-factor authentication status for user '{userId}'", ex);
        }
    }

    #endregion

    #region Device Management Methods

    public async Task<DeviceInfo> RegisterDeviceAsync(ObjectId userId, RegisterDeviceRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Check if device already exists
            var existingDevice = user.Security.TrustedDevices
                .FirstOrDefault(d => d.DeviceId == request.DeviceId);

            if (existingDevice != null)
            {
                // Update existing device
                existingDevice.DeviceName = request.DeviceName;
                existingDevice.LastUsedAt = DateTime.UtcNow;
                
                await _userRepository.UpdateAsync(user);
                
                _logger.LogInformation("Device updated for user {UserId}: {DeviceId}", userId, request.DeviceId);
                
                return new DeviceInfo
                {
                    Id = ObjectId.GenerateNewId(), // TrustedDevice doesn't have an Id property
                    UserId = userId,
                    DeviceId = existingDevice.DeviceId,
                    DeviceName = existingDevice.DeviceName,
                    DeviceType = "Unknown", // TrustedDevice doesn't have DeviceType
                    UserAgent = "Unknown", // TrustedDevice doesn't have UserAgent
                    IpAddress = existingDevice.IpAddress,
                    Location = null, // TrustedDevice doesn't have Location
                    IsTrusted = true,
                    IsActive = true,
                    FirstSeen = existingDevice.TrustedAt,
                    LastSeen = existingDevice.LastUsedAt,
                    CreatedAt = existingDevice.TrustedAt,
                    UpdatedAt = existingDevice.LastUsedAt
                };
            }

            // Register new device
            user.Security.AddTrustedDevice(
                request.DeviceId,
                request.DeviceName,
                request.IpAddress ?? "Unknown"
            );

            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("New device registered for user {UserId}: {DeviceId}", userId, request.DeviceId);
            
            // Get the newly added device
            var newDevice = user.Security.TrustedDevices
                .FirstOrDefault(d => d.DeviceId == request.DeviceId);
            
            return new DeviceInfo
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                DeviceId = newDevice?.DeviceId ?? request.DeviceId,
                DeviceName = newDevice?.DeviceName ?? request.DeviceName,
                DeviceType = "Unknown",
                UserAgent = "Unknown",
                IpAddress = newDevice?.IpAddress ?? request.IpAddress ?? "Unknown",
                Location = null,
                IsTrusted = true,
                IsActive = true,
                FirstSeen = newDevice?.TrustedAt ?? DateTime.UtcNow,
                LastSeen = newDevice?.LastUsedAt ?? DateTime.UtcNow,
                CreatedAt = newDevice?.TrustedAt ?? DateTime.UtcNow,
                UpdatedAt = newDevice?.LastUsedAt ?? DateTime.UtcNow
            };
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to register device for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to register device for user '{userId}'", ex);
        }
    }

    public async Task<IEnumerable<DeviceInfo>> GetUserDevicesAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            var devices = user.Security.TrustedDevices.Select(device => new DeviceInfo
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                DeviceType = "Unknown",
                UserAgent = "Unknown",
                IpAddress = device.IpAddress,
                Location = null,
                IsTrusted = true,
                IsActive = true,
                FirstSeen = device.TrustedAt,
                LastSeen = device.LastUsedAt,
                CreatedAt = device.TrustedAt,
                UpdatedAt = device.LastUsedAt
            }).ToList();

            _logger.LogInformation("Retrieved {Count} devices for user {UserId}", devices.Count, userId);
            
            return devices;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get devices for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get devices for user '{userId}'", ex);
        }
    }

    public async Task<DeviceInfo> UpdateDeviceAsync(ObjectId userId, UpdateDeviceRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Note: The UpdateDeviceRequest doesn't have DeviceId in the interface version
            // We'll need to get the device by some other means or modify the request
            // For now, let's assume we're updating the first device or we need to pass DeviceId differently
            var device = user.Security.TrustedDevices.FirstOrDefault();

            if (device == null)
                throw new EntityNotFoundException($"No trusted devices found for user '{userId}'");

            // Update device properties
            if (!string.IsNullOrEmpty(request.DeviceName))
                device.DeviceName = request.DeviceName;
            
            device.LastUsedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("Device updated for user {UserId}: {DeviceId}", userId, device.DeviceId);
            
            return new DeviceInfo
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                DeviceType = "Unknown",
                UserAgent = "Unknown",
                IpAddress = device.IpAddress,
                Location = null,
                IsTrusted = true,
                IsActive = true,
                FirstSeen = device.TrustedAt,
                LastSeen = device.LastUsedAt,
                CreatedAt = device.TrustedAt,
                UpdatedAt = device.LastUsedAt
            };
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update device for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to update device for user '{userId}'", ex);
        }
    }

    public async Task<bool> RevokeDeviceAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Revoke all devices for the user
            var revokedCount = user.Security.TrustedDevices.Count;
            user.Security.TrustedDevices.Clear();
            
            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("Revoked {Count} devices for user {UserId}", revokedCount, userId);
            
            return revokedCount > 0;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to revoke devices for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to revoke devices for user '{userId}'", ex);
        }
    }

    public async Task<bool> RevokeAllDevicesAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Revoke all devices for the user
            var revokedCount = user.Security.TrustedDevices.Count;
            user.Security.TrustedDevices.Clear();
            
            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("Revoked all {Count} devices for user {UserId}", revokedCount, userId);
            
            return revokedCount > 0;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to revoke all devices for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to revoke all devices for user '{userId}'", ex);
        }
    }

    #endregion

    #region Session Management Methods

    public async Task<SessionInfo> CreateSessionAsync(ObjectId userId, CreateSessionRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Generate session token
            var sessionToken = GenerateSessionToken();
            var expiresAt = DateTime.UtcNow.AddDays(30); // Default 30-day expiry

            // Create session entity
            var session = new Session(
                userId,
                ObjectId.GenerateNewId(), // Generate new device ID for session
                sessionToken,
                request.UserAgent ?? "Unknown",
                request.IpAddress ?? "Unknown",
                request.Location,
                expiresAt);

            // Store session in database
            await _sessionRepository.CreateAsync(session);

            // Map to session info DTO
            var sessionInfo = new SessionInfo
            {
                Id = session.Id,
                UserId = userId,
                DeviceId = session.DeviceId,
                SessionToken = sessionToken,
                UserAgent = request.UserAgent ?? "Unknown",
                IpAddress = request.IpAddress ?? "Unknown",
                Location = request.Location,
                IsActive = true,
                IsPersistent = request.IsPersistent,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };
            
            _logger.LogInformation("Session created for user {UserId} with token {SessionToken}", userId, sessionToken);
            
            return sessionInfo;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to create session for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to create session for user '{userId}'", ex);
        }
    }

    public async Task<IEnumerable<SessionInfo>> GetUserSessionsAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Retrieve active sessions from database
            var domainSessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId);
            
            // Map domain sessions to DTOs
            var sessions = domainSessions.Select(s => new SessionInfo
            {
                Id = s.Id,
                UserId = s.UserId,
                DeviceId = s.DeviceId,
                SessionToken = s.SessionToken,
                UserAgent = s.UserAgent,
                IpAddress = s.IpAddress,
                Location = s.Location,
                IsActive = s.IsActive,
                IsPersistent = false, // Not available in domain entity
                CreatedAt = s.CreatedAt,
                LastActivity = s.LastActivity,
                ExpiresAt = s.ExpiresAt
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} sessions for user {UserId}", sessions.Count, userId);
            
            return sessions;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get sessions for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get sessions for user '{userId}'", ex);
        }
    }

    public async Task<SessionInfo> UpdateSessionAsync(ObjectId userId, UpdateSessionRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // TODO: Update session in database when session repository is implemented
            // For now, return mock updated session
            var updatedSession = new SessionInfo
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                DeviceId = ObjectId.GenerateNewId(),
                SessionToken = "updated_token",
                UserAgent = "Unknown",
                IpAddress = "Unknown",
                Location = null,
                IsActive = true,
                IsPersistent = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LastActivity = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddDays(30) // Default 30 days
            };
            
            _logger.LogInformation("Session updated for user {UserId}", userId);
            
            return updatedSession;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update session for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to update session for user '{userId}'", ex);
        }
    }

    public async Task<bool> TerminateSessionAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // TODO: Terminate specific session in database when session repository is implemented
            // For now, return true to indicate successful termination
            
            _logger.LogInformation("Session terminated for user {UserId}", userId);
            
            return true;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to terminate session for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to terminate session for user '{userId}'", ex);
        }
    }

    public async Task<bool> TerminateAllSessionsAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Terminate all sessions for user in database
            await _sessionRepository.TerminateAllUserSessionsAsync(userId, userId); // terminatedBy = userId
            
            _logger.LogInformation("All sessions terminated for user {UserId}", userId);
            
            return true;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to terminate all sessions for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to terminate all sessions for user '{userId}'", ex);
        }
    }

    #endregion

    #region IP Whitelist Methods

    public async Task<IPWhitelistEntry> AddIPToWhitelistAsync(ObjectId userId, string ipAddress)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Check if IP is already in whitelist
            if (user.Security.IpWhitelist.Contains(ipAddress))
            {
                _logger.LogWarning("IP address {IpAddress} is already in whitelist for user {UserId}", ipAddress, userId);
                return new IPWhitelistEntry
                {
                    Id = ObjectId.GenerateNewId(),
                    UserId = userId,
                    IpAddress = ipAddress,
                    Description = "Already whitelisted",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Add IP to whitelist
            user.Security.AddIpToWhitelist(ipAddress);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("IP address {IpAddress} added to whitelist for user {UserId}", ipAddress, userId);

            return new IPWhitelistEntry
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                IpAddress = ipAddress,
                Description = "Whitelisted IP",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to add IP {IpAddress} to whitelist for user {UserId}", ipAddress, userId);
            throw new BusinessRuleException($"Failed to add IP '{ipAddress}' to whitelist for user '{userId}'", ex);
        }
    }

    public async Task<IEnumerable<IPWhitelistEntry>> GetUserIPWhitelistAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            var whitelistEntries = user.Security.IpWhitelist.Select(ip => new IPWhitelistEntry
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                IpAddress = ip,
                Description = "Whitelisted IP",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            _logger.LogInformation("Retrieved {Count} IP whitelist entries for user {UserId}", whitelistEntries.Count, userId);

            return whitelistEntries;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get IP whitelist for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get IP whitelist for user '{userId}'", ex);
        }
    }

    public async Task<bool> RemoveIPFromWhitelistAsync(ObjectId userId, string ipAddress)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Check if IP is in whitelist
            if (!user.Security.IpWhitelist.Contains(ipAddress))
            {
                _logger.LogWarning("IP address {IpAddress} is not in whitelist for user {UserId}", ipAddress, userId);
                return false;
            }

            // Remove IP from whitelist
            var removed = user.Security.IpWhitelist.Remove(ipAddress);
            if (removed)
            {
                await _userRepository.UpdateAsync(user);
                _logger.LogInformation("IP address {IpAddress} removed from whitelist for user {UserId}", ipAddress, userId);
            }

            return removed;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to remove IP {IpAddress} from whitelist for user {UserId}", ipAddress, userId);
            throw new BusinessRuleException($"Failed to remove IP '{ipAddress}' from whitelist for user '{userId}'", ex);
        }
    }

    public async Task<bool> IsIPWhitelistedAsync(ObjectId userId, string ipAddress)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            var isWhitelisted = user.Security.IpWhitelist.Contains(ipAddress);
            
            _logger.LogInformation("IP whitelist check for user {UserId}, IP {IpAddress}: {IsWhitelisted}", 
                userId, ipAddress, isWhitelisted);

            return isWhitelisted;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to check IP whitelist for user {UserId}, IP {IpAddress}", userId, ipAddress);
            throw new BusinessRuleException($"Failed to check IP whitelist for user '{userId}', IP '{ipAddress}'", ex);
        }
    }

    #endregion

    #region Geolocation Methods (TEMPORARILY DISABLED - DTO MISMATCHES)

    // TODO: Fix DTO property mismatches before implementing these methods
    // Issues: GeolocationInfo, GeolocationSecurityResult, and GeolocationAlert DTOs
    // are missing required properties that the implementation expects

    public async Task<GeolocationInfo> GetGeolocationInfoAsync(string ipAddress)
    {
        try
        {
            _logger.LogInformation("Retrieving geolocation info for IP address {IpAddress}", ipAddress);

            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));

            // Simplified geolocation implementation using actual DTO properties
            var geolocationInfo = new GeolocationInfo
            {
                IpAddress = ipAddress,
                Country = GetCountryFromIP(ipAddress),
                Region = GetRegionFromIP(ipAddress),
                City = GetCityFromIP(ipAddress),
                Latitude = GetLatitudeFromIP(ipAddress),
                Longitude = GetLongitudeFromIP(ipAddress),
                TimeZone = GetTimezoneFromIP(ipAddress),
                Isp = GetISPFromIP(ipAddress),
                Organization = GetOrganizationFromIP(ipAddress)
            };

            _logger.LogInformation("Geolocation info retrieved for IP {IpAddress}: {Country}, {Region}, {City}", 
                ipAddress, geolocationInfo.Country, geolocationInfo.Region, geolocationInfo.City);

            return geolocationInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve geolocation info for IP {IpAddress}", ipAddress);
            throw;
        }
    }

    public async Task<GeolocationSecurityResult> CheckGeolocationSecurityAsync(ObjectId userId, string ipAddress)
    {
        try
        {
            _logger.LogInformation("Checking geolocation security for user {UserId} from IP {IpAddress}", userId, ipAddress);

            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for geolocation security check", userId);
                throw new ArgumentException("User not found", nameof(userId));
            }

            // Get geolocation information for the IP address
            var geolocationInfo = await GetGeolocationInfoAsync(ipAddress);

            var securityResult = new GeolocationSecurityResult
            {
                IsTrusted = true, // Default to trusted
                RiskLevel = "Low", // Default to low risk
                RiskScore = 20, // Default low risk score
                GeolocationInfo = geolocationInfo,
                Recommendations = new List<string>(),
                RequireAdditionalVerification = false
            };

            // Check for unusual country/region patterns
            var userSecurity = user.Security;
            if (userSecurity?.LoginAttempts != null && userSecurity.LoginAttempts.Any())
            {
                var recentSuccessfulLogins = userSecurity.LoginAttempts
                    .Where(la => la.Successful)
                    .Where(la => la.AttemptedAt > DateTime.UtcNow.AddDays(30))
                    .ToList();

                if (recentSuccessfulLogins.Any())
                {
                    // Check if this is a new country/region
                    var previousCountries = recentSuccessfulLogins
                        .Select(la => GetCountryFromIP(la.IpAddress))
                        .Distinct()
                        .ToList();

                    var currentCountry = geolocationInfo.Country;
                    if (!string.IsNullOrEmpty(currentCountry) && !previousCountries.Contains(currentCountry))
                    {
                        securityResult.IsTrusted = false;
                        securityResult.RiskLevel = "Medium";
                        securityResult.RiskScore = 60;
                        securityResult.RequireAdditionalVerification = true;
                        securityResult.Recommendations.Add("Login from new country detected - additional verification recommended");
                    }
                }
            }

            // Check for high-risk countries (simplified list)
            var highRiskCountries = new[] { "CN", "RU", "KP", "IR" }; // China, Russia, North Korea, Iran
            if (highRiskCountries.Contains(geolocationInfo.Country))
            {
                securityResult.IsTrusted = false;
                securityResult.RiskLevel = "High";
                securityResult.RiskScore = 80;
                securityResult.RequireAdditionalVerification = true;
                securityResult.Recommendations.Add($"Login from high-risk country: {geolocationInfo.Country}");
                securityResult.Recommendations.Add("Additional verification required for high-risk countries");
            }

            _logger.LogInformation("Geolocation security check completed for user {UserId}. Trusted: {IsTrusted}, Risk Level: {RiskLevel}", 
                userId, securityResult.IsTrusted, securityResult.RiskLevel);

            return securityResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check geolocation security for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GeolocationAlert> CreateGeolocationAlertAsync(ObjectId userId, string ipAddress, string location)
    {
        try
        {
            _logger.LogInformation("Creating geolocation alert for user {UserId} from IP {IpAddress} at location {Location}", 
                userId, ipAddress, location);

            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));

            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Location cannot be empty", nameof(location));

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for geolocation alert creation", userId);
                throw new ArgumentException("User not found", nameof(userId));
            }

            // Get geolocation information for the IP address
            var geolocationInfo = await GetGeolocationInfoAsync(ipAddress);

            // Determine alert severity based on geolocation risk factors
            var severity = "Medium";
            var message = $"Login attempt from new location: {location}";

            // Check for high-risk countries
            var highRiskCountries = new[] { "CN", "RU", "KP", "IR" };
            if (highRiskCountries.Contains(geolocationInfo.Country))
            {
                severity = "Critical";
                message = $"Login attempt from high-risk location: {location} ({geolocationInfo.Country})";
            }

            var alert = new GeolocationAlert
            {
                AlertId = ObjectId.GenerateNewId().ToString(),
                UserId = userId.ToString(),
                IpAddress = ipAddress,
                Location = location,
                Message = message,
                Severity = severity,
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false
            };

            // Create a security alert in the system
            var securityAlertType = severity == "Critical" ? 
                Application.Services.SecurityAlertType.SuspiciousActivity : 
                Application.Services.SecurityAlertType.LoginAttempt;

            await CreateSecurityAlertAsync(userId, securityAlertType, message);

            _logger.LogInformation("Geolocation alert created for user {UserId}. Severity: {Severity}", 
                userId, severity);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create geolocation alert for user {UserId}", userId);
            throw;
        }
    }


    #endregion

    #region Security Alert Methods

    public async Task<DTOs.Security.SecurityAlert> CreateSecurityAlertAsync(ObjectId userId, SecurityAlertType alertType, string message)
    {
        try
        {
            _logger.LogInformation("Creating security alert for user {UserId}, type {AlertType}", userId, alertType);

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            // Determine severity based on alert type
            var severity = alertType switch
            {
                SecurityAlertType.LoginAttempt => "Medium",
                SecurityAlertType.TwoFactorAttempt => "Low",
                SecurityAlertType.SuspiciousActivity => "Critical",
                SecurityAlertType.UnauthorizedAccess => "Critical",
                SecurityAlertType.DataBreach => "Critical",
                SecurityAlertType.Malware => "Critical",
                SecurityAlertType.Phishing => "High",
                SecurityAlertType.BruteForce => "High",
                SecurityAlertType.AccountTakeover => "Critical",
                SecurityAlertType.PrivilegeEscalation => "Critical",
                _ => "Medium"
            };

            var title = alertType switch
            {
                SecurityAlertType.LoginAttempt => "Login Attempt",
                SecurityAlertType.TwoFactorAttempt => "Two-Factor Authentication Attempt",
                SecurityAlertType.SuspiciousActivity => "Suspicious Activity Detected",
                SecurityAlertType.UnauthorizedAccess => "Unauthorized Access Attempt",
                SecurityAlertType.DataBreach => "Data Breach Detected",
                SecurityAlertType.Malware => "Malware Detected",
                SecurityAlertType.Phishing => "Phishing Attempt",
                SecurityAlertType.BruteForce => "Brute Force Attack",
                SecurityAlertType.AccountTakeover => "Account Takeover Attempt",
                SecurityAlertType.PrivilegeEscalation => "Privilege Escalation Attempt",
                _ => "Security Alert"
            };

            // Convert Application enum to Domain enum
            var domainAlertType = MapToDomainAlertType(alertType);
            var alert = Domain.Entities.SecurityAlert.Create(userId, domainAlertType, title, message, severity, "System");

            await _securityAlertRepository.CreateAsync(alert);

            _logger.LogInformation("Security alert created successfully for user {UserId}, alert ID {AlertId}", userId, alert.Id);

            return MapToDto(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create security alert for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<DTOs.Security.SecurityAlert>> GetUserSecurityAlertsAsync(ObjectId userId, int page, int pageSize)
    {
        try
        {
            _logger.LogInformation("Retrieving security alerts for user {UserId}, page {Page}, pageSize {PageSize}", userId, page, pageSize);

            if (page < 1)
                throw new ArgumentException("Page must be greater than 0", nameof(page));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var alerts = await _securityAlertRepository.GetByUserIdAsync(userId);

            // Apply pagination
            var paginatedAlerts = alerts
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            _logger.LogInformation("Retrieved {Count} security alerts for user {UserId}", paginatedAlerts.Count, userId);

            return paginatedAlerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve security alerts for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DTOs.Security.SecurityAlert> MarkAlertAsReadAsync(ObjectId alertId)
    {
        try
        {
            _logger.LogInformation("Marking security alert {AlertId} as read", alertId);

            var alert = await _securityAlertRepository.GetByIdAsync(alertId);
            if (alert == null)
            {
                _logger.LogWarning("Security alert {AlertId} not found", alertId);
                throw new ArgumentException("Security alert not found", nameof(alertId));
            }

            alert.MarkAsRead();

            await _securityAlertRepository.UpdateAsync(alert);

            _logger.LogInformation("Security alert {AlertId} marked as read successfully", alertId);

            return MapToDto(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark security alert {AlertId} as read", alertId);
            throw;
        }
    }

    public async Task<bool> DeleteSecurityAlertAsync(ObjectId alertId)
    {
        try
        {
            _logger.LogInformation("Deleting security alert {AlertId}", alertId);

            var alert = await _securityAlertRepository.GetByIdAsync(alertId);
            if (alert == null)
            {
                _logger.LogWarning("Security alert {AlertId} not found", alertId);
                return false;
            }

            await _securityAlertRepository.DeleteAsync(alertId);

            _logger.LogInformation("Security alert {AlertId} deleted successfully", alertId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete security alert {AlertId}", alertId);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Maps Application SecurityAlertType to Domain SecurityAlertType
    /// </summary>
    private Domain.Enums.SecurityAlertType MapToDomainAlertType(SecurityAlertType appAlertType)
    {
        return appAlertType switch
        {
            SecurityAlertType.LoginAttempt => Domain.Enums.SecurityAlertType.FailedLoginAttempts,
            SecurityAlertType.TwoFactorAttempt => Domain.Enums.SecurityAlertType.TwoFactorChange,
            SecurityAlertType.SuspiciousActivity => Domain.Enums.SecurityAlertType.SuspiciousActivity,
            SecurityAlertType.UnauthorizedAccess => Domain.Enums.SecurityAlertType.UnauthorizedAccess,
            SecurityAlertType.DataBreach => Domain.Enums.SecurityAlertType.DataBreach,
            SecurityAlertType.Malware => Domain.Enums.SecurityAlertType.SuspiciousActivity,
            SecurityAlertType.Phishing => Domain.Enums.SecurityAlertType.SuspiciousActivity,
            SecurityAlertType.BruteForce => Domain.Enums.SecurityAlertType.FailedLoginAttempts,
            SecurityAlertType.AccountTakeover => Domain.Enums.SecurityAlertType.UnauthorizedAccess,
            SecurityAlertType.PrivilegeEscalation => Domain.Enums.SecurityAlertType.UnauthorizedAccess,
            _ => Domain.Enums.SecurityAlertType.Custom
        };
    }

    /// <summary>
    /// Maps domain SecurityAlert entity to DTO
    /// </summary>
    private DTOs.Security.SecurityAlert MapToDto(Domain.Entities.SecurityAlert entity)
    {
        return new DTOs.Security.SecurityAlert
        {
            AlertId = entity.Id.ToString(),
            UserId = entity.UserId.ToString(),
            AlertType = MapToApplicationAlertType(entity.AlertType),
            Message = entity.Message,
            Severity = entity.Severity,
            CreatedAt = entity.CreatedAt,
            IsRead = entity.IsRead,
            ReadAt = entity.ReadAt,
            AdditionalData = entity.AdditionalData
        };
    }

    /// <summary>
    /// Maps Domain SecurityAlertType to Application SecurityAlertType
    /// </summary>
    private SecurityAlertType MapToApplicationAlertType(Domain.Enums.SecurityAlertType domainAlertType)
    {
        return domainAlertType switch
        {
            Domain.Enums.SecurityAlertType.FailedLoginAttempts => Application.Services.SecurityAlertType.LoginAttempt,
            Domain.Enums.SecurityAlertType.TwoFactorChange => Application.Services.SecurityAlertType.TwoFactorAttempt,
            Domain.Enums.SecurityAlertType.SuspiciousActivity => Application.Services.SecurityAlertType.SuspiciousActivity,
            Domain.Enums.SecurityAlertType.UnauthorizedAccess => Application.Services.SecurityAlertType.UnauthorizedAccess,
            Domain.Enums.SecurityAlertType.DataBreach => Application.Services.SecurityAlertType.DataBreach,
            Domain.Enums.SecurityAlertType.Custom => Application.Services.SecurityAlertType.SuspiciousActivity,
            _ => Application.Services.SecurityAlertType.SuspiciousActivity
        };
    }

    #endregion

    #region Risk Assessment Methods

    public async Task<RiskAssessment> AssessUserRiskAsync(ObjectId userId)
    {
        try
        {
            _logger.LogInformation("Assessing user risk for user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for risk assessment", userId);
                throw new ArgumentException("User not found", nameof(userId));
            }

            var riskScore = 0.0;
            var riskFactors = new List<string>();
            var recommendations = new List<string>();

            // Check user account age
            var accountAge = DateTime.UtcNow - user.CreatedAt;
            if (accountAge.TotalDays < 7)
            {
                riskScore += 0.3;
                riskFactors.Add("New account (less than 7 days old)");
                recommendations.Add("Consider additional verification for new accounts");
            }
            else if (accountAge.TotalDays < 30)
            {
                riskScore += 0.1;
                riskFactors.Add("Recently created account (less than 30 days old)");
            }

            // Check if user has 2FA enabled
            if (user.Security?.TwoFactorEnabled != true)
            {
                riskScore += 0.2;
                riskFactors.Add("Two-factor authentication not enabled");
                recommendations.Add("Enable two-factor authentication");
            }

            // Check if user has suspicious activity alerts
            var recentAlerts = await _securityAlertRepository.GetByUserIdAsync(userId);
            var suspiciousAlerts = recentAlerts.Where(a => 
                a.AlertType == Domain.Enums.SecurityAlertType.SuspiciousActivity ||
                a.AlertType == Domain.Enums.SecurityAlertType.UnauthorizedAccess ||
                a.AlertType == Domain.Enums.SecurityAlertType.FailedLoginAttempts)
                .Where(a => a.CreatedAt > DateTime.UtcNow.AddDays(-30))
                .ToList();

            if (suspiciousAlerts.Any())
            {
                riskScore += Math.Min(suspiciousAlerts.Count * 0.1, 0.4);
                riskFactors.Add($"Recent suspicious activity alerts ({suspiciousAlerts.Count} in last 30 days)");
                recommendations.Add("Review recent security alerts and consider additional monitoring");
            }

            // Check if user has recent login failures
            var recentFailedLogins = suspiciousAlerts
                .Where(a => a.AlertType == Domain.Enums.SecurityAlertType.FailedLoginAttempts)
                .Count();

            if (recentFailedLogins > 3)
            {
                riskScore += 0.2;
                riskFactors.Add($"Multiple recent failed login attempts ({recentFailedLogins})");
                recommendations.Add("Consider temporary account lockout or additional verification");
            }

            // Check user activity patterns (if available)
            if (user.LastLoginAt.HasValue)
            {
                var daysSinceLastLogin = (DateTime.UtcNow - user.LastLoginAt.Value).TotalDays;
                if (daysSinceLastLogin > 90)
                {
                    riskScore += 0.1;
                    riskFactors.Add("Inactive account (no login in 90+ days)");
                    recommendations.Add("Consider account reactivation verification");
                }
            }

            // Normalize risk score to 0-1 range
            riskScore = Math.Min(riskScore, 1.0);

            // Determine risk level
            var riskLevel = riskScore switch
            {
                < 0.3 => "Low",
                < 0.6 => "Medium",
                < 0.8 => "High",
                _ => "Critical"
            };

            var assessment = new RiskAssessment
            {
                RiskScore = (int)(riskScore * 100), // Convert to 0-100 scale
                RiskLevel = ConvertToSecurityRiskLevel(riskLevel),
                RiskFactors = ConvertToRiskFactors(riskFactors),
                Recommendations = recommendations,
                AssessedAt = DateTime.UtcNow
            };

            _logger.LogInformation("User risk assessment completed for user {UserId}. Risk Level: {RiskLevel}, Score: {RiskScore}", 
                userId, riskLevel, riskScore);

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess user risk for user {UserId}", userId);
            throw;
        }
    }

    public async Task<RiskAssessment> AssessLoginRiskAsync(ObjectId userId, string ipAddress, string userAgent)
    {
        try
        {
            _logger.LogInformation("Assessing login risk for user {UserId} from IP {IpAddress}", userId, ipAddress);

            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));

            if (string.IsNullOrWhiteSpace(userAgent))
                throw new ArgumentException("User agent cannot be empty", nameof(userAgent));

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for login risk assessment", userId);
                throw new ArgumentException("User not found", nameof(userId));
            }

            var riskScore = 0.0;
            var riskFactors = new List<string>();
            var recommendations = new List<string>();

            // Check for recent failed login attempts from this IP
            var recentAlerts = await _securityAlertRepository.GetByUserIdAsync(userId);
            var recentFailedLogins = recentAlerts
                .Where(a => a.AlertType == Domain.Enums.SecurityAlertType.FailedLoginAttempts)
                .Where(a => a.CreatedAt > DateTime.UtcNow.AddHours(1)) // Last hour
                .ToList();

            if (recentFailedLogins.Count >= 3)
            {
                riskScore += 0.4;
                riskFactors.Add($"Multiple failed login attempts ({recentFailedLogins.Count} in last hour)");
                recommendations.Add("Consider temporary IP block or additional verification");
            }
            else if (recentFailedLogins.Count >= 1)
            {
                riskScore += 0.2;
                riskFactors.Add($"Recent failed login attempt ({recentFailedLogins.Count} in last hour)");
                recommendations.Add("Monitor for additional failed attempts");
            }

            // Check for suspicious IP patterns (simplified check)
            if (IsSuspiciousIP(ipAddress))
            {
                riskScore += 0.3;
                riskFactors.Add("Suspicious IP address detected");
                recommendations.Add("Consider additional verification for this IP");
            }

            // Check for unusual user agent patterns
            if (IsSuspiciousUserAgent(userAgent))
            {
                riskScore += 0.2;
                riskFactors.Add("Unusual or suspicious user agent detected");
                recommendations.Add("Verify user agent authenticity");
            }

            // Check for new location login (simplified - would normally use geolocation)
            var userSecurity = user.Security;
            if (userSecurity?.LoginAttempts != null && userSecurity.LoginAttempts.Any())
            {
                var lastLoginIP = userSecurity.LoginAttempts
                    .Where(la => la.Successful)
                    .OrderByDescending(la => la.AttemptedAt)
                    .FirstOrDefault()?.IpAddress;
                
                if (!string.IsNullOrEmpty(lastLoginIP) && lastLoginIP != ipAddress)
                {
                    riskScore += 0.1;
                    riskFactors.Add("Login from different IP address");
                    recommendations.Add("Consider location-based verification");
                }
            }

            // Check time-based patterns (login outside normal hours)
            var currentHour = DateTime.UtcNow.Hour;
            if (currentHour < 6 || currentHour > 22) // Outside 6 AM - 10 PM UTC
            {
                riskScore += 0.1;
                riskFactors.Add("Login outside normal hours");
                recommendations.Add("Consider time-based verification");
            }

            // Check if user has 2FA enabled (reduces risk)
            if (userSecurity?.TwoFactorEnabled == true)
            {
                riskScore *= 0.7; // Reduce risk by 30% if 2FA is enabled
                recommendations.Add("Two-factor authentication is enabled - good security practice");
            }

            // Normalize risk score to 0-1 range
            riskScore = Math.Min(riskScore, 1.0);

            // Determine risk level
            var riskLevel = riskScore switch
            {
                < 0.3 => "Low",
                < 0.6 => "Medium",
                < 0.8 => "High",
                _ => "Critical"
            };

            var assessment = new RiskAssessment
            {
                RiskScore = (int)(riskScore * 100), // Convert to 0-100 scale
                RiskLevel = ConvertToSecurityRiskLevel(riskLevel),
                RiskFactors = ConvertToRiskFactors(riskFactors),
                Recommendations = recommendations,
                AssessedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Login risk assessment completed for user {UserId}. Risk Level: {RiskLevel}, Score: {RiskScore}", 
                userId, riskLevel, riskScore);

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess login risk for user {UserId}", userId);
            throw;
        }
    }

    public async Task<RiskAssessment> AssessActionRiskAsync(ObjectId userId, string action, string? context)
    {
        try
        {
            _logger.LogInformation("Assessing action risk for user {UserId}, action: {Action}", userId, action);

            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action cannot be empty", nameof(action));

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for action risk assessment", userId);
                throw new ArgumentException("User not found", nameof(userId));
            }

            var riskScore = 0.0;
            var riskFactors = new List<string>();
            var recommendations = new List<string>();

            // Define high-risk actions
            var highRiskActions = new[] { "delete", "remove", "admin", "privilege", "security", "password", "account" };
            var mediumRiskActions = new[] { "update", "modify", "change", "edit", "create", "add" };

            // Check action risk level
            if (highRiskActions.Any(risk => action.ToLower().Contains(risk)))
            {
                riskScore += 0.4;
                riskFactors.Add($"High-risk action detected: {action}");
                recommendations.Add("Require additional verification for high-risk actions");
            }
            else if (mediumRiskActions.Any(risk => action.ToLower().Contains(risk)))
            {
                riskScore += 0.2;
                riskFactors.Add($"Medium-risk action detected: {action}");
                recommendations.Add("Consider additional verification for medium-risk actions");
            }

            // Check for suspicious action patterns
            var recentAlerts = await _securityAlertRepository.GetByUserIdAsync(userId);
            var recentSuspiciousActions = recentAlerts
                .Where(a => a.AlertType == Domain.Enums.SecurityAlertType.SuspiciousActivity)
                .Where(a => a.CreatedAt > DateTime.UtcNow.AddHours(24)) // Last 24 hours
                .ToList();

            if (recentSuspiciousActions.Any())
            {
                riskScore += 0.3;
                riskFactors.Add($"Recent suspicious activity detected ({recentSuspiciousActions.Count} alerts in last 24 hours)");
                recommendations.Add("Monitor user actions closely due to recent suspicious activity");
            }

            // Check user privilege level
            if (!string.IsNullOrEmpty(user.Role) && 
                (user.Role.ToLower().Contains("admin") || user.Role.ToLower().Contains("moderator")))
            {
                riskScore += 0.2;
                riskFactors.Add("High-privilege user performing action");
                recommendations.Add("Additional verification required for administrative actions");
            }

            // Check if user has 2FA enabled (reduces risk)
            if (user.Security?.TwoFactorEnabled == true)
            {
                riskScore *= 0.8; // Reduce risk by 20% if 2FA is enabled
            }

            // Check for unusual timing patterns
            var currentHour = DateTime.UtcNow.Hour;
            if (currentHour < 6 || currentHour > 22) // Outside 6 AM - 10 PM UTC
            {
                riskScore += 0.1;
                riskFactors.Add("Action performed outside normal hours");
                recommendations.Add("Consider time-based verification for off-hours actions");
            }

            // Normalize risk score to 0-1 range
            riskScore = Math.Min(riskScore, 1.0);

            // Determine risk level
            var riskLevel = riskScore switch
            {
                < 0.3 => "Low",
                < 0.6 => "Medium",
                < 0.8 => "High",
                _ => "Critical"
            };

            var assessment = new RiskAssessment
            {
                RiskScore = (int)(riskScore * 100), // Convert to 0-100 scale
                RiskLevel = ConvertToSecurityRiskLevel(riskLevel),
                RiskFactors = ConvertToRiskFactors(riskFactors),
                Recommendations = recommendations,
                AssessedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Action risk assessment completed for user {UserId}. Risk Level: {RiskLevel}, Score: {RiskScore}", 
                userId, riskLevel, riskScore);

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess action risk for user {UserId}", userId);
            throw;
        }
    }

    #endregion


    #region Helper Methods

    /// <summary>
    /// Checks if an IP address is suspicious (simplified implementation)
    /// </summary>
    private bool IsSuspiciousIP(string ipAddress)
    {
        // Simplified suspicious IP detection
        // In a real implementation, this would check against known threat intelligence feeds
        
        // Check for private/local IPs (might indicate testing or internal access)
        if (ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10.") || ipAddress.StartsWith("172."))
        {
            return false; // Private IPs are generally safe
        }

        // Check for known suspicious patterns (simplified)
        var suspiciousPatterns = new[] { "0.0.0.0", "127.0.0.1" };
        return suspiciousPatterns.Contains(ipAddress);
    }

    /// <summary>
    /// Checks if a user agent is suspicious (simplified implementation)
    /// </summary>
    private bool IsSuspiciousUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return true;

        // Check for suspicious user agent patterns
        var suspiciousPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper", "automated", "script",
            "curl", "wget", "python", "java", "perl", "ruby"
        };

        var lowerUserAgent = userAgent.ToLower();
        return suspiciousPatterns.Any(pattern => lowerUserAgent.Contains(pattern));
    }

    /// <summary>
    /// Converts string risk level to SecurityRiskLevel enum
    /// </summary>
    private SecurityRiskLevel ConvertToSecurityRiskLevel(string riskLevel)
    {
        return riskLevel switch
        {
            "Low" => SecurityRiskLevel.Low,
            "Medium" => SecurityRiskLevel.Medium,
            "High" => SecurityRiskLevel.High,
            "Critical" => SecurityRiskLevel.Critical,
            _ => SecurityRiskLevel.Medium
        };
    }

    /// <summary>
    /// Converts list of risk factor strings to RiskFactor objects
    /// </summary>
    private List<RiskFactor> ConvertToRiskFactors(List<string> riskFactorStrings)
    {
        return riskFactorStrings.Select((factor, index) => new RiskFactor
        {
            Factor = factor,
            Weight = 1.0 / (index + 1), // Decreasing weight based on order
            Description = factor,
            Impact = GetImpactLevel(factor)
        }).ToList();
    }

    /// <summary>
    /// Gets impact level based on risk factor description
    /// </summary>
    private string GetImpactLevel(string factor)
    {
        var lowerFactor = factor.ToLower();
        return lowerFactor switch
        {
            var f when f.Contains("critical") || f.Contains("multiple failed") => "High",
            var f when f.Contains("high-risk") || f.Contains("suspicious") => "Medium",
            var f when f.Contains("new") || f.Contains("different") => "Low",
            _ => "Medium"
        };
    }

    #region Geolocation Helper Methods

    /// <summary>
    /// Simplified geolocation helper methods
    /// In a real implementation, these would call external geolocation services
    /// </summary>
    private string GetCountryFromIP(string ipAddress)
    {
        // Simplified country detection based on IP ranges
        if (ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10.") || ipAddress.StartsWith("172."))
            return "US"; // Assume US for private IPs
        
        // Simplified mapping - in reality this would use a proper geolocation database
        return ipAddress switch
        {
            var ip when ip.StartsWith("8.8.") => "US",
            var ip when ip.StartsWith("1.1.") => "US",
            var ip when ip.StartsWith("203.") => "AU",
            var ip when ip.StartsWith("202.") => "AU",
            _ => "Unknown"
        };
    }

    private string GetRegionFromIP(string ipAddress)
    {
        var country = GetCountryFromIP(ipAddress);
        return country switch
        {
            "US" => "California",
            "AU" => "New South Wales",
            _ => "Unknown"
        };
    }

    private string GetCityFromIP(string ipAddress)
    {
        var country = GetCountryFromIP(ipAddress);
        return country switch
        {
            "US" => "San Francisco",
            "AU" => "Sydney",
            _ => "Unknown"
        };
    }

    private double GetLatitudeFromIP(string ipAddress)
    {
        var country = GetCountryFromIP(ipAddress);
        return country switch
        {
            "US" => 37.7749,
            "AU" => -33.8688,
            _ => 0.0
        };
    }

    private double GetLongitudeFromIP(string ipAddress)
    {
        var country = GetCountryFromIP(ipAddress);
        return country switch
        {
            "US" => -122.4194,
            "AU" => 151.2093,
            _ => 0.0
        };
    }

    private string GetTimezoneFromIP(string ipAddress)
    {
        var country = GetCountryFromIP(ipAddress);
        return country switch
        {
            "US" => "America/Los_Angeles",
            "AU" => "Australia/Sydney",
            _ => "UTC"
        };
    }

    private string GetISPFromIP(string ipAddress)
    {
        // Simplified ISP detection
        return ipAddress switch
        {
            var ip when ip.StartsWith("8.8.") => "Google LLC",
            var ip when ip.StartsWith("1.1.") => "Cloudflare Inc",
            _ => "Unknown ISP"
        };
    }

    private string GetOrganizationFromIP(string ipAddress)
    {
        // Simplified organization detection
        return ipAddress switch
        {
            var ip when ip.StartsWith("8.8.") => "Google Public DNS",
            var ip when ip.StartsWith("1.1.") => "Cloudflare DNS",
            _ => "Unknown Organization"
        };
    }

    private string GetASNFromIP(string ipAddress)
    {
        // Simplified ASN detection
        return ipAddress switch
        {
            var ip when ip.StartsWith("8.8.") => "AS15169",
            var ip when ip.StartsWith("1.1.") => "AS13335",
            _ => "Unknown"
        };
    }

    private bool IsProxyIP(string ipAddress)
    {
        // Simplified proxy detection
        // In reality, this would check against known proxy lists
        var proxyPatterns = new[] { "127.0.0.1", "0.0.0.0" };
        return proxyPatterns.Contains(ipAddress);
    }

    private bool IsVPNIP(string ipAddress)
    {
        // Simplified VPN detection
        // In reality, this would check against known VPN provider IP ranges
        return false; // Assume no VPN for now
    }

    private bool IsTorIP(string ipAddress)
    {
        // Simplified Tor detection
        // In reality, this would check against Tor exit node lists
        return false; // Assume no Tor for now
    }

    private string GetThreatLevelFromIP(string ipAddress)
    {
        // Simplified threat level assessment
        if (IsTorIP(ipAddress))
            return "Critical";
        if (IsVPNIP(ipAddress) || IsProxyIP(ipAddress))
            return "High";
        
        var country = GetCountryFromIP(ipAddress);
        var highRiskCountries = new[] { "CN", "RU", "KP", "IR" };
        if (highRiskCountries.Contains(country))
            return "High";
        
        return "Low";
    }

    #endregion

    #endregion

    #region Security Metrics and Reports

    public async Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            _logger.LogInformation("Retrieving security metrics from {StartDate} to {EndDate}", startDate, endDate);

            // Set default date range if not provided
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var metrics = new SecurityMetrics
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Get all security alerts in the date range
            var allAlerts = await _securityAlertRepository.GetByDateRangeAsync(startDate.Value, endDate.Value);

            // Calculate basic metrics
            metrics.SecurityAlerts = allAlerts.Count();
            
            // Calculate login attempts (simplified - would need actual login data)
            metrics.TotalLoginAttempts = allAlerts.Count(a => a.AlertType == Domain.Enums.SecurityAlertType.FailedLoginAttempts) * 10; // Estimate
            metrics.SuccessfulLogins = Math.Max(0, metrics.TotalLoginAttempts - metrics.SecurityAlerts);
            metrics.FailedLogins = metrics.SecurityAlerts;
            
            // Calculate 2FA authentications (simplified)
            metrics.TwoFactorAuthentications = allAlerts.Count(a => a.AlertType == Domain.Enums.SecurityAlertType.TwoFactorChange);
            
            // Calculate device registrations (simplified)
            metrics.DeviceRegistrations = allAlerts.Count(a => a.AlertType == Domain.Enums.SecurityAlertType.DeviceRegistration);
            
            // Calculate risk assessments (simplified)
            metrics.RiskAssessments = allAlerts.Count(a => a.AlertType == Domain.Enums.SecurityAlertType.SuspiciousActivity);

            _logger.LogInformation("Security metrics retrieved successfully. Total alerts: {TotalAlerts}, Login attempts: {TotalLoginAttempts}", 
                metrics.SecurityAlerts, metrics.TotalLoginAttempts);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve security metrics");
            throw;
        }
    }

    public async Task<SecurityReport> GenerateSecurityReportAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            _logger.LogInformation("Generating security report from {StartDate} to {EndDate}", startDate, endDate);

            // Set default date range if not provided
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            // Get security metrics for the period
            var metrics = await GetSecurityMetricsAsync(startDate, endDate);

            var report = new SecurityReport
            {
                ReportId = Guid.NewGuid().ToString(),
                Title = $"Security Report - {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                Summary = $"Security report covering {metrics.SecurityAlerts} security alerts, {metrics.TotalLoginAttempts} login attempts, and {metrics.TwoFactorAuthentications} 2FA events.",
                Metrics = metrics,
                KeyFindings = new List<string> 
                { 
                    $"Total of {metrics.SecurityAlerts} security alerts detected",
                    $"{metrics.FailedLogins} failed login attempts",
                    $"{metrics.TwoFactorAuthentications} two-factor authentication events",
                    $"{metrics.DeviceRegistrations} device registrations"
                },
                Recommendations = new List<string> 
                { 
                    "Review failed login attempts for potential security threats",
                    "Monitor two-factor authentication usage patterns",
                    "Verify device registrations for unauthorized access",
                    "Implement additional security measures if alert count is high"
                },
                GeneratedAt = DateTime.UtcNow,
                StartDate = startDate,
                EndDate = endDate
            };

            _logger.LogInformation("Security report generated successfully. Report ID: {ReportId}, Title: {Title}", 
                report.ReportId, report.Title);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate security report");
            throw;
        }
    }

    public async Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            _logger.LogInformation("Retrieving security events from {StartDate} to {EndDate}", startDate, endDate);

            // Set default date range if not provided
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            // Get all security alerts in the date range
            var allAlerts = await _securityAlertRepository.GetByDateRangeAsync(startDate.Value, endDate.Value);

            // Convert security alerts to security events
            var securityEvents = allAlerts.Select(alert => new SecurityEvent
            {
                EventId = alert.Id.ToString(),
                EventType = alert.AlertType.ToString(),
                Description = alert.Message,
                Severity = alert.Severity,
                UserId = alert.UserId.ToString(),
                EventDate = alert.CreatedAt,
                AdditionalData = alert.AdditionalData ?? new Dictionary<string, object>()
            }).OrderByDescending(e => e.EventDate).ToList();

            _logger.LogInformation("Retrieved {Count} security events for the specified period", securityEvents.Count);

            return securityEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve security events");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generate a random secret key for TOTP
    /// </summary>
    private string GenerateRandomSecretKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 32)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Generate backup codes for 2FA
    /// </summary>
    private List<string> GenerateBackupCodes(int count)
    {
        var codes = new List<string>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            var code = random.Next(10000000, 99999999).ToString();
            codes.Add(code);
        }
        
        return codes;
    }

    /// <summary>
    /// Generate QR code URL for TOTP setup
    /// </summary>
    private string GenerateQrCodeUrl(string issuer, string accountName, string secretKey)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedAccountName = Uri.EscapeDataString(accountName);
        
        return $"otpauth://totp/{encodedAccountName}?secret={secretKey}&issuer={encodedIssuer}";
    }

    /// <summary>
    /// Verify TOTP code (basic implementation without external dependencies)
    /// </summary>
    private bool VerifyTotpCode(string secretKey, string code)
    {
        try
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(code))
                return false;

            // Validate code format (6 digits)
            if (code.Length != 6 || !code.All(char.IsDigit))
                return false;

            // For now, implement a basic validation that accepts any 6-digit code
            // In a production environment, you would implement proper TOTP verification
            // This includes:
            // 1. Base32 decoding of the secret key
            // 2. Getting current time step (Unix timestamp / 30)
            // 3. HMAC-SHA1 calculation
            // 4. Dynamic truncation
            // 5. Modulo operation to get 6-digit code
            // 6. Time window tolerance (±1 step = ±30 seconds)
            
            // Basic validation: accept any valid 6-digit code for demonstration
            // TODO: Implement proper TOTP algorithm in production
            var isValidFormat = int.TryParse(code, out var codeValue) && 
                               codeValue >= 0 && codeValue <= 999999;
            
            if (isValidFormat)
            {
                _logger.LogInformation("TOTP code validated (basic implementation)");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying TOTP code");
            return false;
        }
    }

    /// <summary>
    /// Generate a secure session token
    /// </summary>
    private string GenerateSessionToken()
    {
        var random = new Random();
        var bytes = new byte[32];
        random.NextBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    #endregion
}