using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoFileStorageMappingRepository : MongoRepository<FileStorageMapping>, IFileStorageMappingRepository
{
    public MongoFileStorageMappingRepository(IMongoDatabase database, ILogger<MongoFileStorageMappingRepository> logger)
        : base(database.GetCollection<FileStorageMapping>("fileStorageMappings"), logger)
    {
    }

    public async Task<FileStorageMapping?> GetByFileIdAsync(ObjectId fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(mapping => mapping.FileId == fileId).FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get file storage mapping for file {FileId}", fileId);
            throw;
        }
    }

    public async Task<IEnumerable<FileStorageMapping>> GetByStorageLocationIdAsync(ObjectId storageLocationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(mapping => mapping.StorageLocationId == storageLocationId)
                .SortByDescending(mapping => mapping.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get file storage mappings for storage location {StorageLocationId}", storageLocationId);
            throw;
        }
    }

    public async Task<IEnumerable<FileStorageMapping>> GetByStorageTypeAsync(string storageType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(mapping => mapping.IsPrimary == true)
                .SortByDescending(mapping => mapping.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get file storage mappings for storage type {StorageType}", storageType);
            throw;
        }
    }

    public async Task<IEnumerable<FileStorageMapping>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(mapping => mapping.IsPrimary == true)
                .SortByDescending(mapping => mapping.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get file storage mappings for status {Status}", status);
            throw;
        }
    }
}
