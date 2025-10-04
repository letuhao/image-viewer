using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// SearchAnalytics - represents search analytics and metrics
/// </summary>
public class SearchAnalytics : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId? UserId { get; private set; }

    [BsonElement("searchQuery")]
    public string SearchQuery { get; private set; } = string.Empty;

    [BsonElement("searchType")]
    public string SearchType { get; private set; } = string.Empty;

    [BsonElement("resultsCount")]
    public int ResultsCount { get; private set; }

    [BsonElement("clickedResults")]
    public List<ObjectId> ClickedResults { get; private set; } = new();

    [BsonElement("searchDuration")]
    public TimeSpan? SearchDuration { get; private set; }

    [BsonElement("isSuccessful")]
    public bool IsSuccessful { get; private set; }

    [BsonElement("filtersApplied")]
    public Dictionary<string, object> FiltersApplied { get; private set; } = new();

    [BsonElement("sortOrder")]
    public string? SortOrder { get; private set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; private set; }

    [BsonElement("sessionId")]
    public string? SessionId { get; private set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; private set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; private set; }

    // Navigation properties
    public User? User { get; private set; }

    // Private constructor for MongoDB
    private SearchAnalytics() { }

    public SearchAnalytics(string searchQuery, string searchType, ObjectId? userId = null)
    {
        SearchQuery = searchQuery ?? throw new ArgumentNullException(nameof(searchQuery));
        SearchType = searchType ?? throw new ArgumentNullException(nameof(searchType));
        UserId = userId;
        ResultsCount = 0;
        IsSuccessful = false;
        Timestamp = DateTime.UtcNow;
    }

    public void SetResults(int resultsCount, bool isSuccessful)
    {
        ResultsCount = resultsCount;
        IsSuccessful = isSuccessful;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddClickedResult(ObjectId resultId)
    {
        if (!ClickedResults.Contains(resultId))
        {
            ClickedResults.Add(resultId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetSearchDuration(TimeSpan duration)
    {
        SearchDuration = duration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddFilter(string filterName, object filterValue)
    {
        FiltersApplied[filterName] = filterValue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSortOrder(string sortOrder)
    {
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSessionInfo(string sessionId, string? ipAddress, string? userAgent)
    {
        SessionId = sessionId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        UpdatedAt = DateTime.UtcNow;
    }
}
