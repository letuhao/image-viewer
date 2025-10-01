using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;
using System.Text.Json;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for Image Viewer System
/// </summary>
public class ImageViewerDbContext : DbContext
{
    private readonly ILogger<ImageViewerDbContext> _logger;

    public ImageViewerDbContext(DbContextOptions<ImageViewerDbContext> options, ILogger<ImageViewerDbContext> logger)
        : base(options)
    {
        _logger = logger;
    }

    // DbSets
            public DbSet<Collection> Collections { get; set; } = null!;
            public DbSet<Image> Images { get; set; } = null!;
            public DbSet<CacheFolder> CacheFolders { get; set; } = null!;
            public DbSet<Tag> Tags { get; set; } = null!;
            public DbSet<CollectionTag> CollectionTags { get; set; } = null!;
            public DbSet<ImageCacheInfo> ImageCacheInfos { get; set; } = null!;
            public DbSet<CollectionCacheBinding> CollectionCacheBindings { get; set; } = null!;
            public DbSet<CollectionStatistics> CollectionStatistics { get; set; } = null!;
            public DbSet<ViewSession> ViewSessions { get; set; } = null!;
            public DbSet<BackgroundJob> BackgroundJobs { get; set; } = null!;
            public DbSet<CollectionSettingsEntity> CollectionSettings { get; set; } = null!;
            public DbSet<ImageMetadataEntity> ImageMetadata { get; set; } = null!;

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
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                
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
            // Settings will be handled by separate entity
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
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.CollectionId);
            entity.HasIndex(e => e.Filename);
            entity.HasIndex(e => new { e.CollectionId, e.Filename }).IsUnique();
            entity.HasIndex(e => e.FileSize);
            entity.HasIndex(e => e.Width);
            entity.HasIndex(e => e.Height);
            entity.Property(e => e.Format).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.IsDeleted);
            
            entity.HasOne(e => e.CacheInfo)
                  .WithOne(c => c.Image)
                  .HasForeignKey<ImageCacheInfo>(c => c.ImageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // CacheFolder configuration
        modelBuilder.Entity<CacheFolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Path);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Priority);
            
            entity.HasMany(e => e.Bindings)
                  .WithOne(b => b.CacheFolder)
                  .HasForeignKey(b => b.CacheFolderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Color).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.UsageCount);
            
            entity.HasMany(e => e.CollectionTags)
                  .WithOne(ct => ct.Tag)
                  .HasForeignKey(ct => ct.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure value converters
        modelBuilder.Entity<Tag>()
            .Property(t => t.Color)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<TagColor>(v, (JsonSerializerOptions?)null) ?? TagColor.Default
            );

        modelBuilder.Entity<ViewSession>()
            .Property(vs => vs.Settings)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<ViewSessionSettings>(v, (JsonSerializerOptions?)null) ?? ViewSessionSettings.Default()
            );
        
        // CollectionTag configuration
        modelBuilder.Entity<CollectionTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => new { e.CollectionId, e.TagId }).IsUnique();
            entity.HasIndex(e => e.CollectionId);
            entity.HasIndex(e => e.TagId);
        });
        
        // ImageCacheInfo configuration
        modelBuilder.Entity<ImageCacheInfo>(entity =>
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
        
        // CollectionCacheBinding configuration
        modelBuilder.Entity<CollectionCacheBinding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => new { e.CollectionId, e.CacheFolderId }).IsUnique();
            entity.HasIndex(e => e.CollectionId);
            entity.HasIndex(e => e.CacheFolderId);
        });
        
        // CollectionStatistics configuration
        modelBuilder.Entity<CollectionStatistics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.LastViewedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.LastUpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.CollectionId).IsUnique();
            entity.HasIndex(e => e.ViewCount);
            entity.HasIndex(e => e.LastViewedAt);
        });
        
        // ViewSession configuration
        modelBuilder.Entity<ViewSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Settings).HasColumnType("jsonb");
            entity.Property(e => e.StartedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.CollectionId);
            entity.HasIndex(e => e.CurrentImageId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.EndedAt);
        });
        
        // BackgroundJob configuration
        modelBuilder.Entity<BackgroundJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.JobType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Parameters).HasColumnType("jsonb");
            entity.Property(e => e.Result).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.JobType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.CompletedAt);
        });

        // CollectionSettings configuration
        modelBuilder.Entity<CollectionSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CollectionId).IsRequired();
            entity.Property(e => e.AdditionalSettingsJson).HasDefaultValue("{}");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.Collection)
                  .WithOne(c => c.Settings)
                  .HasForeignKey<CollectionSettingsEntity>(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => e.CollectionId).IsUnique();
        });

        // ImageMetadata configuration
        modelBuilder.Entity<ImageMetadataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ImageId).IsRequired();
            entity.Property(e => e.AdditionalMetadataJson).HasDefaultValue("{}");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.Image)
                  .WithOne(i => i.Metadata)
                  .HasForeignKey<ImageMetadataEntity>(e => e.ImageId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => e.ImageId).IsUnique();
        });
    }
}
