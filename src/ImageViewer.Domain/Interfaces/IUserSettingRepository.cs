using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for UserSetting entity
/// </summary>
public interface IUserSettingRepository : IRepository<UserSetting>
{
    /// <summary>
    /// Get user settings by user ID
    /// </summary>
    Task<UserSetting?> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user settings by preference category
    /// </summary>
    Task<IEnumerable<UserSetting>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user settings by setting key
    /// </summary>
    Task<IEnumerable<UserSetting>> GetBySettingKeyAsync(string settingKey, CancellationToken cancellationToken = default);
}
