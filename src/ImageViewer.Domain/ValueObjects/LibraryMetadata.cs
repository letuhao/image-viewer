using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Library metadata value object
/// </summary>
public class LibraryMetadata
{
    [BsonElement("tags")]
    public List<string> Tags { get; private set; }
    
    [BsonElement("categories")]
    public List<string> Categories { get; private set; }
    
    [BsonElement("customFields")]
    public Dictionary<string, object> CustomFields { get; private set; }
    
    [BsonElement("version")]
    public string Version { get; private set; }
    
    [BsonElement("lastModified")]
    public DateTime? LastModified { get; private set; }
    
    [BsonElement("createdBy")]
    public string CreatedBy { get; private set; }
    
    [BsonElement("modifiedBy")]
    public string ModifiedBy { get; private set; }
    
    [BsonElement("description")]
    public string Description { get; private set; }

    public LibraryMetadata()
    {
        Tags = new List<string>();
        Categories = new List<string>();
        CustomFields = new Dictionary<string, object>();
        Version = "1.0";
        CreatedBy = string.Empty;
        ModifiedBy = string.Empty;
        Description = string.Empty;
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

    public void UpdateVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or empty", nameof(version));
        
        Version = version;
    }

    public void UpdateLastModified(DateTime modifiedDate)
    {
        LastModified = modifiedDate;
    }

    public void UpdateCreatedBy(string createdBy)
    {
        CreatedBy = createdBy ?? string.Empty;
    }

    public void UpdateModifiedBy(string modifiedBy)
    {
        ModifiedBy = modifiedBy ?? string.Empty;
    }

    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
    }
}
