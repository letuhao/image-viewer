using FluentAssertions;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Tests.Infrastructure.Data;

public class ImageViewerDbContextTests : IDisposable
{
    private readonly ImageViewerDbContext _context;
    private readonly TestDataBuilder _builder;

    public ImageViewerDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ImageViewerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var logger = new Mock<ILogger<ImageViewerDbContext>>();
        _context = new ImageViewerDbContext(options, logger.Object);
        _builder = new TestDataBuilder();

        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveChangesAsync_WithValidCollection_ShouldSaveToDatabase()
    {
        // Arrange
        var collection = _builder.Collection()
            .WithName("Test Collection")
            .WithPath("C:\\Test\\Path")
            .WithType(CollectionType.Folder)
            .Build();

        // Act
        _context.Collections.Add(collection);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        var savedCollection = await _context.Collections.FindAsync(collection.Id);
        savedCollection.Should().NotBeNull();
        savedCollection!.Name.Should().Be("Test Collection");
        savedCollection.Path.Should().Be("C:\\Test\\Path");
        savedCollection.Type.Should().Be(CollectionType.Folder);
    }

    [Fact]
    public async Task SaveChangesAsync_WithValidImage_ShouldSaveToDatabase()
    {
        // Arrange
        var collection = _builder.Collection().Build();
        var image = _builder.Image()
            .WithCollectionId(collection.Id)
            .WithFilename("test.jpg")
            .WithFormat("jpeg")
            .Build();

        // Act
        _context.Collections.Add(collection);
        _context.Images.Add(image);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(2);
        var savedImage = await _context.Images.FindAsync(image.Id);
        savedImage.Should().NotBeNull();
        savedImage!.Filename.Should().Be("test.jpg");
        savedImage.Format.Should().Be("jpeg");
        savedImage.CollectionId.Should().Be(collection.Id);
    }

    [Fact]
    public async Task Collections_WithIncludeImages_ShouldLoadRelatedImages()
    {
        // Arrange
        var collection = _builder.Collection().Build();
        var image1 = _builder.Image().WithCollectionId(collection.Id).WithFilename("image1.jpg").Build();
        var image2 = _builder.Image().WithCollectionId(collection.Id).WithFilename("image2.jpg").Build();

        _context.Collections.Add(collection);
        _context.Images.AddRange(image1, image2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _context.Collections
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == collection.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Images.Should().HaveCount(2);
        result.Images.Should().Contain(i => i.Filename == "image1.jpg");
        result.Images.Should().Contain(i => i.Filename == "image2.jpg");
    }

    [Fact]
    public async Task Collections_WithSoftDelete_ShouldNotReturnDeletedCollections()
    {
        // Arrange
        var collection1 = _builder.Collection().WithName("Active Collection").Build();
        var collection2 = _builder.Collection().WithName("Deleted Collection").Build();

        _context.Collections.AddRange(collection1, collection2);
        await _context.SaveChangesAsync();

        collection2.SoftDelete();
        await _context.SaveChangesAsync();

        // Act
        var activeCollections = await _context.Collections
            .Where(c => !c.IsDeleted)
            .ToListAsync();

        var allCollections = await _context.Collections.ToListAsync();

        // Assert
        activeCollections.Should().HaveCount(1);
        activeCollections.Should().Contain(c => c.Name == "Active Collection");
        allCollections.Should().HaveCount(2);
    }

    [Fact]
    public async Task Images_WithOrderBy_ShouldReturnInCorrectOrder()
    {
        // Arrange
        var collection = _builder.Collection().Build();
        var image1 = _builder.Image().WithCollectionId(collection.Id).WithFilename("image1.jpg").Build();
        var image2 = _builder.Image().WithCollectionId(collection.Id).WithFilename("image2.jpg").Build();
        var image3 = _builder.Image().WithCollectionId(collection.Id).WithFilename("image3.jpg").Build();

        _context.Collections.Add(collection);
        _context.Images.AddRange(image1, image2, image3);
        await _context.SaveChangesAsync();

        // Act
        var orderedImages = await _context.Images
            .Where(i => i.CollectionId == collection.Id)
            .OrderBy(i => i.Filename)
            .ToListAsync();

        // Assert
        orderedImages.Should().HaveCount(3);
        orderedImages[0].Filename.Should().Be("image1.jpg");
        orderedImages[1].Filename.Should().Be("image2.jpg");
        orderedImages[2].Filename.Should().Be("image3.jpg");
    }

    [Fact]
    public async Task CollectionSettings_WithJsonb_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var settings = _builder.CollectionSettings()
            .WithThumbnailSize(200, 200)
            .WithCacheSize(1280, 720)
            .WithAdditionalSettingsJson("{\"customKey\":\"customValue\"}")
            .Build();

        var collection = _builder.Collection().WithSettings(settings).Build();

        // Act
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();

        var savedCollection = await _context.Collections.FindAsync(collection.Id);

        // Assert
        savedCollection.Should().NotBeNull();
        savedCollection!.Settings.ThumbnailWidth.Should().Be(200);
        savedCollection.Settings.ThumbnailHeight.Should().Be(200);
        savedCollection.Settings.CacheWidth.Should().Be(1280);
        savedCollection.Settings.CacheHeight.Should().Be(720);
        savedCollection.Settings.AdditionalSettingsJson.Should().Contain("customKey");
        savedCollection.Settings.AdditionalSettingsJson.Should().Contain("customValue");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
