using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Generic MongoDB repository implementation
/// </summary>
public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;
    protected readonly ILogger<MongoRepository<T>> _logger;

    public MongoRepository(IMongoCollection<T> collection, ILogger<MongoRepository<T>> logger)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MongoRepository(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<T>(collectionName);
        _logger = null!; // TODO: Inject logger properly
    }

    public virtual async Task<T> GetByIdAsync(ObjectId id)
    {
        try
        {
            return await _collection.Find(entity => entity.Id == id).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get entity with ID {Id}", id);
            throw new RepositoryException($"Failed to get entity with ID {id}", ex);
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get all entities");
            throw new RepositoryException("Failed to get all entities", ex);
        }
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        try
        {
            await _collection.InsertOneAsync(entity);
            return entity;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateEntityException($"Entity with ID {entity.Id} already exists", ex);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to create entity with ID {Id}", entity.Id);
            throw new RepositoryException($"Failed to create entity with ID {entity.Id}", ex);
        }
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        try
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
            var result = await _collection.ReplaceOneAsync(filter, entity);
            
            if (result.MatchedCount == 0)
            {
                throw new EntityNotFoundException($"Entity with ID {entity.Id} not found");
            }
            
            return entity;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to update entity with ID {Id}", entity.Id);
            throw new RepositoryException($"Failed to update entity with ID {entity.Id}", ex);
        }
    }

    public virtual async Task DeleteAsync(ObjectId id)
    {
        try
        {
            var result = await _collection.DeleteOneAsync(entity => entity.Id == id);
            
            if (result.DeletedCount == 0)
            {
                throw new EntityNotFoundException($"Entity with ID {id} not found");
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to delete entity with ID {Id}", id);
            throw new RepositoryException($"Failed to delete entity with ID {id}", ex);
        }
    }

    public virtual async Task<bool> ExistsAsync(ObjectId id)
    {
        try
        {
            var count = await _collection.CountDocumentsAsync(entity => entity.Id == id);
            return count > 0;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to check if entity exists with ID {Id}", id);
            throw new RepositoryException($"Failed to check if entity exists with ID {id}", ex);
        }
    }

    public virtual async Task<long> CountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to count entities");
            throw new RepositoryException("Failed to count entities", ex);
        }
    }

    public virtual async Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter)
    {
        try
        {
            return await _collection.Find(filter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to find entities with filter");
            throw new RepositoryException("Failed to find entities with filter", ex);
        }
    }

    public virtual async Task<T> FindOneAsync(FilterDefinition<T> filter)
    {
        try
        {
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to find entity with filter");
            throw new RepositoryException("Failed to find entity with filter", ex);
        }
    }

    public virtual async Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter, SortDefinition<T> sort, int limit = 0, int skip = 0)
    {
        try
        {
            var query = _collection.Find(filter);
            
            if (sort != null)
            {
                query = query.Sort(sort);
            }
            
            if (skip > 0)
            {
                query = query.Skip(skip);
            }
            
            if (limit > 0)
            {
                query = query.Limit(limit);
            }
            
            return await query.ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to find entities with filter, sort, limit, and skip");
            throw new RepositoryException("Failed to find entities with filter, sort, limit, and skip", ex);
        }
    }

    public virtual async Task<long> CountAsync(FilterDefinition<T> filter)
    {
        try
        {
            return await _collection.CountDocumentsAsync(filter);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to count entities with filter");
            throw new RepositoryException("Failed to count entities with filter", ex);
        }
    }

    public virtual async Task<UpdateResult> UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update)
    {
        try
        {
            return await _collection.UpdateManyAsync(filter, update);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to update many entities");
            throw new RepositoryException("Failed to update many entities", ex);
        }
    }

    public virtual async Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter)
    {
        try
        {
            return await _collection.DeleteManyAsync(filter);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to delete many entities");
            throw new RepositoryException("Failed to delete many entities", ex);
        }
    }
}