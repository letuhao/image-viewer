using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Premium feature entity - represents premium features and subscriptions
/// </summary>
public class PremiumFeature : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("type")]
    public string Type { get; private set; } = string.Empty; // Storage, Bandwidth, Advanced, Priority, etc.

    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty; // Basic, Pro, Enterprise, etc.

    [BsonElement("tier")]
    public string Tier { get; private set; } = "Basic"; // Basic, Silver, Gold, Platinum, Diamond

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isVisible")]
    public bool IsVisible { get; private set; } = true;

    [BsonElement("isDefault")]
    public bool IsDefault { get; private set; } = false;

    [BsonElement("isPopular")]
    public bool IsPopular { get; private set; } = false;

    [BsonElement("isRecommended")]
    public bool IsRecommended { get; private set; } = false;

    [BsonElement("price")]
    public decimal Price { get; private set; } = 0;

    [BsonElement("currency")]
    public string Currency { get; private set; } = "USD";

    [BsonElement("billingPeriod")]
    public string BillingPeriod { get; private set; } = "Monthly"; // Monthly, Quarterly, Yearly, Lifetime

    [BsonElement("trialPeriodDays")]
    public int? TrialPeriodDays { get; private set; }

    [BsonElement("maxUsers")]
    public int? MaxUsers { get; private set; }

    [BsonElement("maxStorage")]
    public long? MaxStorage { get; private set; } // bytes

    [BsonElement("maxBandwidth")]
    public long? MaxBandwidth { get; private set; } // bytes per month

    [BsonElement("maxCollections")]
    public int? MaxCollections { get; private set; }

    [BsonElement("maxImages")]
    public int? MaxImages { get; private set; }

    [BsonElement("features")]
    public List<string> Features { get; private set; } = new();

    [BsonElement("benefits")]
    public List<string> Benefits { get; private set; } = new();

    [BsonElement("limitations")]
    public List<string> Limitations { get; private set; } = new();

    [BsonElement("restrictions")]
    public List<string> Restrictions { get; private set; } = new();

    [BsonElement("iconUrl")]
    public string? IconUrl { get; private set; }

    [BsonElement("bannerUrl")]
    public string? BannerUrl { get; private set; }

    [BsonElement("color")]
    public string? Color { get; private set; } // Hex color code

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; private set; } = 0;

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("createdBy")]
    public ObjectId? CreatedBy { get; private set; }

    [BsonElement("parentFeatureId")]
    public ObjectId? ParentFeatureId { get; private set; }

    [BsonElement("childFeatures")]
    public List<ObjectId> ChildFeatures { get; private set; } = new();

    [BsonElement("subscriptionCount")]
    public int SubscriptionCount { get; private set; } = 0;

    [BsonElement("activeSubscriptions")]
    public int ActiveSubscriptions { get; private set; } = 0;

    [BsonElement("revenue")]
    public decimal Revenue { get; private set; } = 0;

    [BsonElement("lastUpdated")]
    public DateTime? LastUpdated { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public PremiumFeature? ParentFeature { get; private set; }

    // Private constructor for EF Core
    private PremiumFeature() { }

    public static PremiumFeature Create(string name, string type, string category, string tier, decimal price, ObjectId? createdBy = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        if (string.IsNullOrWhiteSpace(tier))
            throw new ArgumentException("Tier cannot be empty", nameof(tier));

        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        return new PremiumFeature
        {
            Name = name,
            Type = type,
            Category = category,
            Tier = tier,
            Price = price,
            Description = description,
            CreatedBy = createdBy,
            IsActive = true,
            IsVisible = true,
            IsDefault = false,
            IsPopular = false,
            IsRecommended = false,
            Currency = "USD",
            BillingPeriod = "Monthly",
            Priority = 0,
            DisplayOrder = 0,
            SubscriptionCount = 0,
            ActiveSubscriptions = 0,
            Revenue = 0,
            Features = new List<string>(),
            Benefits = new List<string>(),
            Limitations = new List<string>(),
            Restrictions = new List<string>(),
            Tags = new List<string>(),
            Metadata = new Dictionary<string, object>(),
            ChildFeatures = new List<ObjectId>()
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

    public void UpdatePricing(decimal price, string? currency = null, string? billingPeriod = null)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        Price = price;
        
        if (!string.IsNullOrEmpty(currency))
            Currency = currency;
            
        if (!string.IsNullOrEmpty(billingPeriod))
            BillingPeriod = billingPeriod;
            
        UpdateTimestamp();
    }

    public void SetTrialPeriod(int? trialPeriodDays)
    {
        TrialPeriodDays = trialPeriodDays;
        UpdateTimestamp();
    }

    public void SetLimits(int? maxUsers, long? maxStorage, long? maxBandwidth, int? maxCollections, int? maxImages)
    {
        MaxUsers = maxUsers;
        MaxStorage = maxStorage;
        MaxBandwidth = maxBandwidth;
        MaxCollections = maxCollections;
        MaxImages = maxImages;
        UpdateTimestamp();
    }

    public void SetVisibility(bool isActive, bool isVisible, bool isDefault, bool isPopular, bool isRecommended)
    {
        IsActive = isActive;
        IsVisible = isVisible;
        IsDefault = isDefault;
        IsPopular = isPopular;
        IsRecommended = isRecommended;
        UpdateTimestamp();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        UpdateTimestamp();
    }

    public void SetUrls(string? iconUrl, string? bannerUrl)
    {
        IconUrl = iconUrl;
        BannerUrl = bannerUrl;
        UpdateTimestamp();
    }

    public void SetColor(string? color)
    {
        Color = color;
        UpdateTimestamp();
    }

    public void SetParentFeature(ObjectId? parentFeatureId)
    {
        ParentFeatureId = parentFeatureId;
        UpdateTimestamp();
    }

    public void AddChildFeature(ObjectId childFeatureId)
    {
        if (!ChildFeatures.Contains(childFeatureId))
        {
            ChildFeatures.Add(childFeatureId);
            UpdateTimestamp();
        }
    }

    public void RemoveChildFeature(ObjectId childFeatureId)
    {
        ChildFeatures.Remove(childFeatureId);
        UpdateTimestamp();
    }

    public void AddFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature))
            throw new ArgumentException("Feature cannot be empty", nameof(feature));

        if (!Features.Contains(feature))
        {
            Features.Add(feature);
            UpdateTimestamp();
        }
    }

    public void RemoveFeature(string feature)
    {
        Features.Remove(feature);
        UpdateTimestamp();
    }

    public void AddBenefit(string benefit)
    {
        if (string.IsNullOrWhiteSpace(benefit))
            throw new ArgumentException("Benefit cannot be empty", nameof(benefit));

        if (!Benefits.Contains(benefit))
        {
            Benefits.Add(benefit);
            UpdateTimestamp();
        }
    }

    public void RemoveBenefit(string benefit)
    {
        Benefits.Remove(benefit);
        UpdateTimestamp();
    }

    public void AddLimitation(string limitation)
    {
        if (string.IsNullOrWhiteSpace(limitation))
            throw new ArgumentException("Limitation cannot be empty", nameof(limitation));

        if (!Limitations.Contains(limitation))
        {
            Limitations.Add(limitation);
            UpdateTimestamp();
        }
    }

    public void RemoveLimitation(string limitation)
    {
        Limitations.Remove(limitation);
        UpdateTimestamp();
    }

    public void AddRestriction(string restriction)
    {
        if (string.IsNullOrWhiteSpace(restriction))
            throw new ArgumentException("Restriction cannot be empty", nameof(restriction));

        if (!Restrictions.Contains(restriction))
        {
            Restrictions.Add(restriction);
            UpdateTimestamp();
        }
    }

    public void RemoveRestriction(string restriction)
    {
        Restrictions.Remove(restriction);
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

    public void RecordSubscription()
    {
        SubscriptionCount++;
        UpdateTimestamp();
    }

    public void RecordActiveSubscription()
    {
        ActiveSubscriptions++;
        UpdateTimestamp();
    }

    public void AddRevenue(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Revenue amount cannot be negative", nameof(amount));

        Revenue += amount;
        UpdateTimestamp();
    }

    public bool IsAvailable()
    {
        return IsActive && IsVisible;
    }

    public bool IsPremium()
    {
        return Price > 0;
    }

    public bool HasTrial()
    {
        return TrialPeriodDays.HasValue && TrialPeriodDays.Value > 0;
    }

    public bool IsUnlimited()
    {
        return !MaxUsers.HasValue && !MaxStorage.HasValue && !MaxBandwidth.HasValue && 
               !MaxCollections.HasValue && !MaxImages.HasValue;
    }

    public decimal GetMonthlyPrice()
    {
        return BillingPeriod switch
        {
            "Monthly" => Price,
            "Quarterly" => Price / 3,
            "Yearly" => Price / 12,
            "Lifetime" => Price,
            _ => Price
        };
    }

    public decimal GetYearlyPrice()
    {
        return BillingPeriod switch
        {
            "Monthly" => Price * 12,
            "Quarterly" => Price * 4,
            "Yearly" => Price,
            "Lifetime" => Price,
            _ => Price * 12
        };
    }

    public double GetConversionRate()
    {
        if (SubscriptionCount == 0) return 0;
        return (double)ActiveSubscriptions / SubscriptionCount * 100;
    }
}
