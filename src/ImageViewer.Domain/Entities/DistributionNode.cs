using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Distribution node entity - represents distribution nodes in the network
/// </summary>
public class DistributionNode : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("type")]
    public string Type { get; private set; } = "Edge"; // Edge, Core, CDN, Cache, Origin

    [BsonElement("status")]
    public string Status { get; private set; } = "Active"; // Active, Inactive, Maintenance, Error, Offline

    [BsonElement("url")]
    public string Url { get; private set; } = string.Empty;

    [BsonElement("ipAddress")]
    public string? IpAddress { get; private set; }

    [BsonElement("port")]
    public int? Port { get; private set; }

    [BsonElement("location")]
    public string? Location { get; private set; }

    [BsonElement("region")]
    public string? Region { get; private set; }

    [BsonElement("country")]
    public string? Country { get; private set; }

    [BsonElement("capacity")]
    public long? Capacity { get; private set; } // bytes

    [BsonElement("usedCapacity")]
    public long UsedCapacity { get; private set; } = 0;

    [BsonElement("bandwidth")]
    public long? Bandwidth { get; private set; } // bytes per second

    [BsonElement("usedBandwidth")]
    public long UsedBandwidth { get; private set; } = 0;

    [BsonElement("maxConnections")]
    public int? MaxConnections { get; private set; }

    [BsonElement("currentConnections")]
    public int CurrentConnections { get; private set; } = 0;

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0;

    [BsonElement("isLoadBalanced")]
    public bool IsLoadBalanced { get; private set; } = false;

    [BsonElement("isSecure")]
    public bool IsSecure { get; private set; } = false;

    [BsonElement("sslCertificate")]
    public string? SslCertificate { get; private set; }

    [BsonElement("lastHealthCheck")]
    public DateTime? LastHealthCheck { get; private set; }

    [BsonElement("healthStatus")]
    public string HealthStatus { get; private set; } = "Unknown"; // Healthy, Warning, Error, Unknown

    [BsonElement("responseTime")]
    public long? ResponseTime { get; private set; } // milliseconds

    [BsonElement("uptime")]
    public double Uptime { get; private set; } = 100.0; // percentage

    [BsonElement("errorCount")]
    public int ErrorCount { get; private set; } = 0;

    [BsonElement("lastError")]
    public DateTime? LastError { get; private set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("supportedFormats")]
    public List<string> SupportedFormats { get; private set; } = new();

    [BsonElement("createdBy")]
    public new ObjectId? CreatedBy { get; private set; }

    [BsonElement("parentNodeId")]
    public ObjectId? ParentNodeId { get; private set; }

    [BsonElement("childNodes")]
    public List<ObjectId> ChildNodes { get; private set; } = new();

    // Navigation properties
    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public DistributionNode? ParentNode { get; private set; }

    // Private constructor for EF Core
    private DistributionNode() { }

    public static DistributionNode Create(string name, string type, string url, ObjectId? createdBy = null, ObjectId? parentNodeId = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        return new DistributionNode
        {
            Name = name,
            Type = type,
            Url = url,
            Description = description,
            CreatedBy = createdBy,
            ParentNodeId = parentNodeId,
            Status = "Active",
            Priority = 0,
            IsLoadBalanced = false,
            IsSecure = false,
            HealthStatus = "Unknown",
            Uptime = 100.0,
            ErrorCount = 0,
            UsedCapacity = 0,
            UsedBandwidth = 0,
            CurrentConnections = 0,
            Metadata = new Dictionary<string, object>(),
            Tags = new List<string>(),
            SupportedFormats = new List<string>(),
            ChildNodes = new List<ObjectId>()
        };
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdateTimestamp();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdateTimestamp();
    }

    public void UpdateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        Url = url;
        UpdateTimestamp();
    }

    public void UpdateLocation(string? location, string? region = null, string? country = null)
    {
        Location = location;
        Region = region;
        Country = country;
        UpdateTimestamp();
    }

    public void SetCapacity(long? capacity)
    {
        Capacity = capacity;
        UpdateTimestamp();
    }

    public void UpdateUsedCapacity(long usedCapacity)
    {
        UsedCapacity = usedCapacity;
        UpdateTimestamp();
    }

    public void SetBandwidth(long? bandwidth)
    {
        Bandwidth = bandwidth;
        UpdateTimestamp();
    }

    public void UpdateUsedBandwidth(long usedBandwidth)
    {
        UsedBandwidth = usedBandwidth;
        UpdateTimestamp();
    }

    public void SetMaxConnections(int? maxConnections)
    {
        MaxConnections = maxConnections;
        UpdateTimestamp();
    }

    public void UpdateCurrentConnections(int currentConnections)
    {
        CurrentConnections = currentConnections;
        UpdateTimestamp();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetLoadBalanced(bool isLoadBalanced)
    {
        IsLoadBalanced = isLoadBalanced;
        UpdateTimestamp();
    }

    public void SetSecure(bool isSecure, string? sslCertificate = null)
    {
        IsSecure = isSecure;
        SslCertificate = sslCertificate;
        UpdateTimestamp();
    }

    public void UpdateHealthStatus(string healthStatus, long? responseTime = null)
    {
        if (string.IsNullOrWhiteSpace(healthStatus))
            throw new ArgumentException("Health status cannot be empty", nameof(healthStatus));

        HealthStatus = healthStatus;
        ResponseTime = responseTime;
        LastHealthCheck = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void UpdateUptime(double uptime)
    {
        Uptime = Math.Max(0, Math.Min(100, uptime));
        UpdateTimestamp();
    }

    public void RecordError(string errorMessage)
    {
        ErrorCount++;
        LastError = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        UpdateTimestamp();
    }

    public void ClearErrors()
    {
        ErrorCount = 0;
        LastError = null;
        ErrorMessage = null;
        UpdateTimestamp();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateTimestamp();
        }
    }

    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        UpdateTimestamp();
    }

    public void AddSupportedFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be empty", nameof(format));

        if (!SupportedFormats.Contains(format))
        {
            SupportedFormats.Add(format);
            UpdateTimestamp();
        }
    }

    public void RemoveSupportedFormat(string format)
    {
        SupportedFormats.Remove(format);
        UpdateTimestamp();
    }

    public void AddChildNode(ObjectId childNodeId)
    {
        if (!ChildNodes.Contains(childNodeId))
        {
            ChildNodes.Add(childNodeId);
            UpdateTimestamp();
        }
    }

    public void RemoveChildNode(ObjectId childNodeId)
    {
        ChildNodes.Remove(childNodeId);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public double GetCapacityUsagePercentage()
    {
        if (!Capacity.HasValue || Capacity.Value == 0)
            return 0;

        return (double)UsedCapacity / Capacity.Value * 100;
    }

    public double GetBandwidthUsagePercentage()
    {
        if (!Bandwidth.HasValue || Bandwidth.Value == 0)
            return 0;

        return (double)UsedBandwidth / Bandwidth.Value * 100;
    }

    public bool IsOverloaded()
    {
        return GetCapacityUsagePercentage() > 90 || GetBandwidthUsagePercentage() > 90;
    }

    public bool IsHealthy()
    {
        return HealthStatus == "Healthy" && Uptime > 95 && ErrorCount < 10;
    }

    public bool CanAcceptConnections()
    {
        return Status == "Active" && (!MaxConnections.HasValue || CurrentConnections < MaxConnections.Value);
    }
}
