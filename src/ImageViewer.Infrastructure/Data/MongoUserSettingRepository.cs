using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for UserSetting entity
/// </summary>
public class MongoUserSettingRepository : MongoRepository<UserSetting>, IUserSettingRepository
{
    public MongoUserSettingRepository(IMongoDatabase database, ILogger<MongoUserSettingRepository> logger) 
        : base(database.GetCollection<UserSetting>("userSettings"), logger)
    {
    }

    public async Task<UserSetting?> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(setting => setting.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user settings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<UserSetting>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(setting => setting.Category == category)
                .SortByDescending(setting => setting.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user settings for category {Category}", category);
            throw;
        }
    }

    public async Task<IEnumerable<UserSetting>> GetBySettingKeyAsync(string settingKey, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(setting => setting.SettingKey == settingKey)
                .SortByDescending(setting => setting.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user settings for setting key {SettingKey}", settingKey);
            throw;
        }
    }
}
