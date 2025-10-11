using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// System maintenance entity - represents system maintenance scheduling and tracking
/// </summary>
public class SystemMaintenance : BaseEntity
{
    [BsonElement("title")]
    public string Title { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("type")]
    public string Type { get; private set; } = "Scheduled"; // Scheduled, Emergency, Preventive, Corrective

    [BsonElement("status")]
    public string Status { get; private set; } = "Planned"; // Planned, InProgress, Completed, Cancelled, Failed

    [BsonElement("priority")]
    public string Priority { get; private set; } = "Medium"; // Low, Medium, High, Critical

    [BsonElement("scheduledStart")]
    public DateTime ScheduledStart { get; private set; }

    [BsonElement("scheduledEnd")]
    public DateTime ScheduledEnd { get; private set; }

    [BsonElement("actualStart")]
    public DateTime? ActualStart { get; private set; }

    [BsonElement("actualEnd")]
    public DateTime? ActualEnd { get; private set; }

    [BsonElement("duration")]
    public long? DurationMs { get; private set; }

    [BsonElement("estimatedDuration")]
    public long? EstimatedDurationMs { get; private set; }

    [BsonElement("affectedSystems")]
    public List<string> AffectedSystems { get; private set; } = new();

    [BsonElement("impact")]
    public string Impact { get; private set; } = "Low"; // Low, Medium, High, Critical

    [BsonElement("affectedUsers")]
    public int? AffectedUsers { get; private set; }

    [BsonElement("notificationSent")]
    public bool NotificationSent { get; private set; } = false;

    [BsonElement("notificationSentAt")]
    public DateTime? NotificationSentAt { get; private set; }

    [BsonElement("assignedTo")]
    public ObjectId? AssignedTo { get; private set; }

    [BsonElement("createdBy")]
    public new ObjectId CreatedBy { get; private set; }

    [BsonElement("completedBy")]
    public ObjectId? CompletedBy { get; private set; }

    [BsonElement("notes")]
    public List<MaintenanceNote> Notes { get; private set; } = new();

    [BsonElement("checklist")]
    public List<MaintenanceTask> Checklist { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("recurringPattern")]
    public RecurringPattern? RecurringPattern { get; private set; }

    [BsonElement("isRecurring")]
    public bool IsRecurring { get; private set; } = false;

    // Navigation properties
    [BsonIgnore]
    public User Creator { get; private set; } = null!;

    [BsonIgnore]
    public User? AssignedUser { get; private set; }

    [BsonIgnore]
    public User? CompletedUser { get; private set; }

    // Private constructor for EF Core
    private SystemMaintenance() { }

    public static SystemMaintenance Create(string title, ObjectId createdBy, DateTime scheduledStart, DateTime scheduledEnd, string? description = null, string type = "Scheduled", string priority = "Medium", string impact = "Low")
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(priority))
            throw new ArgumentException("Priority cannot be empty", nameof(priority));

        if (string.IsNullOrWhiteSpace(impact))
            throw new ArgumentException("Impact cannot be empty", nameof(impact));

        if (scheduledStart >= scheduledEnd)
            throw new ArgumentException("Scheduled start must be before scheduled end");

        return new SystemMaintenance
        {
            Title = title,
            Description = description,
            Type = type,
            Priority = priority,
            Impact = impact,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            CreatedBy = createdBy,
            Status = "Planned",
            AffectedSystems = new List<string>(),
            Notes = new List<MaintenanceNote>(),
            Checklist = new List<MaintenanceTask>(),
            Metadata = new Dictionary<string, object>()
        };
    }

    public void StartMaintenance(ObjectId? assignedTo = null)
    {
        Status = "InProgress";
        ActualStart = DateTime.UtcNow;
        AssignedTo = assignedTo;
        UpdateTimestamp();
    }

    public void CompleteMaintenance(ObjectId completedBy)
    {
        Status = "Completed";
        ActualEnd = DateTime.UtcNow;
        CompletedBy = completedBy;
        
        if (ActualStart.HasValue)
        {
            DurationMs = (long)(ActualEnd.Value - ActualStart.Value).TotalMilliseconds;
        }
        
        UpdateTimestamp();
    }

    public void CancelMaintenance(string? reason = null)
    {
        Status = "Cancelled";
        ActualEnd = DateTime.UtcNow;
        
        if (reason != null)
        {
            AddNote("System", $"Maintenance cancelled: {reason}");
        }
        
        UpdateTimestamp();
    }

    public void FailMaintenance(string reason)
    {
        Status = "Failed";
        ActualEnd = DateTime.UtcNow;
        AddNote("System", $"Maintenance failed: {reason}");
        UpdateTimestamp();
    }

    public void Reschedule(DateTime newStart, DateTime newEnd)
    {
        if (Status == "Completed" || Status == "Cancelled")
            throw new InvalidOperationException("Cannot reschedule completed or cancelled maintenance");

        ScheduledStart = newStart;
        ScheduledEnd = newEnd;
        UpdateTimestamp();
    }

    public void AddAffectedSystem(string system)
    {
        if (string.IsNullOrWhiteSpace(system))
            throw new ArgumentException("System cannot be empty", nameof(system));

        if (!AffectedSystems.Contains(system))
        {
            AffectedSystems.Add(system);
            UpdateTimestamp();
        }
    }

    public void RemoveAffectedSystem(string system)
    {
        AffectedSystems.Remove(system);
        UpdateTimestamp();
    }

    public void AddNote(string author, string content)
    {
        Notes.Add(MaintenanceNote.Create(author, content));
        UpdateTimestamp();
    }

    public void AddTask(string task, bool isCompleted = false)
    {
        Checklist.Add(MaintenanceTask.Create(task, isCompleted));
        UpdateTimestamp();
    }

    public void CompleteTask(string task)
    {
        var taskItem = Checklist.FirstOrDefault(t => t.Task == task);
        if (taskItem != null)
        {
            taskItem.IsCompleted = true;
            taskItem.CompletedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }

    public void SetRecurringPattern(RecurringPattern pattern)
    {
        RecurringPattern = pattern;
        IsRecurring = true;
        UpdateTimestamp();
    }

    public void MarkNotificationSent()
    {
        NotificationSent = true;
        NotificationSentAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetEstimatedDuration(long estimatedDurationMs)
    {
        EstimatedDurationMs = estimatedDurationMs;
        UpdateTimestamp();
    }

    public void SetAffectedUsers(int userCount)
    {
        AffectedUsers = userCount;
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public bool IsOverdue()
    {
        return Status == "InProgress" && ActualStart.HasValue && DateTime.UtcNow > ScheduledEnd;
    }

    public double GetCompletionPercentage()
    {
        if (Checklist.Count == 0) return 0;
        return (double)Checklist.Count(t => t.IsCompleted) / Checklist.Count * 100;
    }
}

/// <summary>
/// Maintenance note entity
/// </summary>
public class MaintenanceNote
{
    [BsonElement("author")]
    public string Author { get; set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("isInternal")]
    public bool IsInternal { get; set; } = false;

    public static MaintenanceNote Create(string author, string content, bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author cannot be empty", nameof(author));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        return new MaintenanceNote
        {
            Author = author,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsInternal = isInternal
        };
    }
}

/// <summary>
/// Maintenance task entity
/// </summary>
public class MaintenanceTask
{
    [BsonElement("task")]
    public string Task { get; set; } = string.Empty;

    [BsonElement("isCompleted")]
    public bool IsCompleted { get; set; } = false;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    public static MaintenanceTask Create(string task, bool isCompleted = false, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(task))
            throw new ArgumentException("Task cannot be empty", nameof(task));

        return new MaintenanceTask
        {
            Task = task,
            IsCompleted = isCompleted,
            CompletedAt = isCompleted ? DateTime.UtcNow : null,
            Notes = notes
        };
    }
}

/// <summary>
/// Recurring pattern for maintenance
/// </summary>
public class RecurringPattern
{
    [BsonElement("frequency")]
    public string Frequency { get; set; } = string.Empty; // Daily, Weekly, Monthly, Quarterly, Yearly

    [BsonElement("interval")]
    public int Interval { get; set; } = 1;

    [BsonElement("daysOfWeek")]
    public List<int> DaysOfWeek { get; set; } = new(); // 0 = Sunday, 1 = Monday, etc.

    [BsonElement("dayOfMonth")]
    public int? DayOfMonth { get; set; }

    [BsonElement("endDate")]
    public DateTime? EndDate { get; set; }

    [BsonElement("maxOccurrences")]
    public int? MaxOccurrences { get; set; }

    public static RecurringPattern Create(string frequency, int interval = 1, List<int>? daysOfWeek = null, int? dayOfMonth = null, DateTime? endDate = null, int? maxOccurrences = null)
    {
        if (string.IsNullOrWhiteSpace(frequency))
            throw new ArgumentException("Frequency cannot be empty", nameof(frequency));

        return new RecurringPattern
        {
            Frequency = frequency,
            Interval = interval,
            DaysOfWeek = daysOfWeek ?? new List<int>(),
            DayOfMonth = dayOfMonth,
            EndDate = endDate,
            MaxOccurrences = maxOccurrences
        };
    }
}
