using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Generic repository interface for MongoDB operations
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    #region Basic CRUD Operations
    
    Task<T> GetByIdAsync(ObjectId id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(ObjectId id);
    Task<bool> ExistsAsync(ObjectId id);
    Task<long> CountAsync();
    
    #endregion
    
    #region Advanced Query Operations
    
    Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter);
    Task<T> FindOneAsync(FilterDefinition<T> filter);
    Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter, SortDefinition<T> sort, int limit = 0, int skip = 0);
    Task<long> CountAsync(FilterDefinition<T> filter);
    
    #endregion
    
    #region Bulk Operations
    
    Task<UpdateResult> UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update);
    Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter);
    
    #endregion
}