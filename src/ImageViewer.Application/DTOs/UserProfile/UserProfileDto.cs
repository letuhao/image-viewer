using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.UserProfile;

/// <summary>
/// User profile information
/// </summary>
public class UserProfileDto
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPublic { get; set; }
    public bool IsVerified { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

/// <summary>
/// User profile creation request
/// </summary>
public class CreateUserProfileRequest
{
    public ObjectId UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

/// <summary>
/// User profile update request
/// </summary>
public class UpdateUserProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
    public bool? IsPublic { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// User profile validation result
/// </summary>
public class UserProfileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, string> FieldErrors { get; set; } = new();
}

/// <summary>
/// User profile privacy settings
/// </summary>
public class UserProfilePrivacySettings
{
    public ObjectId UserId { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool ShowEmail { get; set; } = false;
    public bool ShowLocation { get; set; } = true;
    public bool ShowBirthDate { get; set; } = false;
    public bool ShowWebsite { get; set; } = true;
    public bool ShowBio { get; set; } = true;
    public bool ShowTags { get; set; } = true;
    public bool ShowCustomFields { get; set; } = false;
    public bool AllowSearch { get; set; } = true;
    public bool AllowContact { get; set; } = true;
    public List<string> BlockedUsers { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
}

/// <summary>
/// User profile customization settings
/// </summary>
public class UserProfileCustomizationSettings
{
    public ObjectId UserId { get; set; }
    public string Theme { get; set; } = "default";
    public string ColorScheme { get; set; } = "light";
    public string Layout { get; set; } = "grid";
    public int ItemsPerPage { get; set; } = 20;
    public bool ShowThumbnails { get; set; } = true;
    public bool ShowMetadata { get; set; } = true;
    public bool ShowTags { get; set; } = true;
    public bool ShowStatistics { get; set; } = true;
    public string DefaultSort { get; set; } = "name";
    public string DefaultSortOrder { get; set; } = "asc";
    public List<string> CustomFields { get; set; } = new();
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// User profile statistics
/// </summary>
public class UserProfileStatistics
{
    public ObjectId UserId { get; set; }
    public int TotalCollections { get; set; }
    public int TotalMediaItems { get; set; }
    public long TotalStorageUsed { get; set; }
    public int TotalViews { get; set; }
    public int TotalDownloads { get; set; }
    public int TotalShares { get; set; }
    public int TotalLikes { get; set; }
    public int TotalComments { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime ProfileCreated { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, int> ActivityByMonth { get; set; } = new();
    public Dictionary<string, int> PopularTags { get; set; } = new();
    public Dictionary<string, long> StorageByType { get; set; } = new();
}
