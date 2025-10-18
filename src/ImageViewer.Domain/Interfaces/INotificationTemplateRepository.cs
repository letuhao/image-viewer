using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for NotificationTemplate entity
/// </summary>
public interface INotificationTemplateRepository : IRepository<NotificationTemplate>
{
    Task<IEnumerable<NotificationTemplate>> GetByTemplateTypeAsync(string templateType, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
    Task<NotificationTemplate?> GetByTemplateNameAsync(string templateName, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationTemplate>> GetByLanguageAsync(string language, CancellationToken cancellationToken = default);
}
