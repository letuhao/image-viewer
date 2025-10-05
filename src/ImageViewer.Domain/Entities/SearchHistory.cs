using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Search history entity - represents user search queries and results
/// </summary>
public class SearchHistory : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("searchQuery")]
    public string SearchQuery { get; private set; } = string.Empty;

    [BsonElement("searchType")]
    public string SearchType { get; private set; } = "collection"; // collection, image, tag, user

    [BsonElement("filters")]
    public Dictionary<string, object> Filters { get; private set; } = new();

    [BsonElement("resultCount")]
    public int ResultCount { get; private set; }

    [BsonElement("clickedResults")]
    public List<ObjectId> ClickedResults { get; private set; } = new();

    [BsonElement("sessionId")]
    public string SessionId { get; private set; } = string.Empty;

    [BsonElement("ipAddress")]
    public string? IpAddress { get; private set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; private set; }

    [BsonElement("searchDurationMs")]
    public long SearchDurationMs { get; private set; }

    [BsonElement("isAnonymous")]
    public bool IsAnonymous { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User? User { get; private set; }

    // Private constructor for EF Core
    private SearchHistory() { }

    public static SearchHistory Create(ObjectId? userId, string searchQuery, string searchType, Dictionary<string, object>? filters = null, string? sessionId = null, string? ipAddress = null, string? userAgent = null, bool isAnonymous = false)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            throw new ArgumentException("Search query cannot be empty", nameof(searchQuery));

        return new SearchHistory
        {
            UserId = userId ?? ObjectId.Empty,
            SearchQuery = searchQuery,
            SearchType = searchType,
            Filters = filters ?? new Dictionary<string, object>(),
            ResultCount = 0,
            ClickedResults = new List<ObjectId>(),
            SessionId = sessionId ?? Guid.NewGuid().ToString(),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SearchDurationMs = 0,
            IsAnonymous = isAnonymous
        };
    }

    public void UpdateResultCount(int count)
    {
        ResultCount = count;
        UpdateTimestamp();
    }

    public void AddClickedResult(ObjectId resultId)
    {
        if (!ClickedResults.Contains(resultId))
        {
            ClickedResults.Add(resultId);
            UpdateTimestamp();
        }
    }

    public void UpdateSearchDuration(long durationMs)
    {
        SearchDurationMs = durationMs;
        UpdateTimestamp();
    }

    public void UpdateFilters(Dictionary<string, object> filters)
    {
        Filters = filters ?? new Dictionary<string, object>();
        UpdateTimestamp();
    }
}
