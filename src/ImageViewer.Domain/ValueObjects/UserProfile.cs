using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// User profile value object
/// </summary>
public class UserProfile
{
    [BsonElement("firstName")]
    public string FirstName { get; private set; }
    
    [BsonElement("lastName")]
    public string LastName { get; private set; }
    
    [BsonElement("displayName")]
    public string DisplayName { get; private set; }
    
    [BsonElement("avatar")]
    public string Avatar { get; private set; }
    
    [BsonElement("bio")]
    public string Bio { get; private set; }
    
    [BsonElement("location")]
    public string Location { get; private set; }
    
    [BsonElement("website")]
    public string Website { get; private set; }
    
    [BsonElement("birthDate")]
    public DateTime? BirthDate { get; private set; }
    
    [BsonElement("gender")]
    public string Gender { get; private set; }
    
    [BsonElement("language")]
    public string Language { get; private set; }
    
    [BsonElement("timezone")]
    public string Timezone { get; private set; }

    public UserProfile()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        DisplayName = string.Empty;
        Avatar = string.Empty;
        Bio = string.Empty;
        Location = string.Empty;
        Website = string.Empty;
        Gender = string.Empty;
        Language = "en";
        Timezone = "UTC";
    }

    public UserProfile(string firstName, string lastName, string displayName = "", string avatar = "", 
        string bio = "", string location = "", string website = "", DateTime? birthDate = null, 
        string gender = "", string language = "en", string timezone = "UTC")
    {
        FirstName = firstName ?? string.Empty;
        LastName = lastName ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        Avatar = avatar ?? string.Empty;
        Bio = bio ?? string.Empty;
        Location = location ?? string.Empty;
        Website = website ?? string.Empty;
        BirthDate = birthDate;
        Gender = gender ?? string.Empty;
        Language = language ?? "en";
        Timezone = timezone ?? "UTC";
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName ?? string.Empty;
        LastName = lastName ?? string.Empty;
        UpdateDisplayName();
    }

    public void UpdateDisplayName(string displayName = "")
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = $"{FirstName} {LastName}".Trim();
        }
        else
        {
            DisplayName = displayName;
        }
    }

    public void UpdateAvatar(string avatar)
    {
        Avatar = avatar ?? string.Empty;
    }

    public void UpdateBio(string bio)
    {
        Bio = bio ?? string.Empty;
    }

    public void UpdateLocation(string location)
    {
        Location = location ?? string.Empty;
    }

    public void UpdateWebsite(string website)
    {
        Website = website ?? string.Empty;
    }

    public void UpdateBirthDate(DateTime? birthDate)
    {
        BirthDate = birthDate;
    }

    public void UpdateGender(string gender)
    {
        Gender = gender ?? string.Empty;
    }

    public void UpdateLanguage(string language)
    {
        Language = language ?? "en";
    }

    public void UpdateTimezone(string timezone)
    {
        Timezone = timezone ?? "UTC";
    }
}
