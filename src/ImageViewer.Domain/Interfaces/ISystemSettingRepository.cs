using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for SystemSetting entity
/// </summary>
public interface ISystemSettingRepository : IRepository<SystemSetting>
{
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemSetting>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemSetting>> GetByTypeAsync(string settingType, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemSetting>> GetPublicSettingsAsync(CancellationToken cancellationToken = default);
}
