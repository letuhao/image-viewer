using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoStorageLocationRepository : MongoRepository<StorageLocation>, IStorageLocationRepository
{
    public MongoStorageLocationRepository(IMongoDatabase database, ILogger<MongoStorageLocationRepository> logger)
        : base(database.GetCollection<StorageLocation>("storageLocations"), logger)
    {
    }

    public async Task<IEnumerable<StorageLocation>> GetByTypeAsync(string storageType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(location => location.Type == storageType)
                .SortBy(location => location.Name)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get storage locations for type {StorageType}", storageType);
            throw;
        }
    }

    public async Task<IEnumerable<StorageLocation>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(location => location.IsActive == true)
                .SortBy(location => location.Name)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active storage locations");
            throw;
        }
    }

    public async Task<IEnumerable<StorageLocation>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(location => location.Name.Contains(provider))
                .SortBy(location => location.Name)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get storage locations for provider {Provider}", provider);
            throw;
        }
    }

    public async Task<StorageLocation?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(location => location.IsDefault == true).FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get default storage location");
            throw;
        }
    }
}
