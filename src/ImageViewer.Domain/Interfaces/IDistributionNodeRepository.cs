using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for DistributionNode entity
/// </summary>
public interface IDistributionNodeRepository : IRepository<DistributionNode>
{
    Task<IEnumerable<DistributionNode>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<DistributionNode>> GetByRegionAsync(string region, CancellationToken cancellationToken = default);
    Task<IEnumerable<DistributionNode>> GetActiveNodesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DistributionNode>> GetByNodeTypeAsync(string nodeType, CancellationToken cancellationToken = default);
}
