using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Refresh token entity for JWT authentication
/// </summary>
public class RefreshToken : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("token")]
    public string Token { get; private set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; private set; }

    [BsonElement("isRevoked")]
    public bool IsRevoked { get; private set; }

    [BsonElement("createdByIp")]
    public string? CreatedByIp { get; private set; }

    [BsonElement("revokedByIp")]
    public string? RevokedByIp { get; private set; }

    [BsonElement("revokedAt")]
    public DateTime? RevokedAt { get; private set; }

    [BsonElement("replacedByToken")]
    public string? ReplacedByToken { get; private set; }

    // Private constructor for MongoDB
    private RefreshToken() { }

    public RefreshToken(ObjectId userId, string token, DateTime expiresAt, string? createdByIp = null)
    {
        UserId = userId;
        Token = token ?? throw new ArgumentNullException(nameof(token));
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
        IsRevoked = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? revokedByIp = null, string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
        UpdatedAt = DateTime.UtcNow;
    }
}
