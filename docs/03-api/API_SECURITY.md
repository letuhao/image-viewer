# API Security - Image Viewer System

## Tổng quan Security

### Security Layers
```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Authentication & Authorization                             │
│  - JWT Tokens                                              │
│  - API Keys                                                │
│  - Role-based Access Control                               │
│  - Permission-based Access Control                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Network Layer                           │
├─────────────────────────────────────────────────────────────┤
│  HTTPS & TLS                                               │
│  - SSL/TLS Encryption                                      │
│  - Certificate Management                                  │
│  - HSTS Headers                                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                    │
├─────────────────────────────────────────────────────────────┤
│  Rate Limiting & DDoS Protection                           │
│  - Rate Limiting                                           │
│  - IP Whitelisting/Blacklisting                            │
│  - Request Validation                                      │
│  - Input Sanitization                                     │
└─────────────────────────────────────────────────────────────┘
```

## Authentication & Authorization

### 1. JWT Authentication

#### JWT Configuration
```csharp
public class JwtSettings
{
    public string SecretKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshExpirationDays { get; set; } = 7;
}

public void ConfigureServices(IServiceCollection services)
{
    var jwtSettings = Configuration.GetSection("Jwt").Get<JwtSettings>();
    
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
            
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });
}
```

#### JWT Token Service
```csharp
public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    bool ValidateToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;
    
    public JwtTokenService(IOptions<JwtSettings> jwtSettings, ILogger<JwtTokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }
    
    public string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        // Add roles
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }
        
        // Add permissions
        foreach (var permission in user.Permissions)
        {
            claims.Add(new Claim("permission", permission.Name));
        }
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = false
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }
        
        return principal;
    }
    
    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### 2. API Key Authentication

#### API Key Service
```csharp
public interface IApiKeyService
{
    Task<ApiKey> CreateApiKeyAsync(string name, string description, List<string> permissions);
    Task<ApiKey> GetApiKeyAsync(string key);
    Task<bool> ValidateApiKeyAsync(string key);
    Task RevokeApiKeyAsync(string key);
    Task<List<ApiKey>> GetUserApiKeysAsync(Guid userId);
}

public class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ILogger<ApiKeyService> _logger;
    
    public async Task<ApiKey> CreateApiKeyAsync(string name, string description, List<string> permissions)
    {
        var key = GenerateApiKey();
        var hashedKey = HashApiKey(key);
        
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            KeyHash = hashedKey,
            Permissions = permissions,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };
        
        await _apiKeyRepository.AddAsync(apiKey);
        
        _logger.LogInformation("API key {ApiKeyId} created for {Name}", apiKey.Id, name);
        
        return apiKey;
    }
    
    public async Task<bool> ValidateApiKeyAsync(string key)
    {
        var hashedKey = HashApiKey(key);
        var apiKey = await _apiKeyRepository.GetByHashAsync(hashedKey);
        
        if (apiKey == null || !apiKey.IsActive || apiKey.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }
        
        // Update last used
        apiKey.LastUsedAt = DateTime.UtcNow;
        await _apiKeyRepository.UpdateAsync(apiKey);
        
        return true;
    }
    
    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
    
    private string HashApiKey(string key)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hash);
    }
}
```

#### API Key Authentication Handler
```csharp
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyService _apiKeyService;
    
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IApiKeyService apiKeyService)
        : base(options, logger, encoder, clock)
    {
        _apiKeyService = apiKeyService;
    }
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }
        
        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.NoResult();
        }
        
        var apiKey = await _apiKeyService.GetApiKeyAsync(providedApiKey);
        
        if (apiKey == null || !apiKey.IsActive || apiKey.ExpiresAt < DateTime.UtcNow)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.Id.ToString()),
            new(ClaimTypes.Name, apiKey.Name),
            new("api_key", "true")
        };
        
        foreach (var permission in apiKey.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        
        return AuthenticateResult.Success(ticket);
    }
}
```

### 3. Role-based Access Control (RBAC)

#### Role and Permission Models
```csharp
public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<Permission> Permissions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Permission
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Resource { get; set; }
    public string Action { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public List<Role> Roles { get; set; }
    public List<Permission> Permissions { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### Authorization Policies
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthorization(options =>
    {
        // Collection policies
        options.AddPolicy("Collections.Read", policy =>
            policy.RequireClaim("permission", "collections:read"));
        
        options.AddPolicy("Collections.Write", policy =>
            policy.RequireClaim("permission", "collections:write"));
        
        options.AddPolicy("Collections.Delete", policy =>
            policy.RequireClaim("permission", "collections:delete"));
        
        // Image policies
        options.AddPolicy("Images.Read", policy =>
            policy.RequireClaim("permission", "images:read"));
        
        options.AddPolicy("Images.Write", policy =>
            policy.RequireClaim("permission", "images:write"));
        
        // Cache policies
        options.AddPolicy("Cache.Manage", policy =>
            policy.RequireClaim("permission", "cache:manage"));
        
        // Admin policy
        options.AddPolicy("Admin", policy =>
            policy.RequireRole("Admin"));
        
        // User policy
        options.AddPolicy("User", policy =>
            policy.RequireRole("User", "Admin"));
    });
}
```

#### Authorization Attributes
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CollectionsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Collections.Read")]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollections(
        [FromQuery] GetCollectionsQuery query)
    {
        // Implementation
    }
    
    [HttpPost]
    [Authorize(Policy = "Collections.Write")]
    public async Task<ActionResult<CollectionDto>> CreateCollection(
        [FromBody] CreateCollectionCommand command)
    {
        // Implementation
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy = "Collections.Write")]
    public async Task<ActionResult<CollectionDto>> UpdateCollection(
        Guid id, [FromBody] UpdateCollectionCommand command)
    {
        // Implementation
    }
    
    [HttpDelete("{id}")]
    [Authorize(Policy = "Collections.Delete")]
    public async Task<ActionResult> DeleteCollection(Guid id)
    {
        // Implementation
    }
}
```

## Input Validation & Sanitization

### 1. Model Validation

#### Validation Attributes
```csharp
public class CreateCollectionCommand
{
    [Required(ErrorMessage = "Collection name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Collection name must be between 1 and 255 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_]+$", ErrorMessage = "Collection name contains invalid characters")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "Collection path is required")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Collection path must be between 1 and 1000 characters")]
    [RegularExpression(@"^[a-zA-Z]:\\.*$", ErrorMessage = "Collection path must be a valid Windows path")]
    public string Path { get; set; }
    
    [Required(ErrorMessage = "Collection type is required")]
    [Range(0, 4, ErrorMessage = "Collection type must be between 0 and 4")]
    public CollectionType Type { get; set; }
    
    [ValidateComplexType]
    public CollectionSettings Settings { get; set; }
    
    [MaxLength(10, ErrorMessage = "Maximum 10 tags allowed")]
    public List<string> Tags { get; set; }
}

public class CollectionSettings
{
    [Range(1, 100, ErrorMessage = "Thumbnail quality must be between 1 and 100")]
    public int ThumbnailQuality { get; set; } = 80;
    
    [Range(1, 100, ErrorMessage = "Cache quality must be between 1 and 100")]
    public int CacheQuality { get; set; } = 85;
    
    [StringLength(10, ErrorMessage = "Cache format must be 10 characters or less")]
    [RegularExpression(@"^(jpeg|png|webp)$", ErrorMessage = "Cache format must be jpeg, png, or webp")]
    public string CacheFormat { get; set; } = "jpeg";
    
    [Range(1048576, 107374182400, ErrorMessage = "Max cache size must be between 1MB and 100GB")]
    public long MaxCacheSize { get; set; } = 10737418240; // 10GB
}
```

#### Custom Validation Attributes
```csharp
public class ValidImageFormatAttribute : ValidationAttribute
{
    private readonly string[] _allowedFormats = { "jpg", "jpeg", "png", "gif", "bmp", "tiff", "webp" };
    
    public override bool IsValid(object value)
    {
        if (value is string format)
        {
            return _allowedFormats.Contains(format.ToLowerInvariant());
        }
        return false;
    }
    
    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: {string.Join(", ", _allowedFormats)}";
    }
}

public class ValidPathAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        if (value is string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                return Directory.Exists(fullPath) || File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
    
    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a valid file or directory path";
    }
}
```

### 2. Input Sanitization

#### HTML Sanitization
```csharp
public interface IHtmlSanitizer
{
    string Sanitize(string html);
    string Sanitize(string html, string[] allowedTags);
}

public class HtmlSanitizer : IHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;
    
    public HtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
    }
    
    public string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;
        
        return _sanitizer.Sanitize(html);
    }
    
    public string Sanitize(string html, string[] allowedTags)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;
        
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in allowedTags)
        {
            sanitizer.AllowedTags.Add(tag);
        }
        
        return sanitizer.Sanitize(html);
    }
}
```

#### Path Sanitization
```csharp
public interface IPathSanitizer
{
    string SanitizePath(string path);
    bool IsPathSafe(string path);
}

public class PathSanitizer : IPathSanitizer
{
    private readonly string[] _dangerousPatterns = {
        "..", "~", "//", "\\", ":", "*", "?", "\"", "<", ">", "|"
    };
    
    public string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        
        // Remove dangerous patterns
        foreach (var pattern in _dangerousPatterns)
        {
            path = path.Replace(pattern, string.Empty);
        }
        
        // Normalize path
        path = Path.GetFullPath(path);
        
        return path;
    }
    
    public bool IsPathSafe(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        
        // Check for dangerous patterns
        foreach (var pattern in _dangerousPatterns)
        {
            if (path.Contains(pattern))
                return false;
        }
        
        // Check for path traversal
        if (path.Contains(".."))
            return false;
        
        return true;
    }
}
```

## Rate Limiting

### 1. Rate Limiting Configuration

#### Rate Limiting Service
```csharp
public interface IRateLimitingService
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
    Task<int> GetRemainingAsync(string key, int limit, TimeSpan window);
    Task<TimeSpan> GetResetTimeAsync(string key, TimeSpan window);
}

public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingService> _logger;
    
    public RateLimitingService(IMemoryCache cache, ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
    {
        var cacheKey = $"rate_limit:{key}";
        var now = DateTime.UtcNow;
        
        if (_cache.TryGetValue(cacheKey, out RateLimitInfo info))
        {
            if (now < info.ResetTime)
            {
                if (info.Count >= limit)
                {
                    _logger.LogWarning("Rate limit exceeded for key {Key}", key);
                    return false;
                }
                
                info.Count++;
                _cache.Set(cacheKey, info, info.ResetTime - now);
                return true;
            }
        }
        
        // Create new window
        var resetTime = now.Add(window);
        var newInfo = new RateLimitInfo
        {
            Count = 1,
            ResetTime = resetTime
        };
        
        _cache.Set(cacheKey, newInfo, window);
        return true;
    }
    
    public async Task<int> GetRemainingAsync(string key, int limit, TimeSpan window)
    {
        var cacheKey = $"rate_limit:{key}";
        
        if (_cache.TryGetValue(cacheKey, out RateLimitInfo info))
        {
            return Math.Max(0, limit - info.Count);
        }
        
        return limit;
    }
    
    public async Task<TimeSpan> GetResetTimeAsync(string key, TimeSpan window)
    {
        var cacheKey = $"rate_limit:{key}";
        
        if (_cache.TryGetValue(cacheKey, out RateLimitInfo info))
        {
            return info.ResetTime - DateTime.UtcNow;
        }
        
        return window;
    }
}

public class RateLimitInfo
{
    public int Count { get; set; }
    public DateTime ResetTime { get; set; }
}
```

#### Rate Limiting Middleware
```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitingService rateLimitingService,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var key = GetRateLimitKey(context);
        var limit = GetRateLimit(context);
        var window = TimeSpan.FromHours(1);
        
        if (!await _rateLimitingService.IsAllowedAsync(key, limit, window))
        {
            var remaining = await _rateLimitingService.GetRemainingAsync(key, limit, window);
            var resetTime = await _rateLimitingService.GetResetTimeAsync(key, window);
            
            context.Response.StatusCode = 429;
            context.Response.Headers.Add("X-RateLimit-Limit", limit.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", resetTime.TotalSeconds.ToString());
            
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        var remainingAfter = await _rateLimitingService.GetRemainingAsync(key, limit, window);
        var resetTimeAfter = await _rateLimitingService.GetResetTimeAsync(key, window);
        
        context.Response.Headers.Add("X-RateLimit-Limit", limit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", remainingAfter.ToString());
        context.Response.Headers.Add("X-RateLimit-Reset", resetTimeAfter.TotalSeconds.ToString());
        
        await _next(context);
    }
    
    private string GetRateLimitKey(HttpContext context)
    {
        var user = context.User.Identity.Name;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        
        if (!string.IsNullOrEmpty(user))
        {
            return $"user:{user}";
        }
        
        return $"ip:{ip}";
    }
    
    private int GetRateLimit(HttpContext context)
    {
        if (context.User.HasClaim("api_key", "true"))
        {
            return 10000; // API key limit
        }
        
        if (context.User.Identity.IsAuthenticated)
        {
            return 1000; // Authenticated user limit
        }
        
        return 100; // Anonymous user limit
    }
}
```

### 2. Rate Limiting Policies

#### Different Limits for Different Endpoints
```csharp
public class EndpointRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<EndpointRateLimitingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Request.Path.Value?.ToLowerInvariant();
        var key = GetRateLimitKey(context);
        
        var (limit, window) = GetRateLimitForEndpoint(endpoint);
        
        if (!await _rateLimitingService.IsAllowedAsync(key, limit, window))
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        await _next(context);
    }
    
    private (int limit, TimeSpan window) GetRateLimitForEndpoint(string endpoint)
    {
        return endpoint switch
        {
            "/api/v1/collections" => (100, TimeSpan.FromMinutes(1)),
            "/api/v1/images" => (200, TimeSpan.FromMinutes(1)),
            "/api/v1/cache/generate" => (10, TimeSpan.FromMinutes(5)),
            "/api/v1/collections/scan" => (5, TimeSpan.FromMinutes(10)),
            _ => (1000, TimeSpan.FromHours(1))
        };
    }
}
```

## Security Headers

### 1. Security Headers Middleware

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // HSTS
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        
        // X-Frame-Options
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // X-Content-Type-Options
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // X-XSS-Protection
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Referrer-Policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Content-Security-Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';");
        
        // Permissions-Policy
        context.Response.Headers.Add("Permissions-Policy", 
            "geolocation=(), " +
            "microphone=(), " +
            "camera=(), " +
            "payment=(), " +
            "usb=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "accelerometer=()");
        
        await _next(context);
    }
}
```

### 2. CORS Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.WithOrigins("https://app.imageviewer.com", "https://admin.imageviewer.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
        
        options.AddPolicy("ApiPolicy", policy =>
        {
            policy.WithOrigins("https://api.imageviewer.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseCors("DefaultPolicy");
}
```

## Logging & Monitoring

### 1. Security Logging

```csharp
public class SecurityLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityLoggingMiddleware> _logger;
    
    public SecurityLoggingMiddleware(RequestDelegate next, ILogger<SecurityLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            await _next(context);
        }
        finally
        {
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            
            // Log security events
            if (context.Response.StatusCode == 401)
            {
                _logger.LogWarning("Unauthorized access attempt from {IpAddress} to {Path}", 
                    context.Connection.RemoteIpAddress, context.Request.Path);
            }
            else if (context.Response.StatusCode == 403)
            {
                _logger.LogWarning("Forbidden access attempt from {IpAddress} to {Path}", 
                    context.Connection.RemoteIpAddress, context.Request.Path);
            }
            else if (context.Response.StatusCode == 429)
            {
                _logger.LogWarning("Rate limit exceeded from {IpAddress} to {Path}", 
                    context.Connection.RemoteIpAddress, context.Request.Path);
            }
            
            // Log all requests for security monitoring
            _logger.LogInformation("Request {Method} {Path} from {IpAddress} returned {StatusCode} in {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress,
                context.Response.StatusCode,
                duration.TotalMilliseconds);
        }
    }
}
```

### 2. Security Monitoring

```csharp
public interface ISecurityMonitoringService
{
    Task LogSecurityEventAsync(SecurityEvent securityEvent);
    Task<List<SecurityEvent>> GetSecurityEventsAsync(DateTime from, DateTime to);
    Task<bool> IsSuspiciousActivityAsync(string ipAddress);
}

public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ISecurityEventRepository _repository;
    private readonly ILogger<SecurityMonitoringService> _logger;
    
    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        await _repository.AddAsync(securityEvent);
        
        _logger.LogWarning("Security event: {EventType} from {IpAddress} at {Timestamp}",
            securityEvent.EventType,
            securityEvent.IpAddress,
            securityEvent.Timestamp);
    }
    
    public async Task<bool> IsSuspiciousActivityAsync(string ipAddress)
    {
        var recentEvents = await _repository.GetRecentEventsAsync(ipAddress, TimeSpan.FromHours(1));
        
        // Check for suspicious patterns
        var failedAttempts = recentEvents.Count(e => e.EventType == SecurityEventType.FailedLogin);
        var rateLimitExceeded = recentEvents.Count(e => e.EventType == SecurityEventType.RateLimitExceeded);
        var unauthorizedAccess = recentEvents.Count(e => e.EventType == SecurityEventType.UnauthorizedAccess);
        
        return failedAttempts > 5 || rateLimitExceeded > 10 || unauthorizedAccess > 3;
    }
}

public class SecurityEvent
{
    public Guid Id { get; set; }
    public SecurityEventType EventType { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string Path { get; set; }
    public string Method { get; set; }
    public string Details { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum SecurityEventType
{
    FailedLogin,
    UnauthorizedAccess,
    RateLimitExceeded,
    SuspiciousActivity,
    DataBreachAttempt,
    MaliciousRequest
}
```

## Advanced Security Features

### 1. Two-Factor Authentication (2FA)

#### TOTP Implementation
```csharp
public interface ITwoFactorService
{
    Task<TwoFactorSetupResult> SetupTwoFactorAsync(string userId, string method);
    Task<bool> VerifyTwoFactorAsync(string userId, string code, string method);
    Task<List<string>> GenerateBackupCodesAsync(string userId);
    Task<bool> ValidateBackupCodeAsync(string userId, string code);
}

public class TwoFactorService : ITwoFactorService
{
    private readonly IUserSecurityRepository _userSecurityRepository;
    private readonly IQRCodeGenerator _qrCodeGenerator;
    
    public async Task<TwoFactorSetupResult> SetupTwoFactorAsync(string userId, string method)
    {
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var qrCodeData = $"otpauth://totp/ImageViewer:{userId}?secret={secretKey}&issuer=ImageViewer";
        var qrCode = await _qrCodeGenerator.GenerateAsync(qrCodeData);
        
        var backupCodes = GenerateBackupCodes(10);
        
        var twoFactorInfo = new TwoFactorInfo
        {
            Enabled = false,
            Method = method,
            SecretKey = secretKey,
            BackupCodes = backupCodes,
            SetupDate = DateTime.UtcNow
        };
        
        await _userSecurityRepository.UpdateTwoFactorAsync(userId, twoFactorInfo);
        
        return new TwoFactorSetupResult
        {
            QRCode = qrCode,
            SecretKey = secretKey,
            BackupCodes = backupCodes
        };
    }
    
    public async Task<bool> VerifyTwoFactorAsync(string userId, string code, string method)
    {
        var userSecurity = await _userSecurityRepository.GetByUserIdAsync(userId);
        if (userSecurity?.TwoFactor?.Enabled != true)
            return false;
        
        var totp = new Totp(Encoding.UTF8.GetBytes(userSecurity.TwoFactor.SecretKey));
        var isValid = totp.VerifyTotp(code, out var timeStepMatched, new VerificationWindow(1, 1));
        
        if (isValid)
        {
            await RecordSecurityEventAsync(userId, "2fa_verified", "success");
        }
        else
        {
            await RecordSecurityEventAsync(userId, "2fa_failed", "failure");
        }
        
        return isValid;
    }
}
```

### 2. Device Management

#### Device Registration
```csharp
public interface IDeviceManagementService
{
    Task<Device> RegisterDeviceAsync(string userId, DeviceInfo deviceInfo);
    Task<List<Device>> GetUserDevicesAsync(string userId);
    Task<bool> TrustDeviceAsync(string userId, string deviceId);
    Task<bool> RevokeDeviceAsync(string userId, string deviceId);
    Task<bool> IsDeviceTrustedAsync(string userId, string deviceId);
}

public class DeviceManagementService : IDeviceManagementService
{
    private readonly IUserSecurityRepository _userSecurityRepository;
    private readonly IDeviceFingerprintService _fingerprintService;
    
    public async Task<Device> RegisterDeviceAsync(string userId, DeviceInfo deviceInfo)
    {
        var fingerprint = await _fingerprintService.GenerateFingerprintAsync(deviceInfo);
        
        var device = new Device
        {
            DeviceId = Guid.NewGuid().ToString(),
            Name = deviceInfo.Name,
            Type = deviceInfo.Type,
            OS = deviceInfo.OS,
            Browser = deviceInfo.Browser,
            IPAddress = deviceInfo.IPAddress,
            Location = deviceInfo.Location,
            Fingerprint = fingerprint,
            IsTrusted = false,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            UserAgent = deviceInfo.UserAgent
        };
        
        await _userSecurityRepository.AddDeviceAsync(userId, device);
        await RecordSecurityEventAsync(userId, "device_registered", "info", device);
        
        return device;
    }
    
    public async Task<bool> TrustDeviceAsync(string userId, string deviceId)
    {
        var device = await _userSecurityRepository.GetDeviceAsync(userId, deviceId);
        if (device == null) return false;
        
        device.IsTrusted = true;
        device.TrustedAt = DateTime.UtcNow;
        
        await _userSecurityRepository.UpdateDeviceAsync(userId, device);
        await RecordSecurityEventAsync(userId, "device_trusted", "info", device);
        
        return true;
    }
}
```

### 3. Risk Assessment

#### Risk Scoring Algorithm
```csharp
public interface IRiskAssessmentService
{
    Task<RiskScore> CalculateRiskScoreAsync(string userId, SecurityEvent securityEvent);
    Task<List<RiskFactor>> AnalyzeRiskFactorsAsync(string userId);
    Task<bool> IsHighRiskUserAsync(string userId);
}

public class RiskAssessmentService : IRiskAssessmentService
{
    private readonly IUserSecurityRepository _userSecurityRepository;
    private readonly IGeolocationService _geolocationService;
    
    public async Task<RiskScore> CalculateRiskScoreAsync(string userId, SecurityEvent securityEvent)
    {
        var riskFactors = new List<RiskFactor>();
        var userSecurity = await _userSecurityRepository.GetByUserIdAsync(userId);
        
        // Analyze login location
        if (securityEvent.Type == "login")
        {
            var locationRisk = await AnalyzeLocationRiskAsync(userId, securityEvent.Location);
            if (locationRisk > 0)
            {
                riskFactors.Add(new RiskFactor("unusual_location", locationRisk, 0.3, "Login from unusual location"));
            }
        }
        
        // Analyze device
        if (securityEvent.Device != null)
        {
            var deviceRisk = AnalyzeDeviceRiskAsync(userSecurity, securityEvent.Device);
            if (deviceRisk > 0)
            {
                riskFactors.Add(new RiskFactor("untrusted_device", deviceRisk, 0.4, "Login from untrusted device"));
            }
        }
        
        // Analyze time patterns
        var timeRisk = AnalyzeTimeRiskAsync(userSecurity, securityEvent.Timestamp);
        if (timeRisk > 0)
        {
            riskFactors.Add(new RiskFactor("unusual_time", timeRisk, 0.2, "Login at unusual time"));
        }
        
        // Analyze frequency
        var frequencyRisk = await AnalyzeFrequencyRiskAsync(userId, securityEvent);
        if (frequencyRisk > 0)
        {
            riskFactors.Add(new RiskFactor("high_frequency", frequencyRisk, 0.3, "High frequency of security events"));
        }
        
        var totalScore = (int)riskFactors.Sum(f => f.Score * f.Weight);
        
        return new RiskScore(totalScore, riskFactors, DateTime.UtcNow);
    }
    
    private async Task<int> AnalyzeLocationRiskAsync(string userId, string location)
    {
        var userSecurity = await _userSecurityRepository.GetByUserIdAsync(userId);
        var recentLocations = userSecurity.LoginHistory
            .Where(h => h.Timestamp > DateTime.UtcNow.AddDays(-30))
            .Select(h => h.Location)
            .Distinct()
            .ToList();
        
        if (!recentLocations.Contains(location))
        {
            return 30; // New location
        }
        
        return 0;
    }
    
    private int AnalyzeDeviceRiskAsync(UserSecurity userSecurity, Device device)
    {
        if (!userSecurity.Devices.Any(d => d.DeviceId == device.DeviceId))
        {
            return 40; // New device
        }
        
        var existingDevice = userSecurity.Devices.First(d => d.DeviceId == device.DeviceId);
        if (!existingDevice.IsTrusted)
        {
            return 20; // Untrusted device
        }
        
        return 0;
    }
}
```

### 4. Content Moderation Security

#### AI-Powered Content Analysis
```csharp
public interface IContentModerationService
{
    Task<ModerationResult> AnalyzeContentAsync(ContentAnalysisRequest request);
    Task<bool> IsContentAppropriateAsync(string content, string contentType);
    Task<List<string>> ExtractInappropriateElementsAsync(string content);
}

public class ContentModerationService : IContentModerationService
{
    private readonly IAIAnalysisService _aiAnalysisService;
    private readonly IContentPolicyService _policyService;
    
    public async Task<ModerationResult> AnalyzeContentAsync(ContentAnalysisRequest request)
    {
        var aiAnalysis = await _aiAnalysisService.AnalyzeAsync(request);
        var policyCheck = await _policyService.CheckPoliciesAsync(request);
        
        var result = new ModerationResult
        {
            IsAppropriate = aiAnalysis.IsAppropriate && policyCheck.IsCompliant,
            Confidence = aiAnalysis.Confidence,
            Categories = aiAnalysis.Categories,
            Violations = policyCheck.Violations,
            Recommendations = GenerateRecommendations(aiAnalysis, policyCheck)
        };
        
        return result;
    }
    
    public async Task<bool> IsContentAppropriateAsync(string content, string contentType)
    {
        var request = new ContentAnalysisRequest
        {
            Content = content,
            ContentType = contentType,
            AnalysisTypes = new[] { "inappropriate_content", "spam", "hate_speech" }
        };
        
        var result = await AnalyzeContentAsync(request);
        return result.IsAppropriate;
    }
}
```

### 5. Copyright Protection

#### DMCA Management
```csharp
public interface ICopyrightManagementService
{
    Task<DMCAReport> ProcessDMCAReportAsync(DMCAReportRequest request);
    Task<bool> IsContentCopyrightedAsync(string contentId);
    Task<List<CopyrightViolation>> CheckCopyrightViolationsAsync(string contentId);
}

public class CopyrightManagementService : ICopyrightManagementService
{
    private readonly ICopyrightDetectionService _detectionService;
    private readonly IDMCAProcessor _dmcaProcessor;
    
    public async Task<DMCAReport> ProcessDMCAReportAsync(DMCAReportRequest request)
    {
        // Validate the DMCA report
        var validation = await ValidateDMCAReportAsync(request);
        if (!validation.IsValid)
        {
            throw new InvalidDMCAReportException(validation.Errors);
        }
        
        // Process the report
        var report = new DMCAReport
        {
            ReportId = Guid.NewGuid().ToString(),
            ReporterId = request.ReporterId,
            ContentId = request.ContentId,
            Reason = request.Reason,
            Description = request.Description,
            Status = DMCAStatus.Pending,
            ReportedAt = DateTime.UtcNow
        };
        
        await _dmcaProcessor.ProcessReportAsync(report);
        
        return report;
    }
    
    public async Task<bool> IsContentCopyrightedAsync(string contentId)
    {
        var copyrightInfo = await _detectionService.DetectCopyrightAsync(contentId);
        return copyrightInfo.HasCopyright;
    }
}
```

### 6. Advanced Monitoring

#### Real-time Security Monitoring
```csharp
public interface ISecurityMonitoringService
{
    Task MonitorSecurityEventsAsync();
    Task<List<SecurityAlert>> GetActiveAlertsAsync();
    Task<bool> IsSystemUnderAttackAsync();
}

public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ISecurityEventRepository _eventRepository;
    private readonly IAlertService _alertService;
    
    public async Task MonitorSecurityEventsAsync()
    {
        var recentEvents = await _eventRepository.GetRecentEventsAsync(TimeSpan.FromMinutes(5));
        
        // Analyze patterns
        var suspiciousPatterns = AnalyzeSuspiciousPatterns(recentEvents);
        
        foreach (var pattern in suspiciousPatterns)
        {
            await _alertService.CreateAlertAsync(new SecurityAlert
            {
                Type = pattern.Type,
                Severity = pattern.Severity,
                Description = pattern.Description,
                AffectedUsers = pattern.AffectedUsers,
                Timestamp = DateTime.UtcNow
            });
        }
    }
    
    public async Task<bool> IsSystemUnderAttackAsync()
    {
        var recentEvents = await _eventRepository.GetRecentEventsAsync(TimeSpan.FromMinutes(10));
        
        var attackIndicators = new[]
        {
            recentEvents.Count(e => e.Type == "brute_force_attack") > 50,
            recentEvents.Count(e => e.Type == "ddos_attack") > 1000,
            recentEvents.Count(e => e.Type == "sql_injection_attempt") > 10
        };
        
        return attackIndicators.Any(indicator => indicator);
    }
}
```

## Conclusion

Security implementation đảm bảo:

1. **Authentication**: JWT tokens, 2FA, và device management
2. **Authorization**: Role-based và permission-based access control
3. **Input Validation**: Comprehensive validation và sanitization
4. **Rate Limiting**: Protection against abuse và DDoS
5. **Security Headers**: Protection against common attacks
6. **Logging & Monitoring**: Security event tracking và monitoring
7. **Advanced Security**: Risk assessment, content moderation, copyright protection
8. **Real-time Monitoring**: Threat detection và automated response

Security strategy này đảm bảo hệ thống được bảo vệ khỏi các threats phổ biến và có thể detect suspicious activities với advanced AI-powered analysis.
