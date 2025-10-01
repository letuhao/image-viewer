# PostgreSQL Setup - Image Viewer System

## Tổng quan PostgreSQL Configuration

### PostgreSQL Version
- **Version**: PostgreSQL 15+
- **Extensions**: uuid-ossp, pg_stat_statements
- **Connection Pooling**: Npgsql with connection pooling
- **Performance**: Optimized for image metadata and cache operations

## Database Setup

### 1. Database Creation

#### Create Database Script
```sql
-- Create database
CREATE DATABASE imageviewer
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- Connect to database
\c imageviewer;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
CREATE EXTENSION IF NOT EXISTS "btree_gin";
CREATE EXTENSION IF NOT EXISTS "btree_gist";
```

### 2. Connection String Configuration

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=imageviewer;Username=postgres;Password=your_password;Port=5432;Pooling=true;MinPoolSize=5;MaxPoolSize=100;CommandTimeout=30;Timeout=15;"
  },
  "PostgreSQL": {
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "MaxRetryDelay": "00:00:30",
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "CommandTimeout": 30
  }
}
```

#### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=imageviewer_dev;Username=postgres;Password=your_password;Port=5432;Pooling=true;MinPoolSize=2;MaxPoolSize=20;CommandTimeout=60;Timeout=30;"
  },
  "PostgreSQL": {
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "MaxRetryDelay": "00:00:30",
    "EnableSensitiveDataLogging": true,
    "EnableDetailedErrors": true,
    "CommandTimeout": 60
  }
}
```

### 3. EF Core Configuration

#### DbContext Configuration
```csharp
public class ImageViewerDbContext : DbContext
{
    private readonly ILogger<ImageViewerDbContext> _logger;
    
    public ImageViewerDbContext(DbContextOptions<ImageViewerDbContext> options, ILogger<ImageViewerDbContext> logger)
        : base(options)
    {
        _logger = logger;
    }
    
    public DbSet<Collection> Collections { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<CollectionTag> CollectionTags { get; set; }
    public DbSet<CacheInfo> CacheInfos { get; set; }
    public DbSet<CacheFolder> CacheFolders { get; set; }
    public DbSet<CollectionCacheBinding> CollectionCacheBindings { get; set; }
    public DbSet<CollectionStatistics> CollectionStatistics { get; set; }
    public DbSet<ViewSession> ViewSessions { get; set; }
    public DbSet<SearchLog> SearchLogs { get; set; }
    public DbSet<BackgroundJob> BackgroundJobs { get; set; }
    public DbSet<ApplicationLog> ApplicationLogs { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
                ?? "Host=localhost;Database=imageviewer;Username=postgres;Password=password;Port=5432";
            
            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30));
                
                npgsqlOptions.CommandTimeout(30);
            });
            
            // Enable logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                optionsBuilder.LogTo(
                    message => _logger.LogDebug("EF Core: {Message}", message),
                    LogLevel.Information);
                
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
            }
        }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure PostgreSQL-specific settings
        modelBuilder.HasDefaultSchema("public");
        
        // Collection configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.SettingsJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);
            
            entity.HasMany(e => e.Images)
                  .WithOne(i => i.Collection)
                  .HasForeignKey(i => i.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasMany(e => e.Tags)
                  .WithOne(t => t.Collection)
                  .HasForeignKey(t => t.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Statistics)
                  .WithOne(s => s.Collection)
                  .HasForeignKey<CollectionStatistics>(s => s.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Image configuration
        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Filename).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RelativePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.MetadataJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.CollectionId);
            entity.HasIndex(e => e.Filename);
            entity.HasIndex(e => new { e.CollectionId, e.Filename }).IsUnique();
            entity.HasIndex(e => e.FileSize);
            entity.HasIndex(e => e.Width);
            entity.HasIndex(e => e.Height);
            entity.HasIndex(e => e.Format);
            entity.HasIndex(e => e.IsDeleted);
            
            entity.HasOne(e => e.CacheInfo)
                  .WithOne(c => c.Image)
                  .HasForeignKey<CacheInfo>(c => c.ImageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // CacheInfo configuration
        modelBuilder.Entity<CacheInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CachePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Dimensions).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CachedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.ImageId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsValid);
            entity.HasIndex(e => e.CachedAt);
        });
        
        // ApplicationLog configuration
        modelBuilder.Entity<ApplicationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Properties).HasColumnType("jsonb");
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.Application);
            entity.HasIndex(e => e.MachineName);
        });
    }
}
```

### 4. Service Registration

#### Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add PostgreSQL DbContext
builder.Services.AddDbContext<ImageViewerDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: builder.Configuration.GetValue<int>("PostgreSQL:MaxRetryCount", 3),
            maxRetryDelay: TimeSpan.Parse(builder.Configuration.GetValue<string>("PostgreSQL:MaxRetryDelay", "00:00:30")));
        
        npgsqlOptions.CommandTimeout(builder.Configuration.GetValue<int>("PostgreSQL:CommandTimeout", 30));
    });
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add connection pool configuration
builder.Services.Configure<NpgsqlDbContextOptions>(options =>
{
    options.CommandTimeout(builder.Configuration.GetValue<int>("PostgreSQL:CommandTimeout", 30));
    options.EnableRetryOnFailure(builder.Configuration.GetValue<int>("PostgreSQL:MaxRetryCount", 3));
});

var app = builder.Build();
```

## Performance Optimization

### 1. Connection Pooling

#### Connection Pool Configuration
```csharp
public class DatabaseConfiguration
{
    public static void ConfigureConnectionPooling(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContextPool<ImageViewerDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Connection pool settings
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30));
                
                npgsqlOptions.CommandTimeout(30);
            });
        }, poolSize: 100); // Pool size
    }
}
```

### 2. Query Optimization

#### Compiled Queries
```csharp
public static class CompiledQueries
{
    public static readonly Func<ImageViewerDbContext, Guid, IAsyncEnumerable<Image>> GetImagesByCollectionId =
        EF.CompileAsyncQuery((ImageViewerDbContext context, Guid collectionId) =>
            context.Images
                .Where(i => i.CollectionId == collectionId && !i.IsDeleted)
                .OrderBy(i => i.Filename));

    public static readonly Func<ImageViewerDbContext, Guid, Task<Collection>> GetCollectionById =
        EF.CompileAsyncQuery((ImageViewerDbContext context, Guid id) =>
            context.Collections
                .Include(c => c.Statistics)
                .Include(c => c.Tags)
                .FirstOrDefault(c => c.Id == id && !c.IsDeleted));

    public static readonly Func<ImageViewerDbContext, Guid, Task<CacheInfo>> GetCacheInfoByImageId =
        EF.CompileAsyncQuery((ImageViewerDbContext context, Guid imageId) =>
            context.CacheInfos
                .FirstOrDefault(c => c.ImageId == imageId && c.IsValid && c.ExpiresAt > DateTime.UtcNow));
}
```

### 3. Index Optimization

#### Performance Indexes
```sql
-- Composite indexes for common queries
CREATE INDEX CONCURRENTLY IX_Images_CollectionId_Format_FileSize 
ON Images (CollectionId, Format, FileSize);

CREATE INDEX CONCURRENTLY IX_Images_CollectionId_Width_Height 
ON Images (CollectionId, Width, Height);

CREATE INDEX CONCURRENTLY IX_CacheInfo_ImageId_IsValid_ExpiresAt 
ON CacheInfo (ImageId, IsValid, ExpiresAt);

-- Covering indexes for frequently accessed data
CREATE INDEX CONCURRENTLY IX_Images_Covering_CollectionId 
ON Images (CollectionId) 
INCLUDE (Filename, FileSize, Width, Height, Format);

-- Partial indexes for filtered queries
CREATE INDEX CONCURRENTLY IX_Images_Active_CollectionId 
ON Images (CollectionId) 
WHERE IsDeleted = FALSE;

-- GIN indexes for JSONB columns
CREATE INDEX CONCURRENTLY IX_Collections_Settings_GIN 
ON Collections USING GIN (SettingsJson);

CREATE INDEX CONCURRENTLY IX_Images_Metadata_GIN 
ON Images USING GIN (MetadataJson);

CREATE INDEX CONCURRENTLY IX_ApplicationLogs_Properties_GIN 
ON ApplicationLogs USING GIN (Properties);
```

## Database Maintenance

### 1. Vacuum and Analyze

#### Maintenance Script
```sql
-- Analyze tables for query optimization
ANALYZE Collections;
ANALYZE Images;
ANALYZE CacheInfo;
ANALYZE ApplicationLogs;

-- Vacuum tables to reclaim space
VACUUM ANALYZE Collections;
VACUUM ANALYZE Images;
VACUUM ANALYZE CacheInfo;
VACUUM ANALYZE ApplicationLogs;

-- Update table statistics
UPDATE pg_stat_user_tables 
SET n_tup_ins = 0, n_tup_upd = 0, n_tup_del = 0 
WHERE relname IN ('collections', 'images', 'cacheinfo', 'applicationlogs');
```

### 2. Partitioning for Large Tables

#### Application Logs Partitioning
```sql
-- Create partitioned table for application logs
CREATE TABLE application_logs (
    id BIGSERIAL,
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
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    PRIMARY KEY (id, timestamp)
) PARTITION BY RANGE (timestamp);

-- Create monthly partitions
CREATE TABLE application_logs_y2024m01 PARTITION OF application_logs
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

CREATE TABLE application_logs_y2024m02 PARTITION OF application_logs
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');

-- Create indexes on partitions
CREATE INDEX ON application_logs_y2024m01 (timestamp);
CREATE INDEX ON application_logs_y2024m01 (level);
CREATE INDEX ON application_logs_y2024m01 USING GIN (properties);

CREATE INDEX ON application_logs_y2024m02 (timestamp);
CREATE INDEX ON application_logs_y2024m02 (level);
CREATE INDEX ON application_logs_y2024m02 USING GIN (properties);
```

### 3. Backup Strategy

#### Backup Script
```bash
#!/bin/bash
# backup_imageviewer.sh

DB_NAME="imageviewer"
BACKUP_DIR="/backups/postgresql"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/imageviewer_$DATE.sql"

# Create backup directory if it doesn't exist
mkdir -p $BACKUP_DIR

# Create backup
pg_dump -h localhost -U postgres -d $DB_NAME \
    --verbose \
    --no-password \
    --format=custom \
    --compress=9 \
    --file=$BACKUP_FILE

# Keep only last 7 days of backups
find $BACKUP_DIR -name "imageviewer_*.sql" -mtime +7 -delete

echo "Backup completed: $BACKUP_FILE"
```

## Monitoring and Health Checks

### 1. Database Health Check

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ImageViewerDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;
    
    public DatabaseHealthCheck(ImageViewerDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Test database connection
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            
            stopwatch.Stop();
            
            // Get database statistics
            var connectionCount = await _context.Database.ExecuteSqlRawAsync("SELECT count(*) FROM pg_stat_activity WHERE datname = current_database()");
            
            var data = new Dictionary<string, object>
            {
                ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["active_connections"] = connectionCount,
                ["database"] = "imageviewer"
            };
            
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded("Database response time is slow", null, data);
            }
            
            return HealthCheckResult.Healthy("Database is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database is unhealthy", ex);
        }
    }
}
```

### 2. Performance Monitoring

```csharp
public class DatabasePerformanceService
{
    private readonly ImageViewerDbContext _context;
    private readonly ILogger<DatabasePerformanceService> _logger;
    
    public async Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync()
    {
        var metrics = new DatabasePerformanceMetrics();
        
        // Get slow queries
        var slowQueries = await _context.Database.SqlQueryRaw<SlowQueryInfo>(@"
            SELECT 
                query,
                calls,
                total_time,
                mean_time,
                rows
            FROM pg_stat_statements 
            WHERE mean_time > 1000 
            ORDER BY mean_time DESC 
            LIMIT 10").ToListAsync();
        
        metrics.SlowQueries = slowQueries;
        
        // Get table sizes
        var tableSizes = await _context.Database.SqlQueryRaw<TableSizeInfo>(@"
            SELECT 
                schemaname,
                tablename,
                pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
            FROM pg_tables 
            WHERE schemaname = 'public'
            ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC").ToListAsync();
        
        metrics.TableSizes = tableSizes;
        
        return metrics;
    }
}

public class SlowQueryInfo
{
    public string Query { get; set; }
    public long Calls { get; set; }
    public double TotalTime { get; set; }
    public double MeanTime { get; set; }
    public long Rows { get; set; }
}

public class TableSizeInfo
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string Size { get; set; }
}
```

## Conclusion

PostgreSQL setup này đảm bảo:

1. **Performance**: Optimized cho image metadata và cache operations
2. **Scalability**: Connection pooling và query optimization
3. **Reliability**: Retry logic và error handling
4. **Monitoring**: Health checks và performance monitoring
5. **Maintenance**: Automated backup và maintenance scripts
6. **Debugging**: Comprehensive logging và query analysis

Configuration này sẽ hỗ trợ hệ thống image viewer với high performance và reliability.
