using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoSystemSettingRepository : MongoRepository<SystemSetting>, ISystemSettingRepository
{
    public MongoSystemSettingRepository(IMongoDatabase database, ILogger<MongoSystemSettingRepository> logger)
        : base(database.GetCollection<SystemSetting>("systemSettings"), logger)
    {
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(setting => setting.SettingKey == key).FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get system setting for key {Key}", key);
            throw;
        }
    }

    public async Task<IEnumerable<SystemSetting>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(setting => setting.Category == category)
                .SortBy(setting => setting.SettingKey)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get system settings for category {Category}", category);
            throw;
        }
    }

    public async Task<IEnumerable<SystemSetting>> GetByTypeAsync(string settingType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(setting => setting.SettingType == settingType)
                .SortBy(setting => setting.SettingKey)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get system settings for type {SettingType}", settingType);
            throw;
        }
    }

    public async Task<IEnumerable<SystemSetting>> GetPublicSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(setting => setting.IsReadOnly == false)
                .SortBy(setting => setting.Category)
                .ThenBy(setting => setting.SettingKey)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get public system settings");
            throw;
        }
    }
}
