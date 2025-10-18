using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Image metadata entity - represents metadata for an image
/// </summary>
public class ImageMetadataEntity : BaseEntity
{
    [BsonElement("imageId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId ImageId { get; private set; }
    
    [BsonElement("quality")]
    public int Quality { get; private set; }
    
    [BsonElement("colorSpace")]
    public string? ColorSpace { get; private set; }
    
    [BsonElement("compression")]
    public string? Compression { get; private set; }
    
    [BsonElement("createdDate")]
    public DateTime? CreatedDate { get; private set; }
    
    [BsonElement("modifiedDate")]
    public DateTime? ModifiedDate { get; private set; }
    
    [BsonElement("camera")]
    public string? Camera { get; private set; }
    
    [BsonElement("software")]
    public string? Software { get; private set; }
    
    [BsonElement("additionalMetadataJson")]
    public string AdditionalMetadataJson { get; private set; } = "{}";
    
    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; private set; }

    // Navigation property - removed (Image entity deleted)

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
        ImageId = imageId;
        Quality = quality;
        ColorSpace = colorSpace;
        Compression = compression;
        CreatedDate = createdDate?.Kind == DateTimeKind.Local ? DateTime.SpecifyKind(createdDate.Value, DateTimeKind.Utc) : createdDate;
        ModifiedDate = modifiedDate?.Kind == DateTimeKind.Local ? DateTime.SpecifyKind(modifiedDate.Value, DateTimeKind.Utc) : modifiedDate;
        Camera = camera;
        Software = software;
        AdditionalMetadataJson = additionalMetadataJson ?? "{}";
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
