using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of NotificationTemplate repository
/// </summary>
public class MongoNotificationTemplateRepository : MongoRepository<NotificationTemplate>, INotificationTemplateRepository
{
    public MongoNotificationTemplateRepository(MongoDbContext context, ILogger<MongoNotificationTemplateRepository> logger) 
        : base(context.NotificationTemplates, logger)
    {
    }

    public async Task<IEnumerable<NotificationTemplate>> GetByTemplateTypeAsync(string templateType, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationTemplate>.Filter.Eq(t => t.TemplateType, templateType);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationTemplate>.Filter.Eq(t => t.Category, category);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationTemplate>.Filter.Eq(t => t.IsActive, true);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<NotificationTemplate?> GetByTemplateNameAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationTemplate>.Filter.Eq(t => t.TemplateName, templateName);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationTemplate>> GetByLanguageAsync(string language, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationTemplate>.Filter.Eq(t => t.Language, language);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }
}
