using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Content similarity entity - represents content similarity analysis and recommendations
/// </summary>
public class ContentSimilarity : BaseEntity
{
    [BsonElement("sourceId")]
    public ObjectId SourceId { get; private set; }

    [BsonElement("sourceType")]
    public string SourceType { get; private set; } = string.Empty; // Image, Collection, User, Tag

    [BsonElement("targetId")]
    public ObjectId TargetId { get; private set; }

    [BsonElement("targetType")]
    public string TargetType { get; private set; } = string.Empty; // Image, Collection, User, Tag

    [BsonElement("similarityScore")]
    public double SimilarityScore { get; private set; } = 0.0; // 0.0 to 1.0

    [BsonElement("similarityType")]
    public string SimilarityType { get; private set; } = "Visual"; // Visual, Metadata, Tag, User, Content

    [BsonElement("algorithm")]
    public string Algorithm { get; private set; } = string.Empty; // PerceptualHash, ColorHistogram, SIFT, etc.

    [BsonElement("confidence")]
    public double Confidence { get; private set; } = 0.0; // 0.0 to 1.0

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isVerified")]
    public bool IsVerified { get; private set; } = false;

    [BsonElement("verificationScore")]
    public double VerificationScore { get; private set; } = 0.0;

    [BsonElement("verifiedBy")]
    public ObjectId? VerifiedBy { get; private set; }

    [BsonElement("verifiedAt")]
    public DateTime? VerifiedAt { get; private set; }

    [BsonElement("features")]
    public Dictionary<string, object> Features { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("category")]
    public string? Category { get; private set; }

    [BsonElement("subcategory")]
    public string? Subcategory { get; private set; }

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0;

    [BsonElement("weight")]
    public double Weight { get; private set; } = 1.0;

    [BsonElement("lastCalculated")]
    public DateTime? LastCalculated { get; private set; }

    [BsonElement("calculationDuration")]
    public TimeSpan? CalculationDuration { get; private set; }

    [BsonElement("isDuplicate")]
    public bool IsDuplicate { get; private set; } = false;

    [BsonElement("isNearDuplicate")]
    public bool IsNearDuplicate { get; private set; } = false;

    [BsonElement("isRecommended")]
    public bool IsRecommended { get; private set; } = false;

    [BsonElement("recommendationScore")]
    public double RecommendationScore { get; private set; } = 0.0;

    [BsonElement("usageCount")]
    public int UsageCount { get; private set; } = 0;

    [BsonElement("lastUsed")]
    public DateTime? LastUsed { get; private set; }

    [BsonElement("createdBy")]
    public new ObjectId? CreatedBy { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public MediaItem? SourceItem { get; private set; }

    [BsonIgnore]
    public MediaItem? TargetItem { get; private set; }

    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public User? Verifier { get; private set; }

    // Private constructor for EF Core
    private ContentSimilarity() { }

    public static ContentSimilarity Create(ObjectId sourceId, string sourceType, ObjectId targetId, string targetType, double similarityScore, string similarityType, string algorithm, ObjectId? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("Source type cannot be empty", nameof(sourceType));

        if (string.IsNullOrWhiteSpace(targetType))
            throw new ArgumentException("Target type cannot be empty", nameof(targetType));

        if (string.IsNullOrWhiteSpace(similarityType))
            throw new ArgumentException("Similarity type cannot be empty", nameof(similarityType));

        if (string.IsNullOrWhiteSpace(algorithm))
            throw new ArgumentException("Algorithm cannot be empty", nameof(algorithm));

        if (similarityScore < 0.0 || similarityScore > 1.0)
            throw new ArgumentException("Similarity score must be between 0.0 and 1.0", nameof(similarityScore));

        return new ContentSimilarity
        {
            SourceId = sourceId,
            SourceType = sourceType,
            TargetId = targetId,
            TargetType = targetType,
            SimilarityScore = similarityScore,
            SimilarityType = similarityType,
            Algorithm = algorithm,
            CreatedBy = createdBy,
            IsActive = true,
            IsVerified = false,
            Confidence = 0.0,
            VerificationScore = 0.0,
            Priority = 0,
            Weight = 1.0,
            IsDuplicate = false,
            IsNearDuplicate = false,
            IsRecommended = false,
            RecommendationScore = 0.0,
            UsageCount = 0,
            Features = new Dictionary<string, object>(),
            Metadata = new Dictionary<string, object>(),
            Tags = new List<string>()
        };
    }

    public void UpdateSimilarityScore(double similarityScore)
    {
        if (similarityScore < 0.0 || similarityScore > 1.0)
            throw new ArgumentException("Similarity score must be between 0.0 and 1.0", nameof(similarityScore));

        SimilarityScore = similarityScore;
        UpdateTimestamp();
    }

    public void UpdateConfidence(double confidence)
    {
        if (confidence < 0.0 || confidence > 1.0)
            throw new ArgumentException("Confidence must be between 0.0 and 1.0", nameof(confidence));

        Confidence = confidence;
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetVerified(bool isVerified, ObjectId? verifiedBy = null, double verificationScore = 0.0)
    {
        IsVerified = isVerified;
        VerifiedBy = verifiedBy;
        VerificationScore = verificationScore;
        
        if (isVerified)
        {
            VerifiedAt = DateTime.UtcNow;
        }
        else
        {
            VerifiedAt = null;
        }
        
        UpdateTimestamp();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetWeight(double weight)
    {
        if (weight < 0.0)
            throw new ArgumentException("Weight cannot be negative", nameof(weight));

        Weight = weight;
        UpdateTimestamp();
    }

    public void SetCategory(string? category, string? subcategory = null)
    {
        Category = category;
        Subcategory = subcategory;
        UpdateTimestamp();
    }

    public void SetDuplicateStatus(bool isDuplicate, bool isNearDuplicate = false)
    {
        IsDuplicate = isDuplicate;
        IsNearDuplicate = isNearDuplicate;
        UpdateTimestamp();
    }

    public void SetRecommended(bool isRecommended, double recommendationScore = 0.0)
    {
        IsRecommended = isRecommended;
        RecommendationScore = recommendationScore;
        UpdateTimestamp();
    }

    public void RecordCalculation(TimeSpan duration)
    {
        LastCalculated = DateTime.UtcNow;
        CalculationDuration = duration;
        UpdateTimestamp();
    }

    public void RecordUsage()
    {
        UsageCount++;
        LastUsed = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void AddFeature(string key, object value)
    {
        Features[key] = value;
        UpdateTimestamp();
    }

    public void RemoveFeature(string key)
    {
        Features.Remove(key);
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

    public bool IsHighSimilarity()
    {
        return SimilarityScore >= 0.8;
    }

    public bool IsMediumSimilarity()
    {
        return SimilarityScore >= 0.5 && SimilarityScore < 0.8;
    }

    public bool IsLowSimilarity()
    {
        return SimilarityScore < 0.5;
    }

    public bool IsHighConfidence()
    {
        return Confidence >= 0.8;
    }

    public bool IsReliable()
    {
        return IsVerified || IsHighConfidence();
    }

    public double GetAdjustedScore()
    {
        return SimilarityScore * Weight * (IsVerified ? 1.2 : 1.0);
    }

    public bool ShouldRecommend()
    {
        return IsRecommended && IsActive && SimilarityScore >= 0.6;
    }

    public bool IsRecent()
    {
        return LastCalculated.HasValue && LastCalculated.Value > DateTime.UtcNow.AddDays(-30);
    }

    public bool IsFrequentlyUsed()
    {
        return UsageCount >= 10;
    }
}
