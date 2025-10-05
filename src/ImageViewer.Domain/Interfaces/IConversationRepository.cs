using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for Conversation entity
/// </summary>
public interface IConversationRepository : IRepository<Conversation>
{
    /// <summary>
    /// Get conversations by user ID
    /// </summary>
    Task<IEnumerable<Conversation>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get conversations by participant ID
    /// </summary>
    Task<IEnumerable<Conversation>> GetByParticipantIdAsync(ObjectId participantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get conversations by type
    /// </summary>
    Task<IEnumerable<Conversation>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get conversations by status
    /// </summary>
    Task<IEnumerable<Conversation>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get conversations by date range
    /// </summary>
    Task<IEnumerable<Conversation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
