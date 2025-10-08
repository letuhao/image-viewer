using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Embedded image metadata value object for MongoDB
/// </summary>
public class ImageMetadataEmbedded
{
    [BsonElement("title")]
    public string? Title { get; private set; }
    
    [BsonElement("description")]
    public string? Description { get; private set; }
    
    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();
    
    [BsonElement("categories")]
    public List<string> Categories { get; private set; } = new();
    
    [BsonElement("exifData")]
    public Dictionary<string, object> ExifData { get; private set; } = new();
    
    [BsonElement("colorProfile")]
    public string? ColorProfile { get; private set; }
    
    [BsonElement("dpi")]
    public double? Dpi { get; private set; }
    
    [BsonElement("orientation")]
    public int? Orientation { get; private set; }
    
    [BsonElement("cameraMake")]
    public string? CameraMake { get; private set; }
    
    [BsonElement("cameraModel")]
    public string? CameraModel { get; private set; }
    
    [BsonElement("dateTaken")]
    public DateTime? DateTaken { get; private set; }
    
    [BsonElement("gpsLatitude")]
    public double? GpsLatitude { get; private set; }
    
    [BsonElement("gpsLongitude")]
    public double? GpsLongitude { get; private set; }
    
    [BsonElement("software")]
    public string? Software { get; private set; }
    
    [BsonElement("artist")]
    public string? Artist { get; private set; }
    
    [BsonElement("copyright")]
    public string? Copyright { get; private set; }

    // Private constructor for MongoDB
    private ImageMetadataEmbedded() { }

    public ImageMetadataEmbedded(string? title = null, string? description = null)
    {
        Title = title;
        Description = description;
    }

    public void SetTitle(string title)
    {
        Title = title;
    }

    public void SetDescription(string description)
    {
        Description = description;
    }

    public void AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
        {
            Tags.Add(tag);
        }
    }

    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
    }

    public void AddCategory(string category)
    {
        if (!string.IsNullOrWhiteSpace(category) && !Categories.Contains(category))
        {
            Categories.Add(category);
        }
    }

    public void RemoveCategory(string category)
    {
        Categories.Remove(category);
    }

    public void SetExifData(Dictionary<string, object> exifData)
    {
        ExifData = exifData ?? new Dictionary<string, object>();
    }

    public void SetCameraInfo(string? make, string? model)
    {
        CameraMake = make;
        CameraModel = model;
    }

    public void SetGpsInfo(double? latitude, double? longitude)
    {
        GpsLatitude = latitude;
        GpsLongitude = longitude;
    }

    public void SetImageInfo(string? colorProfile, double? dpi, int? orientation)
    {
        ColorProfile = colorProfile;
        Dpi = dpi;
        Orientation = orientation;
    }

    public void SetSoftwareInfo(string? software, string? artist, string? copyright)
    {
        Software = software;
        Artist = artist;
        Copyright = copyright;
    }
}
