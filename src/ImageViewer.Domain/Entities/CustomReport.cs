using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Custom report entity - represents custom reporting and analytics
/// </summary>
public class CustomReport : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("reportType")]
    public string ReportType { get; private set; } = string.Empty; // Analytics, Usage, Performance, Security, Audit, etc.

    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty; // System, User, Content, Performance, etc.

    [BsonElement("status")]
    public string Status { get; private set; } = "Draft"; // Draft, Published, Archived, Scheduled

    [BsonElement("isPublic")]
    public bool IsPublic { get; private set; } = false;

    [BsonElement("isScheduled")]
    public bool IsScheduled { get; private set; } = false;

    [BsonElement("scheduleExpression")]
    public string? ScheduleExpression { get; private set; } // Cron expression

    [BsonElement("lastGenerated")]
    public DateTime? LastGenerated { get; private set; }

    [BsonElement("nextGeneration")]
    public DateTime? NextGeneration { get; private set; }

    [BsonElement("template")]
    public ReportTemplate Template { get; private set; } = new();

    [BsonElement("filters")]
    public List<ReportFilter> Filters { get; private set; } = new();

    [BsonElement("dimensions")]
    public List<string> Dimensions { get; private set; } = new();

    [BsonElement("metrics")]
    public List<string> Metrics { get; private set; } = new();

    [BsonElement("groupBy")]
    public List<string> GroupBy { get; private set; } = new();

    [BsonElement("sortBy")]
    public List<SortCriteria> SortBy { get; private set; } = new();

    [BsonElement("dateRange")]
    public DateRange DateRange { get; private set; } = new();

    [BsonElement("outputFormat")]
    public string OutputFormat { get; private set; } = "JSON"; // JSON, CSV, PDF, Excel, HTML

    [BsonElement("outputDestination")]
    public string OutputDestination { get; private set; } = "Database"; // Database, Email, File, API

    [BsonElement("outputPath")]
    public string? OutputPath { get; private set; }

    [BsonElement("emailRecipients")]
    public List<string> EmailRecipients { get; private set; } = new();

    [BsonElement("isRealTime")]
    public bool IsRealTime { get; private set; } = false;

    [BsonElement("refreshInterval")]
    public TimeSpan? RefreshInterval { get; private set; }

    [BsonElement("dataRetention")]
    public TimeSpan? DataRetention { get; private set; }

    [BsonElement("accessLevel")]
    public string AccessLevel { get; private set; } = "Private"; // Private, Shared, Public

    [BsonElement("allowedUsers")]
    public List<ObjectId> AllowedUsers { get; private set; } = new();

    [BsonElement("allowedGroups")]
    public List<ObjectId> AllowedGroups { get; private set; } = new();

    [BsonElement("allowedRoles")]
    public List<string> AllowedRoles { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("createdBy")]
    public ObjectId CreatedBy { get; private set; }

    [BsonElement("lastModifiedBy")]
    public ObjectId? LastModifiedBy { get; private set; }

    [BsonElement("version")]
    public int Version { get; private set; } = 1;

    [BsonElement("generationCount")]
    public int GenerationCount { get; private set; } = 0;

    [BsonElement("lastAccessed")]
    public DateTime? LastAccessed { get; private set; }

    [BsonElement("accessCount")]
    public int AccessCount { get; private set; } = 0;

    [BsonElement("isFavorite")]
    public bool IsFavorite { get; private set; } = false;

    [BsonElement("favoriteCount")]
    public int FavoriteCount { get; private set; } = 0;

    // Navigation properties
    [BsonIgnore]
    public User Creator { get; private set; } = null!;

    [BsonIgnore]
    public User? LastModifier { get; private set; }

    // Private constructor for EF Core
    private CustomReport() { }

    public static CustomReport Create(string name, string reportType, string category, ObjectId createdBy, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(reportType))
            throw new ArgumentException("Report type cannot be empty", nameof(reportType));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        return new CustomReport
        {
            Name = name,
            ReportType = reportType,
            Category = category,
            Description = description,
            CreatedBy = createdBy,
            Status = "Draft",
            IsPublic = false,
            IsScheduled = false,
            IsRealTime = false,
            AccessLevel = "Private",
            Version = 1,
            GenerationCount = 0,
            AccessCount = 0,
            IsFavorite = false,
            FavoriteCount = 0,
            Template = new ReportTemplate(),
            Filters = new List<ReportFilter>(),
            Dimensions = new List<string>(),
            Metrics = new List<string>(),
            GroupBy = new List<string>(),
            SortBy = new List<SortCriteria>(),
            DateRange = new DateRange(),
            EmailRecipients = new List<string>(),
            AllowedUsers = new List<ObjectId>(),
            AllowedGroups = new List<ObjectId>(),
            AllowedRoles = new List<string>(),
            Tags = new List<string>(),
            Metadata = new Dictionary<string, object>()
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

    public void UpdateStatus(string status, ObjectId? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty", nameof(status));

        Status = status;
        LastModifiedBy = modifiedBy;
        UpdateTimestamp();
    }

    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;
        UpdateTimestamp();
    }

    public void SetAccessLevel(string accessLevel)
    {
        if (string.IsNullOrWhiteSpace(accessLevel))
            throw new ArgumentException("Access level cannot be empty", nameof(accessLevel));

        AccessLevel = accessLevel;
        UpdateTimestamp();
    }

    public void SetScheduled(bool isScheduled, string? scheduleExpression = null)
    {
        IsScheduled = isScheduled;
        ScheduleExpression = scheduleExpression;
        UpdateTimestamp();
    }

    public void SetRealTime(bool isRealTime, TimeSpan? refreshInterval = null)
    {
        IsRealTime = isRealTime;
        RefreshInterval = refreshInterval;
        UpdateTimestamp();
    }

    public void SetOutputFormat(string outputFormat)
    {
        if (string.IsNullOrWhiteSpace(outputFormat))
            throw new ArgumentException("Output format cannot be empty", nameof(outputFormat));

        OutputFormat = outputFormat;
        UpdateTimestamp();
    }

    public void SetOutputDestination(string outputDestination, string? outputPath = null)
    {
        if (string.IsNullOrWhiteSpace(outputDestination))
            throw new ArgumentException("Output destination cannot be empty", nameof(outputDestination));

        OutputDestination = outputDestination;
        OutputPath = outputPath;
        UpdateTimestamp();
    }

    public void SetDataRetention(TimeSpan? dataRetention)
    {
        DataRetention = dataRetention;
        UpdateTimestamp();
    }

    public void UpdateTemplate(ReportTemplate template)
    {
        Template = template ?? new ReportTemplate();
        UpdateTimestamp();
    }

    public void SetDateRange(DateTime? startDate, DateTime? endDate)
    {
        DateRange = new DateRange
        {
            StartDate = startDate,
            EndDate = endDate
        };
        UpdateTimestamp();
    }

    public void AddFilter(ReportFilter filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        Filters.Add(filter);
        UpdateTimestamp();
    }

    public void RemoveFilter(ReportFilter filter)
    {
        Filters.Remove(filter);
        UpdateTimestamp();
    }

    public void AddDimension(string dimension)
    {
        if (string.IsNullOrWhiteSpace(dimension))
            throw new ArgumentException("Dimension cannot be empty", nameof(dimension));

        if (!Dimensions.Contains(dimension))
        {
            Dimensions.Add(dimension);
            UpdateTimestamp();
        }
    }

    public void RemoveDimension(string dimension)
    {
        Dimensions.Remove(dimension);
        UpdateTimestamp();
    }

    public void AddMetric(string metric)
    {
        if (string.IsNullOrWhiteSpace(metric))
            throw new ArgumentException("Metric cannot be empty", nameof(metric));

        if (!Metrics.Contains(metric))
        {
            Metrics.Add(metric);
            UpdateTimestamp();
        }
    }

    public void RemoveMetric(string metric)
    {
        Metrics.Remove(metric);
        UpdateTimestamp();
    }

    public void AddGroupBy(string groupBy)
    {
        if (string.IsNullOrWhiteSpace(groupBy))
            throw new ArgumentException("Group by cannot be empty", nameof(groupBy));

        if (!GroupBy.Contains(groupBy))
        {
            GroupBy.Add(groupBy);
            UpdateTimestamp();
        }
    }

    public void RemoveGroupBy(string groupBy)
    {
        GroupBy.Remove(groupBy);
        UpdateTimestamp();
    }

    public void AddSortCriteria(SortCriteria sortCriteria)
    {
        if (sortCriteria == null)
            throw new ArgumentNullException(nameof(sortCriteria));

        SortBy.Add(sortCriteria);
        UpdateTimestamp();
    }

    public void RemoveSortCriteria(SortCriteria sortCriteria)
    {
        SortBy.Remove(sortCriteria);
        UpdateTimestamp();
    }

    public void AddEmailRecipient(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!EmailRecipients.Contains(email))
        {
            EmailRecipients.Add(email);
            UpdateTimestamp();
        }
    }

    public void RemoveEmailRecipient(string email)
    {
        EmailRecipients.Remove(email);
        UpdateTimestamp();
    }

    public void AddAllowedUser(ObjectId userId)
    {
        if (!AllowedUsers.Contains(userId))
        {
            AllowedUsers.Add(userId);
            UpdateTimestamp();
        }
    }

    public void RemoveAllowedUser(ObjectId userId)
    {
        AllowedUsers.Remove(userId);
        UpdateTimestamp();
    }

    public void AddAllowedGroup(ObjectId groupId)
    {
        if (!AllowedGroups.Contains(groupId))
        {
            AllowedGroups.Add(groupId);
            UpdateTimestamp();
        }
    }

    public void RemoveAllowedGroup(ObjectId groupId)
    {
        AllowedGroups.Remove(groupId);
        UpdateTimestamp();
    }

    public void AddAllowedRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        if (!AllowedRoles.Contains(role))
        {
            AllowedRoles.Add(role);
            UpdateTimestamp();
        }
    }

    public void RemoveAllowedRole(string role)
    {
        AllowedRoles.Remove(role);
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

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void RemoveMetadata(string key)
    {
        Metadata.Remove(key);
        UpdateTimestamp();
    }

    public void RecordGeneration()
    {
        GenerationCount++;
        LastGenerated = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void RecordAccess()
    {
        AccessCount++;
        LastAccessed = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetFavorite(bool isFavorite)
    {
        IsFavorite = isFavorite;
        UpdateTimestamp();
    }

    public void IncrementFavoriteCount()
    {
        FavoriteCount++;
        UpdateTimestamp();
    }

    public void DecrementFavoriteCount()
    {
        if (FavoriteCount > 0)
        {
            FavoriteCount--;
            UpdateTimestamp();
        }
    }

    public void IncrementVersion(ObjectId? modifiedBy = null)
    {
        Version++;
        LastModifiedBy = modifiedBy;
        UpdateTimestamp();
    }

    public bool IsPublished()
    {
        return Status == "Published";
    }

    public bool IsDraft()
    {
        return Status == "Draft";
    }

    public bool IsArchived()
    {
        return Status == "Archived";
    }

    public bool IsAccessibleBy(ObjectId userId)
    {
        return IsPublic || AllowedUsers.Contains(userId) || CreatedBy == userId;
    }

    public bool IsScheduledForGeneration()
    {
        return IsScheduled && NextGeneration.HasValue && NextGeneration.Value <= DateTime.UtcNow;
    }

    public bool IsRealTimeEnabled()
    {
        return IsRealTime && RefreshInterval.HasValue;
    }

    public bool HasExpiredData()
    {
        return DataRetention.HasValue && LastGenerated.HasValue && 
               LastGenerated.Value.Add(DataRetention.Value) <= DateTime.UtcNow;
    }
}

/// <summary>
/// Report template entity
/// </summary>
public class ReportTemplate
{
    [BsonElement("templateId")]
    public string TemplateId { get; set; } = string.Empty;

    [BsonElement("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [BsonElement("layout")]
    public string Layout { get; set; } = "Standard";

    [BsonElement("style")]
    public string Style { get; set; } = "Default";

    [BsonElement("header")]
    public string? Header { get; set; }

    [BsonElement("footer")]
    public string? Footer { get; set; }

    [BsonElement("logo")]
    public string? Logo { get; set; }

    [BsonElement("customFields")]
    public Dictionary<string, object> CustomFields { get; set; } = new();

    public static ReportTemplate Create(string templateId, string templateName)
    {
        return new ReportTemplate
        {
            TemplateId = templateId,
            TemplateName = templateName,
            Layout = "Standard",
            Style = "Default",
            CustomFields = new Dictionary<string, object>()
        };
    }
}

/// <summary>
/// Report filter entity
/// </summary>
public class ReportFilter
{
    [BsonElement("field")]
    public string Field { get; set; } = string.Empty;

    [BsonElement("operator")]
    public string Operator { get; set; } = "equals"; // equals, contains, greater_than, less_than, etc.

    [BsonElement("value")]
    public object Value { get; set; } = string.Empty;

    [BsonElement("dataType")]
    public string DataType { get; set; } = "string"; // string, number, date, boolean

    [BsonElement("isRequired")]
    public bool IsRequired { get; set; } = false;

    public static ReportFilter Create(string field, string operatorType, object value, string dataType = "string")
    {
        return new ReportFilter
        {
            Field = field,
            Operator = operatorType,
            Value = value,
            DataType = dataType,
            IsRequired = false
        };
    }
}

/// <summary>
/// Sort criteria entity
/// </summary>
public class SortCriteria
{
    [BsonElement("field")]
    public string Field { get; set; } = string.Empty;

    [BsonElement("direction")]
    public string Direction { get; set; } = "asc"; // asc, desc

    [BsonElement("priority")]
    public int Priority { get; set; } = 0;

    public static SortCriteria Create(string field, string direction = "asc", int priority = 0)
    {
        return new SortCriteria
        {
            Field = field,
            Direction = direction,
            Priority = priority
        };
    }
}

/// <summary>
/// Date range entity
/// </summary>
public class DateRange
{
    [BsonElement("startDate")]
    public DateTime? StartDate { get; set; }

    [BsonElement("endDate")]
    public DateTime? EndDate { get; set; }

    [BsonElement("isRelative")]
    public bool IsRelative { get; set; } = false;

    [BsonElement("relativePeriod")]
    public string? RelativePeriod { get; set; } // last_7_days, last_30_days, etc.

    public static DateRange Create(DateTime? startDate, DateTime? endDate)
    {
        return new DateRange
        {
            StartDate = startDate,
            EndDate = endDate,
            IsRelative = false
        };
    }

    public static DateRange CreateRelative(string relativePeriod)
    {
        return new DateRange
        {
            IsRelative = true,
            RelativePeriod = relativePeriod
        };
    }
}
