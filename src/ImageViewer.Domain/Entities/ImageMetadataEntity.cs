using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Image metadata entity - represents metadata for an image
/// </summary>
public class ImageMetadataEntity : BaseEntity
{
    public new ObjectId Id { get; private set; }
    public ObjectId ImageId { get; private set; }
    public int Quality { get; private set; }
    public string? ColorSpace { get; private set; }
    public string? Compression { get; private set; }
    public DateTime? CreatedDate { get; private set; }
    public DateTime? ModifiedDate { get; private set; }
    public string? Camera { get; private set; }
    public string? Software { get; private set; }
    public string AdditionalMetadataJson { get; private set; } = "{}";
    public new DateTime CreatedAt { get; private set; }
    public new DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Navigation property
    public Image Image { get; private set; } = null!;

    // Private constructor for EF Core
    private ImageMetadataEntity() { }

    public ImageMetadataEntity(
        ObjectId imageId,
        int quality = 95,
        string? colorSpace = null,
        string? compression = null,
        DateTime? createdDate = null,
        DateTime? modifiedDate = null,
        string? camera = null,
        string? software = null,
        string? additionalMetadataJson = null)
    {
        Id = ObjectId.GenerateNewId();
        ImageId = imageId;
        Quality = quality;
        ColorSpace = colorSpace;
        Compression = compression;
        CreatedDate = createdDate?.Kind == DateTimeKind.Local ? DateTime.SpecifyKind(createdDate.Value, DateTimeKind.Utc) : createdDate;
        ModifiedDate = modifiedDate?.Kind == DateTimeKind.Local ? DateTime.SpecifyKind(modifiedDate.Value, DateTimeKind.Utc) : modifiedDate;
        Camera = camera;
        Software = software;
        AdditionalMetadataJson = additionalMetadataJson ?? "{}";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void UpdateQuality(int quality)
    {
        if (quality < 0 || quality > 100)
            throw new ArgumentException("Quality must be between 0 and 100", nameof(quality));

        Quality = quality;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateColorSpace(string? colorSpace)
    {
        ColorSpace = colorSpace;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCompression(string? compression)
    {
        Compression = compression;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCreatedDate(DateTime? createdDate)
    {
        CreatedDate = createdDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateModifiedDate(DateTime? modifiedDate)
    {
        ModifiedDate = modifiedDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCamera(string? camera)
    {
        Camera = camera;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSoftware(string? software)
    {
        Software = software;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAdditionalMetadata(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        AdditionalMetadataJson = json;
        UpdatedAt = DateTime.UtcNow;
    }
}
