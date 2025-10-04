using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Search index value object for collection search optimization
/// </summary>
public class SearchIndex
{
    [BsonElement("searchableText")]
    public string SearchableText { get; private set; }
    
    [BsonElement("tags")]
    public List<string> Tags { get; private set; }
    
    [BsonElement("categories")]
    public List<string> Categories { get; private set; }
    
    [BsonElement("keywords")]
    public List<string> Keywords { get; private set; }
    
    [BsonElement("lastIndexed")]
    public DateTime? LastIndexed { get; private set; }
    
    [BsonElement("indexVersion")]
    public int IndexVersion { get; private set; }

    public SearchIndex()
    {
        SearchableText = string.Empty;
        Tags = new List<string>();
        Categories = new List<string>();
        Keywords = new List<string>();
        IndexVersion = 1;
    }

    public void UpdateSearchableText(string text)
    {
        SearchableText = text ?? string.Empty;
        UpdateLastIndexed();
    }

    public void AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateLastIndexed();
        }
    }

    public void RemoveTag(string tag)
    {
        if (Tags.Remove(tag))
        {
            UpdateLastIndexed();
        }
    }

    public void AddCategory(string category)
    {
        if (!string.IsNullOrWhiteSpace(category) && !Categories.Contains(category))
        {
            Categories.Add(category);
            UpdateLastIndexed();
        }
    }

    public void RemoveCategory(string category)
    {
        if (Categories.Remove(category))
        {
            UpdateLastIndexed();
        }
    }

    public void AddKeyword(string keyword)
    {
        if (!string.IsNullOrWhiteSpace(keyword) && !Keywords.Contains(keyword))
        {
            Keywords.Add(keyword);
            UpdateLastIndexed();
        }
    }

    public void RemoveKeyword(string keyword)
    {
        if (Keywords.Remove(keyword))
        {
            UpdateLastIndexed();
        }
    }

    public void UpdateLastIndexed()
    {
        LastIndexed = DateTime.UtcNow;
        IndexVersion++;
    }

    public void RebuildIndex(string name, string description, List<string> tags, List<string> categories)
    {
        var searchableText = $"{name} {description}";
        
        if (tags.Any())
        {
            searchableText += " " + string.Join(" ", tags);
        }
        
        if (categories.Any())
        {
            searchableText += " " + string.Join(" ", categories);
        }
        
        SearchableText = searchableText.ToLower();
        Tags = tags.ToList();
        Categories = categories.ToList();
        UpdateLastIndexed();
    }
}
