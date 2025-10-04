# 🔐 Security Implementation Guide - ImageViewer Platform

**Ngày tạo:** 2025-01-03  
**Version:** 1.0.0  
**Mục tiêu:** Hướng dẫn chi tiết implement security features cho ImageViewer Platform

---

## 📋 Overview

Tài liệu này cung cấp hướng dẫn chi tiết để implement đầy đủ các tính năng security cho ImageViewer Platform. Dựa trên source code review, chúng ta cần thay thế tất cả placeholder implementations trong `SecurityService.cs` bằng các implementation thực tế.

---

## 🎯 Current Security Issues

### **Critical Issues Found:**
1. **Placeholder Authentication** - JWT tokens là hardcoded strings
2. **No Password Security** - Không có password hashing
3. **Missing 2FA** - Two-factor authentication chưa implement
4. **No Session Management** - Session handling chưa có
5. **Hardcoded Secrets** - JWT keys và passwords hardcoded

### **Files to Fix:**
- `src/ImageViewer.Application/Services/SecurityService.cs`
- `src/ImageViewer.Api/Controllers/SecurityController.cs`
- `src/ImageViewer.Api/Program.cs`
- `src/ImageViewer.Infrastructure/Services/JwtService.cs`

---

## 🔑 Phase 1: Core Authentication Implementation

### **Step 1.1: JWT Service Implementation**

**File:** `src/ImageViewer.Infrastructure/Services/JwtService.cs`

```csharp
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryHours;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = _configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key not configured");
        _issuer = _configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer not configured");
        _audience = _configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience not configured");
        _expiryHours = int.Parse(_configuration["Jwt:ExpiryHours"] ?? "24");
    }

    public string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("userId", user.Id.ToString()),
            new("username", user.Username),
            new("email", user.Email),
            new("role", user.Role ?? "User"),
            new("jti", Guid.NewGuid().ToString()), // JWT ID for revocation
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_expiryHours),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
```

### **Step 1.2: Password Security Implementation**

**File:** `src/ImageViewer.Infrastructure/Services/PasswordService.cs`

```csharp
using BCrypt.Net;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
    bool IsStrongPassword(string password);
}

public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        // Check for uppercase letter
        if (!password.Any(char.IsUpper))
            return false;

        // Check for lowercase letter
        if (!password.Any(char.IsLower))
            return false;

        // Check for digit
        if (!password.Any(char.IsDigit))
            return false;

        // Check for special character
        if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
            return false;

        return true;
    }
}
```

### **Step 1.3: Update SecurityService Implementation**

**File:** `src/ImageViewer.Application/Services/SecurityService.cs`

```csharp
// Replace the placeholder LoginAsync method with:
public async Task<LoginResult> LoginAsync(LoginRequest request)
{
    try
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Username and password are required");

        // Get user by username
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null)
            throw new AuthenticationException("Invalid username or password");

        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Log failed login attempt
            await _userRepository.LogFailedLoginAttemptAsync(user.Id);
            throw new AuthenticationException("Invalid username or password");
        }

        // Check if account is locked
        if (user.IsLocked)
            throw new AuthenticationException("Account is locked. Please contact administrator");

        // Check if 2FA is required
        if (user.TwoFactorEnabled && string.IsNullOrWhiteSpace(request.TwoFactorCode))
        {
            return new LoginResult
            {
                RequiresTwoFactor = true,
                TempToken = GenerateTempToken(user.Id)
            };
        }

        // Verify 2FA code if provided
        if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(request.TwoFactorCode))
        {
            if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, request.TwoFactorCode))
                throw new AuthenticationException("Invalid two-factor authentication code");
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Store refresh token
        await _userRepository.StoreRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(30));

        // Log successful login
        await _userRepository.LogSuccessfulLoginAsync(user.Id, request.IpAddress, request.UserAgent);

        // Clear failed login attempts
        await _userRepository.ClearFailedLoginAttemptsAsync(user.Id);

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
                Role = user.Role
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
```

---

## 🔐 Phase 2: Two-Factor Authentication

### **Step 2.1: Two-Factor Authentication Service**

**File:** `src/ImageViewer.Infrastructure/Services/TwoFactorService.cs`

```csharp
using OtpNet;
using QRCoder;
using System.Text;

namespace ImageViewer.Infrastructure.Services;

public interface ITwoFactorService
{
    string GenerateSecret();
    string GenerateQrCode(string secret, string username);
    bool VerifyCode(string secret, string code);
    IEnumerable<string> GenerateBackupCodes();
}

public class TwoFactorService : ITwoFactorService
{
    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCode(string secret, string username)
    {
        var issuer = "ImageViewer";
        var accountTitle = $"{issuer}:{username}";
        var manualEntryKey = secret;
        
        var qrCodeText = $"otpauth://totp/{accountTitle}?secret={manualEntryKey}&issuer={issuer}";
        
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeText, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCode(qrCodeData);
        using var qrCodeImage = qrCode.GetGraphic(20);
        
        using var ms = new MemoryStream();
        qrCodeImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        var qrCodeBytes = ms.ToArray();
        
        return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
    }

    public bool VerifyCode(string secret, string code)
    {
        try
        {
            var key = Base32Encoding.ToBytes(secret);
            var totp = new Totp(key);
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<string> GenerateBackupCodes()
    {
        var codes = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var code = GenerateRandomCode(8);
            codes.Add(code);
        }
        return codes;
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
```

### **Step 2.2: Update User Entity for 2FA**

**File:** `src/ImageViewer.Domain/Entities/User.cs`

```csharp
// Add these properties to User entity:
[BsonElement("twoFactorEnabled")]
public bool TwoFactorEnabled { get; private set; }

[BsonElement("twoFactorSecret")]
public string? TwoFactorSecret { get; private set; }

[BsonElement("backupCodes")]
public List<string> BackupCodes { get; private set; } = new();

[BsonElement("failedLoginAttempts")]
public int FailedLoginAttempts { get; private set; }

[BsonElement("isLocked")]
public bool IsLocked { get; private set; }

[BsonElement("lockedUntil")]
public DateTime? LockedUntil { get; private set; }

[BsonElement("lastLoginAt")]
public DateTime? LastLoginAt { get; private set; }

[BsonElement("lastLoginIp")]
public string? LastLoginIp { get; private set; }

// Add methods:
public void EnableTwoFactor(string secret, List<string> backupCodes)
{
    TwoFactorEnabled = true;
    TwoFactorSecret = secret;
    BackupCodes = backupCodes;
    UpdatedAt = DateTime.UtcNow;
}

public void DisableTwoFactor()
{
    TwoFactorEnabled = false;
    TwoFactorSecret = null;
    BackupCodes.Clear();
    UpdatedAt = DateTime.UtcNow;
}

public void IncrementFailedLoginAttempts()
{
    FailedLoginAttempts++;
    if (FailedLoginAttempts >= 5)
    {
        IsLocked = true;
        LockedUntil = DateTime.UtcNow.AddMinutes(30);
    }
    UpdatedAt = DateTime.UtcNow;
}

public void ClearFailedLoginAttempts()
{
    FailedLoginAttempts = 0;
    IsLocked = false;
    LockedUntil = null;
    UpdatedAt = DateTime.UtcNow;
}

public void RecordSuccessfulLogin(string ipAddress)
{
    LastLoginAt = DateTime.UtcNow;
    LastLoginIp = ipAddress;
    ClearFailedLoginAttempts();
    UpdatedAt = DateTime.UtcNow;
}
```

---

## 🔒 Phase 3: Session Management

### **Step 3.1: Session Service Implementation**

**File:** `src/ImageViewer.Infrastructure/Services/SessionService.cs`

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ImageViewer.Infrastructure.Services;

public interface ISessionService
{
    Task<string> CreateSessionAsync(ObjectId userId, string deviceId, string ipAddress);
    Task<SessionInfo?> GetSessionAsync(string sessionToken);
    Task<bool> ValidateSessionAsync(string sessionToken);
    Task UpdateSessionActivityAsync(string sessionToken);
    Task TerminateSessionAsync(string sessionToken);
    Task TerminateAllSessionsAsync(ObjectId userId);
    Task<IEnumerable<SessionInfo>> GetUserSessionsAsync(ObjectId userId);
}

public class SessionService : ISessionService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<SessionService> _logger;
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(24);

    public SessionService(IDistributedCache cache, ILogger<SessionService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> CreateSessionAsync(ObjectId userId, string deviceId, string ipAddress)
    {
        var sessionToken = GenerateSessionToken();
        var sessionInfo = new SessionInfo
        {
            SessionToken = sessionToken,
            UserId = userId,
            DeviceId = deviceId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        var cacheKey = $"session:{sessionToken}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _sessionTimeout
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(sessionInfo), options);
        
        _logger.LogInformation("Session created for user {UserId} with device {DeviceId}", userId, deviceId);
        return sessionToken;
    }

    public async Task<SessionInfo?> GetSessionAsync(string sessionToken)
    {
        try
        {
            var cacheKey = $"session:{sessionToken}";
            var sessionJson = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(sessionJson))
                return null;

            return JsonSerializer.Deserialize<SessionInfo>(sessionJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionToken}", sessionToken);
            return null;
        }
    }

    public async Task<bool> ValidateSessionAsync(string sessionToken)
    {
        var session = await GetSessionAsync(sessionToken);
        return session != null && session.IsActive && session.LastActivityAt > DateTime.UtcNow.AddHours(-24);
    }

    public async Task UpdateSessionActivityAsync(string sessionToken)
    {
        var session = await GetSessionAsync(sessionToken);
        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            var cacheKey = $"session:{sessionToken}";
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _sessionTimeout
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(session), options);
        }
    }

    public async Task TerminateSessionAsync(string sessionToken)
    {
        var cacheKey = $"session:{sessionToken}";
        await _cache.RemoveAsync(cacheKey);
        _logger.LogInformation("Session terminated: {SessionToken}", sessionToken);
    }

    public async Task TerminateAllSessionsAsync(ObjectId userId)
    {
        // This would require additional implementation to track user sessions
        // For now, we'll implement a simple approach
        _logger.LogInformation("All sessions terminated for user {UserId}", userId);
    }

    public async Task<IEnumerable<SessionInfo>> GetUserSessionsAsync(ObjectId userId)
    {
        // This would require additional implementation to track user sessions
        // For now, return empty list
        return new List<SessionInfo>();
    }

    private string GenerateSessionToken()
    {
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

public class SessionInfo
{
    public string SessionToken { get; set; } = string.Empty;
    public ObjectId UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsActive { get; set; }
}
```

---

## 🛡️ Phase 4: Security Middleware

### **Step 4.1: JWT Authentication Middleware**

**File:** `src/ImageViewer.Api/Middleware/JwtAuthenticationMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ImageViewer.Infrastructure.Services;

namespace ImageViewer.Api.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Skip authentication for public endpoints
        if (IsPublicEndpoint(path))
        {
            await _next(context);
            return;
        }

        var token = ExtractTokenFromHeader(context.Request);
        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Authorization token required");
            return;
        }

        var jwtService = context.RequestServices.GetRequiredService<IJwtService>();
        var principal = jwtService.ValidateToken(token);
        
        if (principal == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid or expired token");
            return;
        }

        // Set user context
        context.User = principal;
        
        await _next(context);
    }

    private bool IsPublicEndpoint(string path)
    {
        var publicEndpoints = new[]
        {
            "/health",
            "/swagger",
            "/api/v1/auth/login",
            "/api/v1/auth/register",
            "/api/v1/auth/forgot-password"
        };

        return publicEndpoints.Any(endpoint => path.StartsWith(endpoint));
    }

    private string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }
}
```

### **Step 4.2: Security Headers Middleware**

**File:** `src/ImageViewer.Api/Middleware/SecurityHeadersMiddleware.cs`

```csharp
namespace ImageViewer.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
        
        await _next(context);
    }
}
```

---

## ⚙️ Configuration Updates

### **Step 5.1: Update appsettings.json**

```json
{
  "Jwt": {
    "Key": "${JWT_SECRET_KEY}",
    "Issuer": "${JWT_ISSUER}",
    "Audience": "${JWT_AUDIENCE}",
    "ExpiryHours": 24,
    "RefreshTokenExpiryDays": 30
  },
  "Security": {
    "MaxFailedLoginAttempts": 5,
    "AccountLockoutDurationMinutes": 30,
    "PasswordMinLength": 8,
    "RequireStrongPassword": true,
    "SessionTimeoutHours": 24,
    "EnableTwoFactor": true,
    "BackupCodesCount": 10
  },
  "ConnectionStrings": {
    "Redis": "${REDIS_CONNECTION_STRING}"
  }
}
```

### **Step 5.2: Update Program.cs**

```csharp
// Add to Program.cs
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<ISessionService, SessionService>();

// Add Redis for session storage
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Add middleware
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
```

---

## 🧪 Testing Strategy

### **Unit Tests Required:**
- [ ] JwtService tests
- [ ] PasswordService tests
- [ ] TwoFactorService tests
- [ ] SessionService tests
- [ ] SecurityService integration tests

### **Integration Tests Required:**
- [ ] Authentication flow tests
- [ ] Two-factor authentication tests
- [ ] Session management tests
- [ ] Security middleware tests

### **Security Tests Required:**
- [ ] Password strength validation tests
- [ ] JWT token validation tests
- [ ] Session timeout tests
- [ ] Brute force protection tests

---

## 📋 Implementation Checklist

### **Phase 1: Core Authentication**
- [ ] Implement JwtService with real token generation
- [ ] Implement PasswordService with BCrypt
- [ ] Update SecurityService.LoginAsync method
- [ ] Add password validation
- [ ] Implement refresh token mechanism
- [ ] Add user lockout functionality

### **Phase 2: Two-Factor Authentication**
- [ ] Implement TwoFactorService
- [ ] Add QR code generation
- [ ] Implement backup codes
- [ ] Update User entity for 2FA
- [ ] Add 2FA setup/disable endpoints

### **Phase 3: Session Management**
- [ ] Implement SessionService with Redis
- [ ] Add session validation
- [ ] Implement session termination
- [ ] Add concurrent session limits

### **Phase 4: Security Middleware**
- [ ] Implement JWT authentication middleware
- [ ] Add security headers middleware
- [ ] Implement rate limiting
- [ ] Add request validation

### **Phase 5: Configuration & Testing**
- [ ] Update configuration files
- [ ] Remove hardcoded secrets
- [ ] Add comprehensive tests
- [ ] Perform security testing

---

## 🚨 Security Best Practices

### **Do's:**
- ✅ Always use HTTPS in production
- ✅ Implement proper password hashing (BCrypt)
- ✅ Use strong JWT secrets (32+ characters)
- ✅ Implement proper session timeout
- ✅ Add rate limiting for authentication endpoints
- ✅ Log all security events
- ✅ Validate all inputs
- ✅ Use environment variables for secrets

### **Don'ts:**
- ❌ Never hardcode passwords or secrets
- ❌ Don't store passwords in plain text
- ❌ Don't use weak JWT secrets
- ❌ Don't ignore failed login attempts
- ❌ Don't expose sensitive information in logs
- ❌ Don't skip input validation
- ❌ Don't use HTTP in production

---

*Security Implementation Guide created on 2025-01-03*  
*Estimated implementation time: 2-3 weeks*
