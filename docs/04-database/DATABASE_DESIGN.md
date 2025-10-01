# Image Viewer System - Database Design

## Tổng quan Database Schema

### Database Technology
- **Primary Database**: PostgreSQL 15+
- **Cache Database**: Redis 7.0+
- **Search Engine**: Elasticsearch 8.0+ (optional)
- **File Storage**: Local File System hoặc Azure Blob Storage

### Database Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Entity Framework Core 8.0                                 │
│  - Code First Migrations                                   │
│  - Compiled Queries                                        │
│  - Change Tracking                                         │
│  - Connection Pooling                                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Database Layer                           │
├─────────────────────────────────────────────────────────────┤
│  PostgreSQL 15+                                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Core      │  │   Cache     │  │   Analytics │        │
│  │   Tables    │  │   Tables    │  │   Tables    │        │
│  │             │  │             │  │             │        │
│  │ - Collections│  │ - CacheInfo │  │ - ViewStats │        │
│  │ - Images    │  │ - CacheFolders│  │ - SearchStats│      │
│  │ - Tags      │  │ - CacheJobs │  │ - UserStats │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## Core Tables

### Collections Table
```sql
CREATE TABLE Collections (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(255) NOT NULL,
    Path VARCHAR(1000) NOT NULL,
    Type SMALLINT NOT NULL, -- 0=Folder, 1=Zip, 2=SevenZip, 3=Rar, 4=Tar
    Settings JSONB, -- JSON settings
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
    DeletedAt TIMESTAMP WITH TIME ZONE NULL,
    
    -- Indexes
    CREATE INDEX IX_Collections_Name ON Collections (Name),
    CREATE UNIQUE INDEX IX_Collections_Path ON Collections (Path),
    CREATE INDEX IX_Collections_Type ON Collections (Type),
    CREATE INDEX IX_Collections_CreatedAt ON Collections (CreatedAt),
    CREATE INDEX IX_Collections_UpdatedAt ON Collections (UpdatedAt),
    CREATE INDEX IX_Collections_IsDeleted ON Collections (IsDeleted)
);

-- Collection Settings JSON Schema
/*
{
  "totalImages": 1500,
  "lastScanned": "2024-01-01T00:00:00Z",
  "autoScan": true,
  "thumbnailQuality": 80,
  "cacheEnabled": true,
  "cacheQuality": 85,
  "cacheFormat": "jpeg",
  "maxCacheSize": 10737418240,
  "scanRecursively": true,
  "includeSubfolders": true,
  "excludePatterns": ["*.tmp", "*.log"],
  "includePatterns": ["*.jpg", "*.png", "*.gif", "*.webp"],
  "thumbnailSize": {"width": 300, "height": 300},
  "cacheSize": {"width": 1920, "height": 1080}
}
*/
```

### Images Table
```sql
CREATE TABLE Images (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    CollectionId UUID NOT NULL,
    Filename VARCHAR(255) NOT NULL,
    RelativePath VARCHAR(1000) NOT NULL,
    FileSize BIGINT NOT NULL,
    Width INTEGER NOT NULL,
    Height INTEGER NOT NULL,
    Format VARCHAR(10) NOT NULL,
    Metadata JSONB, -- JSON metadata
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
    DeletedAt TIMESTAMP WITH TIME ZONE NULL,
    
    -- Foreign Keys
    CONSTRAINT FK_Images_Collections FOREIGN KEY (CollectionId) REFERENCES Collections(Id) ON DELETE CASCADE,
    
    -- Indexes
    CREATE INDEX IX_Images_CollectionId ON Images (CollectionId),
    CREATE INDEX IX_Images_Filename ON Images (Filename),
    CREATE UNIQUE INDEX IX_Images_CollectionId_Filename ON Images (CollectionId, Filename),
    CREATE INDEX IX_Images_FileSize ON Images (FileSize),
    CREATE INDEX IX_Images_Width ON Images (Width),
    CREATE INDEX IX_Images_Height ON Images (Height),
    CREATE INDEX IX_Images_Format ON Images (Format),
    CREATE INDEX IX_Images_CreatedAt ON Images (CreatedAt),
    CREATE INDEX IX_Images_IsDeleted ON Images (IsDeleted)
);

-- Image Metadata JSON Schema
/*
{
  "quality": 95,
  "colorSpace": "RGB",
  "colorDepth": 24,
  "compression": "JPEG",
  "exif": {
    "camera": "Canon EOS R5",
    "lens": "RF 24-70mm F2.8L IS USM",
    "focalLength": "50mm",
    "aperture": "f/2.8",
    "shutterSpeed": "1/125s",
    "iso": 400,
    "dateTaken": "2024-01-01T00:00:00Z"
  },
  "iccProfile": "sRGB IEC61966-2.1",
  "orientation": 1,
  "hasTransparency": false,
  "isAnimated": false,
  "frameCount": 1,
  "duration": 0
}
*/
```

### CollectionTags Table
```sql
CREATE TABLE CollectionTags (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CollectionId UNIQUEIDENTIFIER NOT NULL,
    Tag NVARCHAR(100) NOT NULL,
    AddedBy NVARCHAR(100) NOT NULL,
    AddedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_CollectionTags_Collections FOREIGN KEY (CollectionId) REFERENCES Collections(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_CollectionTags_CollectionId (CollectionId),
    INDEX IX_CollectionTags_Tag (Tag),
    INDEX IX_CollectionTags_CollectionId_Tag (CollectionId, Tag),
    INDEX IX_CollectionTags_AddedBy (AddedBy),
    INDEX IX_CollectionTags_AddedAt (AddedAt)
);
```

## Cache Tables

### CacheInfo Table
```sql
CREATE TABLE CacheInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ImageId UNIQUEIDENTIFIER NOT NULL,
    CachePath NVARCHAR(1000) NOT NULL,
    ThumbnailPath NVARCHAR(1000) NULL,
    CacheSize BIGINT NOT NULL,
    ThumbnailSize BIGINT NULL,
    Quality TINYINT NOT NULL,
    Format NVARCHAR(10) NOT NULL,
    Dimensions NVARCHAR(20) NOT NULL, -- "1920x1080"
    CachedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2(7) NOT NULL,
    IsValid BIT NOT NULL DEFAULT 1,
    
    -- Foreign Keys
    CONSTRAINT FK_CacheInfo_Images FOREIGN KEY (ImageId) REFERENCES Images(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_CacheInfo_ImageId (ImageId),
    INDEX IX_CacheInfo_CachePath (CachePath),
    INDEX IX_CacheInfo_ExpiresAt (ExpiresAt),
    INDEX IX_CacheInfo_IsValid (IsValid),
    INDEX IX_CacheInfo_CachedAt (CachedAt),
    INDEX IX_CacheInfo_Quality (Quality),
    INDEX IX_CacheInfo_Format (Format)
);
```

### CacheFolders Table
```sql
CREATE TABLE CacheFolders (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Path NVARCHAR(1000) NOT NULL,
    Priority INT NOT NULL DEFAULT 0,
    MaxSize BIGINT NOT NULL,
    CurrentSize BIGINT NOT NULL DEFAULT 0,
    FileCount INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Indexes
    INDEX IX_CacheFolders_Name (Name),
    INDEX IX_CacheFolders_Path (Path) UNIQUE,
    INDEX IX_CacheFolders_Priority (Priority),
    INDEX IX_CacheFolders_IsActive (IsActive),
    INDEX IX_CacheFolders_CurrentSize (CurrentSize)
);
```

### CollectionCacheBindings Table
```sql
CREATE TABLE CollectionCacheBindings (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CollectionId UNIQUEIDENTIFIER NOT NULL,
    CacheFolderId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_CollectionCacheBindings_Collections FOREIGN KEY (CollectionId) REFERENCES Collections(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CollectionCacheBindings_CacheFolders FOREIGN KEY (CacheFolderId) REFERENCES CacheFolders(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_CollectionCacheBindings_CollectionId (CollectionId) UNIQUE,
    INDEX IX_CollectionCacheBindings_CacheFolderId (CacheFolderId),
    INDEX IX_CollectionCacheBindings_CreatedAt (CreatedAt)
);
```

## Analytics Tables

### CollectionStatistics Table
```sql
CREATE TABLE CollectionStatistics (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CollectionId UNIQUEIDENTIFIER NOT NULL,
    ViewCount BIGINT NOT NULL DEFAULT 0,
    TotalViewTime BIGINT NOT NULL DEFAULT 0, -- in seconds
    SearchCount BIGINT NOT NULL DEFAULT 0,
    LastViewed DATETIME2(7) NULL,
    LastSearched DATETIME2(7) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_CollectionStatistics_Collections FOREIGN KEY (CollectionId) REFERENCES Collections(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_CollectionStatistics_CollectionId (CollectionId) UNIQUE,
    INDEX IX_CollectionStatistics_ViewCount (ViewCount),
    INDEX IX_CollectionStatistics_TotalViewTime (TotalViewTime),
    INDEX IX_CollectionStatistics_SearchCount (SearchCount),
    INDEX IX_CollectionStatistics_LastViewed (LastViewed),
    INDEX IX_CollectionStatistics_LastSearched (LastSearched)
);
```

### ViewSessions Table
```sql
CREATE TABLE ViewSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CollectionId UNIQUEIDENTIFIER NOT NULL,
    SessionId NVARCHAR(100) NOT NULL,
    StartTime DATETIME2(7) NOT NULL,
    EndTime DATETIME2(7) NULL,
    TotalTime BIGINT NULL, -- in seconds
    ImageCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_ViewSessions_Collections FOREIGN KEY (CollectionId) REFERENCES Collections(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_ViewSessions_CollectionId (CollectionId),
    INDEX IX_ViewSessions_SessionId (SessionId),
    INDEX IX_ViewSessions_StartTime (StartTime),
    INDEX IX_ViewSessions_EndTime (EndTime),
    INDEX IX_ViewSessions_TotalTime (TotalTime)
);
```

### SearchLogs Table
```sql
CREATE TABLE SearchLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CollectionId UNIQUEIDENTIFIER NOT NULL,
    Query NVARCHAR(500) NOT NULL,
    ResultCount INT NOT NULL,
    SearchTime DECIMAL(10,3) NOT NULL, -- in seconds
    UserAgent NVARCHAR(500) NULL,
    IpAddress NVARCHAR(45) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_SearchLogs_Collections FOREIGN KEY (CollectionId) REFERENCES Collections(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_SearchLogs_CollectionId (CollectionId),
    INDEX IX_SearchLogs_Query (Query),
    INDEX IX_SearchLogs_ResultCount (ResultCount),
    INDEX IX_SearchLogs_SearchTime (SearchTime),
    INDEX IX_SearchLogs_CreatedAt (CreatedAt)
);
```

## Background Jobs Tables

### BackgroundJobs Table
```sql
CREATE TABLE BackgroundJobs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    JobId NVARCHAR(100) NOT NULL UNIQUE,
    Type NVARCHAR(50) NOT NULL,
    Status TINYINT NOT NULL, -- 0=Queued, 1=Running, 2=Completed, 3=Failed, 4=Cancelled
    Progress NVARCHAR(MAX), -- JSON progress data
    Parameters NVARCHAR(MAX), -- JSON job parameters
    Result NVARCHAR(MAX), -- JSON job result
    ErrorMessage NVARCHAR(MAX) NULL,
    StartedAt DATETIME2(7) NULL,
    CompletedAt DATETIME2(7) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Indexes
    INDEX IX_BackgroundJobs_JobId (JobId),
    INDEX IX_BackgroundJobs_Type (Type),
    INDEX IX_BackgroundJobs_Status (Status),
    INDEX IX_BackgroundJobs_StartedAt (StartedAt),
    INDEX IX_BackgroundJobs_CompletedAt (CompletedAt),
    INDEX IX_BackgroundJobs_CreatedAt (CreatedAt)
);

-- Progress JSON Schema
/*
{
  "total": 3000,
  "completed": 1500,
  "percentage": 50,
  "currentItem": "page_0750.jpg",
  "errors": [],
  "estimatedCompletion": "2024-01-01T00:15:00Z",
  "currentCollection": "My Manga Collection",
  "processedCollections": 5,
  "totalCollections": 10
}
*/
```

## Entity Framework Models

### Collection Entity
```csharp
[Table("Collections")]
public class Collection : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string Path { get; set; }
    
    [Required]
    public CollectionType Type { get; set; }
    
    [Column(TypeName = "nvarchar(max)")]
    public string SettingsJson { get; set; }
    
    [NotMapped]
    public CollectionSettings Settings
    {
        get => JsonSerializer.Deserialize<CollectionSettings>(SettingsJson ?? "{}");
        set => SettingsJson = JsonSerializer.Serialize(value);
    }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Image> Images { get; set; }
    public virtual ICollection<CollectionTag> Tags { get; set; }
    public virtual CollectionStatistics Statistics { get; set; }
    public virtual CollectionCacheBinding CacheBinding { get; set; }
}

public enum CollectionType : byte
{
    Folder = 0,
    Zip = 1,
    SevenZip = 2,
    Rar = 3,
    Tar = 4
}

public class CollectionSettings
{
    public int TotalImages { get; set; }
    public DateTime? LastScanned { get; set; }
    public bool AutoScan { get; set; } = true;
    public int ThumbnailQuality { get; set; } = 80;
    public bool CacheEnabled { get; set; } = true;
    public int CacheQuality { get; set; } = 85;
    public string CacheFormat { get; set; } = "jpeg";
    public long MaxCacheSize { get; set; } = 10737418240; // 10GB
    public bool ScanRecursively { get; set; } = true;
    public bool IncludeSubfolders { get; set; } = true;
    public List<string> ExcludePatterns { get; set; } = new();
    public List<string> IncludePatterns { get; set; } = new();
    public ThumbnailSize ThumbnailSize { get; set; } = new() { Width = 300, Height = 300 };
    public CacheSize CacheSize { get; set; } = new() { Width = 1920, Height = 1080 };
}

public class ThumbnailSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public class CacheSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}
```

### Image Entity
```csharp
[Table("Images")]
public class Image : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid CollectionId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Filename { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string RelativePath { get; set; }
    
    [Required]
    public long FileSize { get; set; }
    
    [Required]
    public int Width { get; set; }
    
    [Required]
    public int Height { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Format { get; set; }
    
    [Column(TypeName = "nvarchar(max)")]
    public string MetadataJson { get; set; }
    
    [NotMapped]
    public ImageMetadata Metadata
    {
        get => JsonSerializer.Deserialize<ImageMetadata>(MetadataJson ?? "{}");
        set => MetadataJson = JsonSerializer.Serialize(value);
    }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual Collection Collection { get; set; }
    public virtual CacheInfo CacheInfo { get; set; }
}

public class ImageMetadata
{
    public int Quality { get; set; }
    public string ColorSpace { get; set; }
    public int ColorDepth { get; set; }
    public string Compression { get; set; }
    public ExifData Exif { get; set; }
    public string IccProfile { get; set; }
    public int Orientation { get; set; }
    public bool HasTransparency { get; set; }
    public bool IsAnimated { get; set; }
    public int FrameCount { get; set; }
    public decimal Duration { get; set; }
}

public class ExifData
{
    public string Camera { get; set; }
    public string Lens { get; set; }
    public string FocalLength { get; set; }
    public string Aperture { get; set; }
    public string ShutterSpeed { get; set; }
    public int Iso { get; set; }
    public DateTime? DateTaken { get; set; }
}
```

### CacheInfo Entity
```csharp
[Table("CacheInfo")]
public class CacheInfo : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid ImageId { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string CachePath { get; set; }
    
    [MaxLength(1000)]
    public string ThumbnailPath { get; set; }
    
    [Required]
    public long CacheSize { get; set; }
    
    public long? ThumbnailSize { get; set; }
    
    [Required]
    public byte Quality { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Format { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Dimensions { get; set; }
    
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsValid { get; set; } = true;
    
    // Navigation properties
    public virtual Image Image { get; set; }
}
```

## Database Configuration

### DbContext Configuration
```csharp
public class ImageViewerDbContext : DbContext
{
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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Collection configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
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
            entity.Property(e => e.Filename).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RelativePath).IsRequired().HasMaxLength(1000);
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
            entity.Property(e => e.CachePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Dimensions).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.ImageId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsValid);
            entity.HasIndex(e => e.CachedAt);
        });
        
        // CacheFolder configuration
        modelBuilder.Entity<CacheFolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CurrentSize);
        });
        
        // CollectionCacheBinding configuration
        modelBuilder.Entity<CollectionCacheBinding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CollectionId).IsUnique();
            entity.HasIndex(e => e.CacheFolderId);
        });
        
        // CollectionStatistics configuration
        modelBuilder.Entity<CollectionStatistics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CollectionId).IsUnique();
            entity.HasIndex(e => e.ViewCount);
            entity.HasIndex(e => e.TotalViewTime);
            entity.HasIndex(e => e.SearchCount);
        });
        
        // BackgroundJob configuration
        modelBuilder.Entity<BackgroundJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.JobId).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.CompletedAt);
        });
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(
                connectionString,
                options => options.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30))
            );
        }
    }
}
```

## Database Migrations

### Initial Migration
```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Collections table
        migrationBuilder.CreateTable(
            name: "Collections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Type = table.Column<byte>(type: "tinyint", nullable: false),
                SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Collections", x => x.Id);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_Collections_Name",
            table: "Collections",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Collections_Path",
            table: "Collections",
            column: "Path",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Collections_Type",
            table: "Collections",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_Collections_CreatedAt",
            table: "Collections",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Collections_IsDeleted",
            table: "Collections",
            column: "IsDeleted");

        // Create Images table
        migrationBuilder.CreateTable(
            name: "Images",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CollectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Filename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                FileSize = table.Column<long>(type: "bigint", nullable: false),
                Width = table.Column<int>(type: "int", nullable: false),
                Height = table.Column<int>(type: "int", nullable: false),
                Format = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Images", x => x.Id);
                table.ForeignKey(
                    name: "FK_Images_Collections_CollectionId",
                    column: x => x.CollectionId,
                    principalTable: "Collections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create Images indexes
        migrationBuilder.CreateIndex(
            name: "IX_Images_CollectionId",
            table: "Images",
            column: "CollectionId");

        migrationBuilder.CreateIndex(
            name: "IX_Images_CollectionId_Filename",
            table: "Images",
            columns: new[] { "CollectionId", "Filename" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Images_FileSize",
            table: "Images",
            column: "FileSize");

        migrationBuilder.CreateIndex(
            name: "IX_Images_Width",
            table: "Images",
            column: "Width");

        migrationBuilder.CreateIndex(
            name: "IX_Images_Height",
            table: "Images",
            column: "Height");

        migrationBuilder.CreateIndex(
            name: "IX_Images_Format",
            table: "Images",
            column: "Format");

        migrationBuilder.CreateIndex(
            name: "IX_Images_IsDeleted",
            table: "Images",
            column: "IsDeleted");

        // Create other tables...
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Images");
        migrationBuilder.DropTable(name: "Collections");
        // Drop other tables...
    }
}
```

## Database Performance Optimizations

### 1. Indexing Strategy
```sql
-- Composite indexes for common queries
CREATE INDEX IX_Images_CollectionId_Format_FileSize 
ON Images (CollectionId, Format, FileSize);

CREATE INDEX IX_Images_CollectionId_Width_Height 
ON Images (CollectionId, Width, Height);

CREATE INDEX IX_CacheInfo_ImageId_IsValid_ExpiresAt 
ON CacheInfo (ImageId, IsValid, ExpiresAt);

-- Covering indexes for frequently accessed data
CREATE INDEX IX_Images_Covering_CollectionId 
ON Images (CollectionId) 
INCLUDE (Filename, FileSize, Width, Height, Format);

-- Partial indexes for filtered queries
CREATE INDEX IX_Images_Active_CollectionId 
ON Images (CollectionId) 
WHERE IsDeleted = 0;
```

### 2. Query Optimization
```csharp
// Compiled queries for frequently used operations
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

### 3. Connection Pooling
```csharp
// Configure connection pooling
services.AddDbContext<ImageViewerDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30));
        
        npgsqlOptions.CommandTimeout(30);
    });
    
    options.EnableSensitiveDataLogging(false);
    options.EnableDetailedErrors(false);
    options.EnableServiceProviderCaching();
    options.EnableThreadSafetyChecks();
});

// Configure connection pool
services.Configure<NpgsqlDbContextOptions>(options =>
{
    options.CommandTimeout(30);
    options.EnableRetryOnFailure(3);
});
```

### 4. Bulk Operations
```csharp
public class BulkOperations
{
    public async Task BulkInsertImagesAsync(IEnumerable<Image> images)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            await _context.Images.AddRangeAsync(images);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task BulkUpdateCacheInfoAsync(IEnumerable<CacheInfo> cacheInfos)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            _context.CacheInfos.UpdateRange(cacheInfos);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

## Database Maintenance

### 1. Cleanup Procedures
```sql
-- Cleanup expired cache entries
CREATE PROCEDURE CleanupExpiredCache
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DeletedCount INT = 0;
    
    -- Delete expired cache records
    DELETE FROM CacheInfo 
    WHERE ExpiresAt < GETUTCDATE() AND IsValid = 1;
    
    SET @DeletedCount = @@ROWCOUNT;
    
    -- Update cache folder statistics
    UPDATE cf 
    SET CurrentSize = (
            SELECT ISNULL(SUM(CacheSize), 0) 
            FROM CacheInfo ci 
            WHERE ci.CachePath LIKE cf.Path + '%' 
            AND ci.IsValid = 1
        ),
        FileCount = (
            SELECT COUNT(*) 
            FROM CacheInfo ci 
            WHERE ci.CachePath LIKE cf.Path + '%' 
            AND ci.IsValid = 1
        )
    FROM CacheFolders cf;
    
    SELECT @DeletedCount AS DeletedCount;
END;

-- Cleanup old background jobs
CREATE PROCEDURE CleanupOldJobs
    @DaysToKeep INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@DaysToKeep, GETUTCDATE());
    
    DELETE FROM BackgroundJobs 
    WHERE CompletedAt < @CutoffDate 
    AND Status IN (2, 3, 4); -- Completed, Failed, Cancelled
    
    SELECT @@ROWCOUNT AS DeletedCount;
END;
```

### 2. Statistics Updates
```sql
-- Update collection statistics
CREATE PROCEDURE UpdateCollectionStatistics
    @CollectionId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE cs 
    SET ViewCount = (
            SELECT COUNT(*) 
            FROM ViewSessions vs 
            WHERE vs.CollectionId = @CollectionId
        ),
        TotalViewTime = (
            SELECT ISNULL(SUM(TotalTime), 0) 
            FROM ViewSessions vs 
            WHERE vs.CollectionId = @CollectionId
        ),
        SearchCount = (
            SELECT COUNT(*) 
            FROM SearchLogs sl 
            WHERE sl.CollectionId = @CollectionId
        ),
        LastViewed = (
            SELECT MAX(EndTime) 
            FROM ViewSessions vs 
            WHERE vs.CollectionId = @CollectionId
        ),
        LastSearched = (
            SELECT MAX(CreatedAt) 
            FROM SearchLogs sl 
            WHERE sl.CollectionId = @CollectionId
        ),
        UpdatedAt = GETUTCDATE()
    FROM CollectionStatistics cs
    WHERE cs.CollectionId = @CollectionId;
END;
```

### 3. Health Monitoring
```sql
-- Database health check
CREATE PROCEDURE CheckDatabaseHealth
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        'Collections' AS TableName,
        COUNT(*) AS RecordCount,
        MAX(UpdatedAt) AS LastUpdated
    FROM Collections 
    WHERE IsDeleted = 0
    
    UNION ALL
    
    SELECT 
        'Images' AS TableName,
        COUNT(*) AS RecordCount,
        MAX(UpdatedAt) AS LastUpdated
    FROM Images 
    WHERE IsDeleted = 0
    
    UNION ALL
    
    SELECT 
        'CacheInfo' AS TableName,
        COUNT(*) AS RecordCount,
        MAX(CachedAt) AS LastUpdated
    FROM CacheInfo 
    WHERE IsValid = 1
    
    UNION ALL
    
    SELECT 
        'BackgroundJobs' AS TableName,
        COUNT(*) AS RecordCount,
        MAX(UpdatedAt) AS LastUpdated
    FROM BackgroundJobs;
END;
```

## Conclusion

Database design này được tối ưu cho:

1. **Performance**: Indexes được thiết kế cho các query patterns phổ biến
2. **Scalability**: Schema hỗ trợ horizontal scaling và partitioning
3. **Maintainability**: Clean structure với proper relationships
4. **Reliability**: Comprehensive error handling và data integrity
5. **Flexibility**: JSON columns cho dynamic data, soft deletes cho data recovery
6. **Monitoring**: Built-in health checks và maintenance procedures

Database này sẽ hỗ trợ hệ thống image viewer với hàng triệu images và hàng nghìn collections một cách hiệu quả.
