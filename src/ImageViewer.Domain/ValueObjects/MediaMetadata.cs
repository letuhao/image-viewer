using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Media metadata value object
/// </summary>
public class MediaMetadata
{
    [BsonElement("title")]
    public string Title { get; private set; }
    
    [BsonElement("description")]
    public string Description { get; private set; }
    
    [BsonElement("tags")]
    public List<string> Tags { get; private set; }
    
    [BsonElement("categories")]
    public List<string> Categories { get; private set; }
    
    [BsonElement("customFields")]
    public Dictionary<string, object> CustomFields { get; private set; }
    
    [BsonElement("exifData")]
    public Dictionary<string, object> ExifData { get; private set; }
    
    [BsonElement("colorProfile")]
    public string ColorProfile { get; private set; }
    
    [BsonElement("bitDepth")]
    public int BitDepth { get; private set; }
    
    [BsonElement("compression")]
    public string Compression { get; private set; }
    
    [BsonElement("createdDate")]
    public DateTime? CreatedDate { get; private set; }
    
    [BsonElement("modifiedDate")]
    public DateTime? ModifiedDate { get; private set; }
    
    [BsonElement("cameraInfo")]
    public CameraInfo CameraInfo { get; private set; }

    public MediaMetadata()
    {
        Title = string.Empty;
        Description = string.Empty;
        Tags = new List<string>();
        Categories = new List<string>();
        CustomFields = new Dictionary<string, object>();
        ExifData = new Dictionary<string, object>();
        ColorProfile = string.Empty;
        BitDepth = 8;
        Compression = string.Empty;
        CameraInfo = new CameraInfo();
    }

    public void UpdateTitle(string title)
    {
        Title = title ?? string.Empty;
    }

    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
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

    public void AddCustomField(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            CustomFields[key] = value;
        }
    }

    public void RemoveCustomField(string key)
    {
        CustomFields.Remove(key);
    }

    public void AddExifData(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            ExifData[key] = value;
        }
    }

    public void RemoveExifData(string key)
    {
        ExifData.Remove(key);
    }

    public void UpdateColorProfile(string colorProfile)
    {
        ColorProfile = colorProfile ?? string.Empty;
    }

    public void UpdateBitDepth(int bitDepth)
    {
        if (bitDepth <= 0)
            throw new ArgumentException("Bit depth must be greater than 0", nameof(bitDepth));
        
        BitDepth = bitDepth;
    }

    public void UpdateCompression(string compression)
    {
        Compression = compression ?? string.Empty;
    }

    public void UpdateCreatedDate(DateTime? createdDate)
    {
        CreatedDate = createdDate;
    }

    public void UpdateModifiedDate(DateTime? modifiedDate)
    {
        ModifiedDate = modifiedDate;
    }

    public void UpdateCameraInfo(CameraInfo cameraInfo)
    {
        CameraInfo = cameraInfo ?? throw new ArgumentNullException(nameof(cameraInfo));
    }
}

/// <summary>
/// Camera info value object
/// </summary>
public class CameraInfo
{
    [BsonElement("make")]
    public string Make { get; private set; }
    
    [BsonElement("model")]
    public string Model { get; private set; }
    
    [BsonElement("lens")]
    public string Lens { get; private set; }
    
    [BsonElement("focalLength")]
    public string FocalLength { get; private set; }
    
    [BsonElement("aperture")]
    public string Aperture { get; private set; }
    
    [BsonElement("shutterSpeed")]
    public string ShutterSpeed { get; private set; }
    
    [BsonElement("iso")]
    public string Iso { get; private set; }
    
    [BsonElement("flash")]
    public string Flash { get; private set; }

    public CameraInfo()
    {
        Make = string.Empty;
        Model = string.Empty;
        Lens = string.Empty;
        FocalLength = string.Empty;
        Aperture = string.Empty;
        ShutterSpeed = string.Empty;
        Iso = string.Empty;
        Flash = string.Empty;
    }

    public void UpdateMake(string make)
    {
        Make = make ?? string.Empty;
    }

    public void UpdateModel(string model)
    {
        Model = model ?? string.Empty;
    }

    public void UpdateLens(string lens)
    {
        Lens = lens ?? string.Empty;
    }

    public void UpdateFocalLength(string focalLength)
    {
        FocalLength = focalLength ?? string.Empty;
    }

    public void UpdateAperture(string aperture)
    {
        Aperture = aperture ?? string.Empty;
    }

    public void UpdateShutterSpeed(string shutterSpeed)
    {
        ShutterSpeed = shutterSpeed ?? string.Empty;
    }

    public void UpdateIso(string iso)
    {
        Iso = iso ?? string.Empty;
    }

    public void UpdateFlash(string flash)
    {
        Flash = flash ?? string.Empty;
    }
}
