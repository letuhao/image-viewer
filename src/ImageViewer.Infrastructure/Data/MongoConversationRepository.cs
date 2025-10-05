using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoConversationRepository : MongoRepository<Conversation>, IConversationRepository
{
    public MongoConversationRepository(IMongoDatabase database, ILogger<MongoConversationRepository> logger)
        : base(database.GetCollection<Conversation>("conversations"), logger)
    {
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(conversation => conversation.Participants.Contains(userId))
                .SortByDescending(conversation => conversation.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get conversations for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<Conversation>> GetByParticipantIdAsync(ObjectId participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(conversation => conversation.Participants.Contains(participantId))
                .SortByDescending(conversation => conversation.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get conversations for participant {ParticipantId}", participantId);
            throw;
        }
    }

    public async Task<IEnumerable<Conversation>> GetUnreadByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(conversation => 
                conversation.Participants.Contains(userId) && 
                conversation.UnreadCount.ContainsKey(userId) &&
                conversation.UnreadCount[userId] > 0)
                .SortByDescending(conversation => conversation.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get unread conversations for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Conversation?> GetByParticipantsAsync(IEnumerable<ObjectId> participantIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var participantList = participantIds.ToList();
            return await _collection.Find(conversation => 
                conversation.Participants.Count == participantList.Count &&
                conversation.Participants.All(id => participantList.Contains(id)))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get conversation by participants");
            throw;
        }
    }
}
