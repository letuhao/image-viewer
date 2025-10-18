using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoDistributionNodeRepository : MongoRepository<DistributionNode>, IDistributionNodeRepository
{
    public MongoDistributionNodeRepository(IMongoDatabase database, ILogger<MongoDistributionNodeRepository> logger)
        : base(database.GetCollection<DistributionNode>("distributionNodes"), logger)
    {
    }

    public async Task<IEnumerable<DistributionNode>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(node => node.Status == status)
                .SortByDescending(node => node.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get distribution nodes for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<DistributionNode>> GetByRegionAsync(string region, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(node => node.Region == region)
                .SortByDescending(node => node.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get distribution nodes for region {Region}", region);
            throw;
        }
    }

    public async Task<IEnumerable<DistributionNode>> GetActiveNodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(node => node.Status == "Active")
                .SortByDescending(node => node.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active distribution nodes");
            throw;
        }
    }

    public async Task<IEnumerable<DistributionNode>> GetByNodeTypeAsync(string nodeType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(node => node.Type == nodeType)
                .SortByDescending(node => node.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get distribution nodes for type {NodeType}", nodeType);
            throw;
        }
    }
}
