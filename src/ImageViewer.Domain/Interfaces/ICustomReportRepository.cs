using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for CustomReport entity
/// </summary>
public interface ICustomReportRepository : IRepository<CustomReport>
{
    Task<IEnumerable<CustomReport>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomReport>> GetByReportTypeAsync(string reportType, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomReport>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomReport>> GetPublicReportsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomReport>> GetScheduledReportsAsync(CancellationToken cancellationToken = default);
}
