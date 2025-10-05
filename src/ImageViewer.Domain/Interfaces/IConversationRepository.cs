using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for Conversation entity
/// </summary>
public interface IConversationRepository : IRepository<Conversation>
{
    Task<IEnumerable<Conversation>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Conversation>> GetByParticipantIdAsync(ObjectId participantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Conversation>> GetUnreadByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByParticipantsAsync(IEnumerable<ObjectId> participantIds, CancellationToken cancellationToken = default);
}