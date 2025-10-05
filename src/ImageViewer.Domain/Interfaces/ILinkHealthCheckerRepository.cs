using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for LinkHealthChecker entity
/// </summary>
public interface ILinkHealthCheckerRepository : IRepository<LinkHealthChecker>
{
    Task<IEnumerable<LinkHealthChecker>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<LinkHealthChecker>> GetByHealthStatusAsync(string healthStatus, CancellationToken cancellationToken = default);
    Task<IEnumerable<LinkHealthChecker>> GetByLinkIdAsync(ObjectId linkId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LinkHealthChecker>> GetUnhealthyLinksAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<LinkHealthChecker>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
