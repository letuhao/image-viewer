using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for User
/// </summary>
public class UserRepository : MongoRepository<User>, IUserRepository
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public UserRepository(IMongoCollection<User> collection, IRefreshTokenRepository refreshTokenRepository, ILogger<UserRepository> logger)
        : base(collection, logger)
    {
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
    }

    public async Task<User> GetByUsernameAsync(string username)
    {
        try
        {
            return await _collection.Find(u => u.Username == username).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user by username {Username}", username);
            throw new RepositoryException($"Failed to get user by username {username}", ex);
        }
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        try
        {
            return await _collection.Find(u => u.Email == email).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user by email {Email}", email);
            throw new RepositoryException($"Failed to get user by email {email}", ex);
        }
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        try
        {
            return await _collection.Find(u => u.IsActive).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active users");
            throw new RepositoryException("Failed to get active users", ex);
        }
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
    {
        try
        {
            // Note: This would need to be implemented based on how roles are stored in User entity
            // For now, returning empty list as role property is not defined in current User entity
            return new List<User>();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get users by role {Role}", role);
            throw new RepositoryException($"Failed to get users by role {role}", ex);
        }
    }

    public async Task<long> GetUserCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user count");
            throw new RepositoryException("Failed to get user count", ex);
        }
    }

    public async Task<long> GetActiveUserCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(u => u.IsActive);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active user count");
            throw new RepositoryException("Failed to get active user count", ex);
        }
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string query)
    {
        try
        {
            var filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.Username, new BsonRegularExpression(query, "i")),
                Builders<User>.Filter.Regex(u => u.Email, new BsonRegularExpression(query, "i")),
                Builders<User>.Filter.Regex(u => u.Profile.FirstName, new BsonRegularExpression(query, "i")),
                Builders<User>.Filter.Regex(u => u.Profile.LastName, new BsonRegularExpression(query, "i")),
                Builders<User>.Filter.Regex(u => u.Profile.DisplayName, new BsonRegularExpression(query, "i"))
            );
            
            return await _collection.Find(filter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to search users with query {Query}", query);
            throw new RepositoryException($"Failed to search users with query {query}", ex);
        }
    }

    public async Task<IEnumerable<User>> GetUsersByFilterAsync(UserFilter filter)
    {
        try
        {
            var builder = Builders<User>.Filter;
            var filters = new List<FilterDefinition<User>>();

            if (filter.IsActive.HasValue)
            {
                filters.Add(builder.Eq(u => u.IsActive, filter.IsActive.Value));
            }

            if (filter.IsEmailVerified.HasValue)
            {
                filters.Add(builder.Eq(u => u.IsEmailVerified, filter.IsEmailVerified.Value));
            }

            if (filter.CreatedAfter.HasValue)
            {
                filters.Add(builder.Gte(u => u.CreatedAt, filter.CreatedAfter.Value));
            }

            if (filter.CreatedBefore.HasValue)
            {
                filters.Add(builder.Lte(u => u.CreatedAt, filter.CreatedBefore.Value));
            }

            if (filter.Location != null)
            {
                filters.Add(builder.Eq(u => u.Profile.Location, filter.Location));
            }

            if (filter.Language != null)
            {
                filters.Add(builder.Eq(u => u.Profile.Language, filter.Language));
            }

            if (filter.Timezone != null)
            {
                filters.Add(builder.Eq(u => u.Profile.Timezone, filter.Timezone));
            }

            var combinedFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.Find(combinedFilter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get users by filter");
            throw new RepositoryException("Failed to get users by filter", ex);
        }
    }

    public async Task<ImageViewer.Domain.ValueObjects.UserStatistics> GetUserStatisticsAsync()
    {
        try
        {
            var totalUsers = await _collection.CountDocumentsAsync(_ => true);
            var activeUsers = await _collection.CountDocumentsAsync(u => u.IsActive);
            var verifiedUsers = await _collection.CountDocumentsAsync(u => u.IsEmailVerified);
            
            var now = DateTime.UtcNow;
            var newUsersThisMonth = await _collection.CountDocumentsAsync(u => u.CreatedAt >= now.AddMonths(-1));
            var newUsersThisWeek = await _collection.CountDocumentsAsync(u => u.CreatedAt >= now.AddDays(-7));
            var newUsersToday = await _collection.CountDocumentsAsync(u => u.CreatedAt >= now.AddDays(-1));

            return new ImageViewer.Domain.ValueObjects.UserStatistics
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                VerifiedUsers = verifiedUsers,
                NewUsersThisMonth = newUsersThisMonth,
                NewUsersThisWeek = newUsersThisWeek,
                NewUsersToday = newUsersToday
            };
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user statistics");
            throw new RepositoryException("Failed to get user statistics", ex);
        }
    }

    public async Task<IEnumerable<User>> GetTopUsersByActivityAsync(int limit = 10)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(u => u.Statistics.TotalViews)
                .Limit(limit)
                .ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get top users by activity");
            throw new RepositoryException("Failed to get top users by activity", ex);
        }
    }

    public async Task<IEnumerable<User>> GetRecentUsersAsync(int limit = 10)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(u => u.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get recent users");
            throw new RepositoryException("Failed to get recent users", ex);
        }
    }

    #region Security Methods

    public async Task LogFailedLoginAttemptAsync(ObjectId userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Inc(u => u.FailedLoginAttempts, 1)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            
            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("User {UserId} not found for failed login attempt logging", userId);
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to log failed login attempt for user {UserId}", userId);
            throw new RepositoryException($"Failed to log failed login attempt for user {userId}", ex);
        }
    }

    public async Task LogSuccessfulLoginAsync(ObjectId userId, string ipAddress, string userAgent)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.LastLoginAt, DateTime.UtcNow)
                .Set(u => u.LastLoginIp, ipAddress)
                .Set(u => u.FailedLoginAttempts, 0)
                .Set(u => u.IsLocked, false)
                .Set(u => u.LockedUntil, null)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            
            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("User {UserId} not found for successful login logging", userId);
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to log successful login for user {UserId}", userId);
            throw new RepositoryException($"Failed to log successful login for user {userId}", ex);
        }
    }

    public async Task ClearFailedLoginAttemptsAsync(ObjectId userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.FailedLoginAttempts, 0)
                .Set(u => u.IsLocked, false)
                .Set(u => u.LockedUntil, null)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            
            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("User {UserId} not found for clearing failed login attempts", userId);
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to clear failed login attempts for user {UserId}", userId);
            throw new RepositoryException($"Failed to clear failed login attempts for user {userId}", ex);
        }
    }

    public async Task StoreRefreshTokenAsync(ObjectId userId, string refreshToken, DateTime expiryDate)
    {
        try
        {
            // Create new refresh token entity
            var token = new RefreshToken(userId, refreshToken, expiryDate);
            
            // Store in refresh token repository
            await _refreshTokenRepository.CreateAsync(token);
            
            _logger.LogInformation("Refresh token stored for user {UserId} with expiry {ExpiryDate}", userId, expiryDate);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to store refresh token for user {UserId}", userId);
            throw new RepositoryException($"Failed to store refresh token for user {userId}", ex);
        }
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        try
        {
            // Get refresh token from repository
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (token == null || !token.IsActive)
            {
                _logger.LogInformation("Invalid or expired refresh token");
                return null;
            }

            // Get user by ID from the token
            var user = await GetByIdAsync(token.UserId);
            return user;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user by refresh token");
            throw new RepositoryException("Failed to get user by refresh token", ex);
        }
    }

    public async Task InvalidateRefreshTokenAsync(ObjectId userId, string refreshToken)
    {
        try
        {
            // Get the refresh token
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (token != null && token.UserId == userId)
            {
                // Revoke the token
                token.Revoke();
                await _refreshTokenRepository.UpdateAsync(token);
                
                _logger.LogInformation("Refresh token invalidated for user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Refresh token not found or does not belong to user {UserId}", userId);
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to invalidate refresh token for user {UserId}", userId);
            throw new RepositoryException($"Failed to invalidate refresh token for user {userId}", ex);
        }
    }

    #endregion
}
