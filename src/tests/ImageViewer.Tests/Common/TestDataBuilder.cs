using AutoFixture;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Tests.Common;

/// <summary>
/// Builder class for creating test data
/// </summary>
public class TestDataBuilder
{
    private readonly IFixture _fixture;

    public TestDataBuilder()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    public CollectionBuilder Collection()
    {
        return new CollectionBuilder(_fixture);
    }

    public ImageBuilder Image()
    {
        return new ImageBuilder(_fixture);
    }

    public CollectionSettingsBuilder CollectionSettings()
    {
        return new CollectionSettingsBuilder(_fixture);
    }
}

public class CollectionBuilder
{
    private readonly IFixture _fixture;
    private string _name = "Test Collection";
    private string _path = "C:\\Test\\Path";
    private CollectionType _type = CollectionType.Folder;
    private CollectionSettingsEntity? _settings = null;

    public CollectionBuilder(IFixture fixture)
    {
        _fixture = fixture;
    }

    public CollectionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CollectionBuilder WithPath(string path)
    {
        _path = path;
        return this;
    }

    public CollectionBuilder WithType(CollectionType type)
    {
        _type = type;
        return this;
    }

    public CollectionBuilder WithSettings(CollectionSettingsEntity settings)
    {
        _settings = settings;
        return this;
    }

    public Collection Build()
    {
        var collection = new Collection(_name, _path, _type);
        if (_settings != null)
        {
            collection.SetSettings(_settings);
        }
        return collection;
    }
}

public class ImageBuilder
{
    private readonly IFixture _fixture;
    private Guid _collectionId = Guid.NewGuid();
    private string _filename = "test.jpg";
    private string _relativePath = "test.jpg";
    private string _format = "jpeg";
    private long _size = 1024 * 1024; // 1MB
    private ImageMetadataEntity? _metadata = null;
    private int _order = 1;

    public ImageBuilder(IFixture fixture)
    {
        _fixture = fixture;
    }

    public ImageBuilder WithCollectionId(Guid collectionId)
    {
        _collectionId = collectionId;
        return this;
    }

    public ImageBuilder WithFilename(string filename)
    {
        _filename = filename;
        return this;
    }

    public ImageBuilder WithRelativePath(string relativePath)
    {
        _relativePath = relativePath;
        return this;
    }

    public ImageBuilder WithFormat(string format)
    {
        _format = format;
        return this;
    }

    public ImageBuilder WithSize(long size)
    {
        _size = size;
        return this;
    }

    public ImageBuilder WithMetadata(ImageMetadataEntity metadata)
    {
        _metadata = metadata;
        return this;
    }

    public ImageBuilder WithOrder(int order)
    {
        _order = order;
        return this;
    }

    public Image Build()
    {
        var image = new Image(_collectionId, _filename, _relativePath, _size, 1920, 1080, _format);
        if (_metadata != null)
        {
            image.SetMetadata(_metadata);
        }
        return image;
    }
}

public class CollectionSettingsBuilder
{
    private readonly IFixture _fixture;
    private Guid _collectionId = Guid.NewGuid();
    private int _totalImages = 0;
    private long _totalSizeBytes = 0;
    private int _thumbnailWidth = 300;
    private int _thumbnailHeight = 300;
    private int _cacheWidth = 1920;
    private int _cacheHeight = 1080;
    private bool _autoGenerateThumbnails = true;
    private bool _autoGenerateCache = true;
    private TimeSpan _cacheExpiration = TimeSpan.FromDays(30);
    private string _additionalSettingsJson = "{}";

    public CollectionSettingsBuilder(IFixture fixture)
    {
        _fixture = fixture;
    }

    public CollectionSettingsBuilder WithCollectionId(Guid collectionId)
    {
        _collectionId = collectionId;
        return this;
    }

    public CollectionSettingsBuilder WithTotalImages(int totalImages)
    {
        _totalImages = totalImages;
        return this;
    }

    public CollectionSettingsBuilder WithTotalSizeBytes(long totalSizeBytes)
    {
        _totalSizeBytes = totalSizeBytes;
        return this;
    }

    public CollectionSettingsBuilder WithThumbnailSize(int width, int height)
    {
        _thumbnailWidth = width;
        _thumbnailHeight = height;
        return this;
    }

    public CollectionSettingsBuilder WithCacheSize(int width, int height)
    {
        _cacheWidth = width;
        _cacheHeight = height;
        return this;
    }

    public CollectionSettingsBuilder WithAutoGenerateThumbnails(bool autoGenerate)
    {
        _autoGenerateThumbnails = autoGenerate;
        return this;
    }

    public CollectionSettingsBuilder WithAutoGenerateCache(bool autoGenerate)
    {
        _autoGenerateCache = autoGenerate;
        return this;
    }

    public CollectionSettingsBuilder WithCacheExpiration(TimeSpan expiration)
    {
        _cacheExpiration = expiration;
        return this;
    }

    public CollectionSettingsBuilder WithAdditionalSettingsJson(string json)
    {
        _additionalSettingsJson = json;
        return this;
    }

    public CollectionSettingsEntity Build()
    {
        return new CollectionSettingsEntity(
            _collectionId,
            _totalImages,
            _totalSizeBytes,
            _thumbnailWidth,
            _thumbnailHeight,
            _cacheWidth,
            _cacheHeight,
            _autoGenerateThumbnails,
            _autoGenerateCache,
            _cacheExpiration,
            _additionalSettingsJson
        );
    }
}
