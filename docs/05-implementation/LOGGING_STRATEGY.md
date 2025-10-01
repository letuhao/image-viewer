# Logging Strategy - Image Viewer System

## Tổng quan Logging Strategy

### Logging Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Serilog + Structured Logging                              │
│  - Request/Response Logging                                 │
│  - Performance Logging                                      │
│  - Error Logging                                            │
│  - Security Logging                                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Logging Infrastructure                   │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Console   │  │    File     │  │   Database  │        │
│  │   Logging   │  │   Logging   │  │   Logging   │        │
│  │             │  │             │  │             │        │
│  │ - Debug     │  │ - Daily     │  │ - Errors    │        │
│  │ - Info      │  │ - Rotated   │  │ - Security  │        │
│  │ - Warnings  │  │ - Compressed│  │ - Audit     │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## Serilog Configuration

### 1. Serilog Setup

#### Program.cs Configuration
```csharp
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ImageViewer")
    .Enrich.WithProperty("Version", "1.0.0")
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/imageviewer-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
        rollOnFileSizeLimit: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        tableName: "application_logs",
        columnWriters: new List<ITextFormatter>
        {
            new RenderedMessageColumnWriter(),
            new MessageTemplateColumnWriter(),
            new LevelColumnWriter(),
            new TimeStampColumnWriter(),
            new ExceptionColumnWriter(),
            new PropertiesColumnWriter()
        })
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Ensure logs directory exists
Directory.CreateDirectory("logs");

app.Run();
```

#### appsettings.json Configuration
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/imageviewer-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 104857600,
          "rollOnFileSizeLimit": true,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "PostgreSQL",
        "Args": {
          "connectionString": "Host=localhost;Database=imageviewer;Username=postgres;Password=password",
          "tableName": "application_logs",
          "restrictedToMinimumLevel": "Warning"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId",
      "WithProcessId"
    ],
    "Properties": {
      "Application": "ImageViewer",
      "Version": "1.0.0"
    }
  }
}
```

### 2. Database Logging Table

#### PostgreSQL Logging Table
```sql
CREATE TABLE application_logs (
    id SERIAL PRIMARY KEY,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    level VARCHAR(10) NOT NULL,
    message TEXT NOT NULL,
    message_template TEXT,
    exception TEXT,
    properties JSONB,
    machine_name VARCHAR(255),
    thread_id INTEGER,
    process_id INTEGER,
    application VARCHAR(100),
    version VARCHAR(20),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX IX_application_logs_timestamp ON application_logs (timestamp);
CREATE INDEX IX_application_logs_level ON application_logs (level);
CREATE INDEX IX_application_logs_application ON application_logs (application);
CREATE INDEX IX_application_logs_machine_name ON application_logs (machine_name);

-- Partitioning by month for better performance
CREATE TABLE application_logs_y2024m01 PARTITION OF application_logs
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

CREATE TABLE application_logs_y2024m02 PARTITION OF application_logs
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');
```

## Structured Logging Implementation

### 1. Request/Response Logging Middleware

```csharp
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    
    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;
        
        var stopwatch = Stopwatch.StartNew();
        
        // Log request
        await LogRequestAsync(context, requestId);
        
        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);
            
            // Restore response body
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
    
    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        var request = context.Request;
        
        _logger.LogInformation(
            "Request {RequestId} {Method} {Path} from {IpAddress} UserAgent: {UserAgent}",
            requestId,
            request.Method,
            request.Path,
            context.Connection.RemoteIpAddress?.ToString(),
            request.Headers.UserAgent.ToString());
        
        // Log request body for POST/PUT requests
        if (request.Method is "POST" or "PUT" or "PATCH")
        {
            request.EnableBuffering();
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Position = 0;
            
            _logger.LogDebug(
                "Request {RequestId} Body: {RequestBody}",
                requestId,
                body);
        }
        
        // Log query parameters
        if (request.QueryString.HasValue)
        {
            _logger.LogDebug(
                "Request {RequestId} Query: {QueryString}",
                requestId,
                request.QueryString.ToString());
        }
    }
    
    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs)
    {
        var response = context.Response;
        
        _logger.LogInformation(
            "Response {RequestId} {StatusCode} in {ElapsedMs}ms",
            requestId,
            response.StatusCode,
            elapsedMs);
        
        // Log response body for errors
        if (response.StatusCode >= 400)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            
            _logger.LogWarning(
                "Response {RequestId} Error Body: {ResponseBody}",
                requestId,
                body);
        }
        
        // Log performance warnings
        if (elapsedMs > 1000)
        {
            _logger.LogWarning(
                "Slow request {RequestId} took {ElapsedMs}ms",
                requestId,
                elapsedMs);
        }
    }
}
```

### 2. Performance Logging

```csharp
public class PerformanceLoggingService
{
    private readonly ILogger<PerformanceLoggingService> _logger;
    
    public PerformanceLoggingService(ILogger<PerformanceLoggingService> logger)
    {
        _logger = logger;
    }
    
    public async Task<T> LogExecutionTimeAsync<T>(string operation, Func<Task<T>> func)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await func();
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Operation {Operation} completed in {ElapsedMs}ms",
                operation,
                stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Operation {Operation} failed after {ElapsedMs}ms",
                operation,
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
    
    public async Task LogExecutionTimeAsync(string operation, Func<Task> func)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await func();
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Operation {Operation} completed in {ElapsedMs}ms",
                operation,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Operation {Operation} failed after {ElapsedMs}ms",
                operation,
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}
```

### 3. Database Query Logging

```csharp
public class DatabaseLoggingService
{
    private readonly ILogger<DatabaseLoggingService> _logger;
    
    public DatabaseLoggingService(ILogger<DatabaseLoggingService> logger)
    {
        _logger = logger;
    }
    
    public void LogQuery(string query, object parameters, long elapsedMs)
    {
        _logger.LogDebug(
            "Database Query executed in {ElapsedMs}ms: {Query} with parameters {Parameters}",
            elapsedMs,
            query,
            JsonSerializer.Serialize(parameters));
        
        if (elapsedMs > 1000)
        {
            _logger.LogWarning(
                "Slow database query took {ElapsedMs}ms: {Query}",
                elapsedMs,
                query);
        }
    }
    
    public void LogQueryError(string query, object parameters, Exception exception)
    {
        _logger.LogError(exception,
            "Database Query failed: {Query} with parameters {Parameters}",
            query,
            JsonSerializer.Serialize(parameters));
    }
}

// EF Core Query Logging
public class ImageViewerDbContext : DbContext
{
    private readonly ILogger<ImageViewerDbContext> _logger;
    
    public ImageViewerDbContext(DbContextOptions<ImageViewerDbContext> options, ILogger<ImageViewerDbContext> logger)
        : base(options)
    {
        _logger = logger;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(
            message => _logger.LogDebug("EF Core: {Message}", message),
            LogLevel.Information);
        
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
    }
}
```

### 4. Image Processing Logging

```csharp
public class ImageProcessingLoggingService
{
    private readonly ILogger<ImageProcessingLoggingService> _logger;
    
    public ImageProcessingLoggingService(ILogger<ImageProcessingLoggingService> logger)
    {
        _logger = logger;
    }
    
    public void LogImageProcessingStart(Guid imageId, string operation, ImageProcessingOptions options)
    {
        _logger.LogInformation(
            "Image processing started for {ImageId} operation {Operation} with options {Options}",
            imageId,
            operation,
            JsonSerializer.Serialize(options));
    }
    
    public void LogImageProcessingComplete(Guid imageId, string operation, long elapsedMs, long outputSize)
    {
        _logger.LogInformation(
            "Image processing completed for {ImageId} operation {Operation} in {ElapsedMs}ms output size {OutputSize}bytes",
            imageId,
            operation,
            elapsedMs,
            outputSize);
    }
    
    public void LogImageProcessingError(Guid imageId, string operation, Exception exception)
    {
        _logger.LogError(exception,
            "Image processing failed for {ImageId} operation {Operation}",
            imageId,
            operation);
    }
    
    public void LogCacheHit(Guid imageId, string cachePath)
    {
        _logger.LogDebug(
            "Cache hit for {ImageId} at {CachePath}",
            imageId,
            cachePath);
    }
    
    public void LogCacheMiss(Guid imageId, string reason)
    {
        _logger.LogDebug(
            "Cache miss for {ImageId} reason: {Reason}",
            imageId,
            reason);
    }
}
```

### 5. Background Job Logging

```csharp
public class BackgroundJobLoggingService
{
    private readonly ILogger<BackgroundJobLoggingService> _logger;
    
    public BackgroundJobLoggingService(ILogger<BackgroundJobLoggingService> logger)
    {
        _logger = logger;
    }
    
    public void LogJobStarted(string jobId, string jobType, object parameters)
    {
        _logger.LogInformation(
            "Background job {JobId} of type {JobType} started with parameters {Parameters}",
            jobId,
            jobType,
            JsonSerializer.Serialize(parameters));
    }
    
    public void LogJobProgress(string jobId, int completed, int total, string currentItem)
    {
        var percentage = (double)completed / total * 100;
        
        _logger.LogInformation(
            "Background job {JobId} progress: {Completed}/{Total} ({Percentage:F1}%) current item: {CurrentItem}",
            jobId,
            completed,
            total,
            percentage,
            currentItem);
    }
    
    public void LogJobCompleted(string jobId, string jobType, long elapsedMs)
    {
        _logger.LogInformation(
            "Background job {JobId} of type {JobType} completed in {ElapsedMs}ms",
            jobId,
            jobType,
            elapsedMs);
    }
    
    public void LogJobFailed(string jobId, string jobType, Exception exception)
    {
        _logger.LogError(exception,
            "Background job {JobId} of type {JobType} failed",
            jobId,
            jobType);
    }
    
    public void LogJobCancelled(string jobId, string jobType)
    {
        _logger.LogWarning(
            "Background job {JobId} of type {JobType} was cancelled",
            jobId,
            jobType);
    }
}
```

## Error Logging & Exception Handling

### 1. Global Exception Handler

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
        
        _logger.LogError(exception,
            "Unhandled exception {RequestId} {Method} {Path} from {IpAddress}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress?.ToString());
        
        var response = new
        {
            success = false,
            error = new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "An internal server error occurred",
                requestId = requestId
            },
            timestamp = DateTime.UtcNow
        };
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### 2. Custom Exception Types

```csharp
public class ImageViewerException : Exception
{
    public string ErrorCode { get; }
    public object Details { get; }
    
    public ImageViewerException(string errorCode, string message, object details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
    
    public ImageViewerException(string errorCode, string message, Exception innerException, object details = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}

public class ValidationException : ImageViewerException
{
    public ValidationException(string message, object details = null)
        : base("VALIDATION_ERROR", message, details)
    {
    }
}

public class NotFoundException : ImageViewerException
{
    public NotFoundException(string resourceType, object resourceId)
        : base("RESOURCE_NOT_FOUND", $"{resourceType} with ID {resourceId} not found", new { resourceType, resourceId })
    {
    }
}

public class UnauthorizedException : ImageViewerException
{
    public UnauthorizedException(string message = "Unauthorized access")
        : base("UNAUTHORIZED", message)
    {
    }
}
```

## Security Logging

### 1. Security Event Logging

```csharp
public class SecurityLoggingService
{
    private readonly ILogger<SecurityLoggingService> _logger;
    
    public SecurityLoggingService(ILogger<SecurityLoggingService> logger)
    {
        _logger = logger;
    }
    
    public void LogAuthenticationAttempt(string username, string ipAddress, bool success)
    {
        _logger.LogInformation(
            "Authentication attempt for {Username} from {IpAddress} result: {Success}",
            username,
            ipAddress,
            success ? "SUCCESS" : "FAILED");
    }
    
    public void LogAuthorizationFailure(string username, string resource, string action, string ipAddress)
    {
        _logger.LogWarning(
            "Authorization failure for {Username} attempting {Action} on {Resource} from {IpAddress}",
            username,
            action,
            resource,
            ipAddress);
    }
    
    public void LogRateLimitExceeded(string ipAddress, string endpoint)
    {
        _logger.LogWarning(
            "Rate limit exceeded for {IpAddress} on endpoint {Endpoint}",
            ipAddress,
            endpoint);
    }
    
    public void LogSuspiciousActivity(string ipAddress, string activity, object details)
    {
        _logger.LogWarning(
            "Suspicious activity detected from {IpAddress} activity: {Activity} details: {Details}",
            ipAddress,
            activity,
            JsonSerializer.Serialize(details));
    }
}
```

## Logging Configuration for Different Environments

### 1. Development Environment

```csharp
public static class LoggingConfiguration
{
    public static void ConfigureDevelopmentLogging(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", "Development")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/dev/imageviewer-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();
    }
}
```

### 2. Production Environment

```csharp
public static void ConfigureProductionLogging(WebApplicationBuilder builder)
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Environment", "Production")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/prod/imageviewer-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 100 * 1024 * 1024,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.PostgreSQL(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
            tableName: "application_logs",
            restrictedToMinimumLevel: LogEventLevel.Warning)
        .CreateLogger();
}
```

## Log Analysis & Monitoring

### 1. Log Query Examples

```sql
-- Find slow requests
SELECT timestamp, message, properties
FROM application_logs
WHERE level = 'Warning'
  AND message LIKE '%Slow request%'
ORDER BY timestamp DESC
LIMIT 100;

-- Find errors by application
SELECT timestamp, message, exception, machine_name
FROM application_logs
WHERE level = 'Error'
  AND application = 'ImageViewer'
ORDER BY timestamp DESC
LIMIT 50;

-- Find security events
SELECT timestamp, message, properties
FROM application_logs
WHERE level = 'Warning'
  AND (message LIKE '%Authentication%' OR message LIKE '%Authorization%' OR message LIKE '%Rate limit%')
ORDER BY timestamp DESC
LIMIT 100;

-- Performance analysis
SELECT 
    DATE_TRUNC('hour', timestamp) as hour,
    COUNT(*) as request_count,
    AVG(CAST(properties->>'ElapsedMs' AS INTEGER)) as avg_response_time
FROM application_logs
WHERE level = 'Information'
  AND message LIKE '%Response%'
GROUP BY hour
ORDER BY hour DESC;
```

### 2. Log Monitoring Dashboard

```csharp
public class LogMonitoringService
{
    private readonly ILogger<LogMonitoringService> _logger;
    private readonly ImageViewerDbContext _context;
    
    public LogMonitoringService(ILogger<LogMonitoringService> logger, ImageViewerDbContext context)
    {
        _logger = logger;
        _context = context;
    }
    
    public async Task<LogStatistics> GetLogStatisticsAsync(DateTime from, DateTime to)
    {
        var logs = await _context.ApplicationLogs
            .Where(l => l.Timestamp >= from && l.Timestamp <= to)
            .ToListAsync();
        
        return new LogStatistics
        {
            TotalLogs = logs.Count,
            ErrorCount = logs.Count(l => l.Level == "Error"),
            WarningCount = logs.Count(l => l.Level == "Warning"),
            InfoCount = logs.Count(l => l.Level == "Information"),
            DebugCount = logs.Count(l => l.Level == "Debug"),
            AverageResponseTime = logs
                .Where(l => l.Message.Contains("Response"))
                .Select(l => JsonSerializer.Deserialize<ResponseLog>(l.Properties))
                .Where(r => r != null)
                .Average(r => r.ElapsedMs)
        };
    }
}
```

## Conclusion

Logging strategy này đảm bảo:

1. **Comprehensive Logging**: Log tất cả aspects của hệ thống
2. **Structured Logging**: Sử dụng Serilog với structured data
3. **Performance Monitoring**: Track performance metrics và slow operations
4. **Security Monitoring**: Log security events và suspicious activities
5. **Error Tracking**: Comprehensive error logging và exception handling
6. **Debugging Support**: Detailed logging để debug issues
7. **Production Ready**: Optimized cho production environment

Strategy này sẽ giúp debug và monitor hệ thống một cách hiệu quả.
