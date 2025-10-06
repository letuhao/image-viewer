using ImageViewer.Application.DTOs.BulkOperations;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for bulk operations management
/// </summary>
public interface IBulkOperationService
{
    /// <summary>
    /// Performs bulk import of collections and media items
    /// </summary>
    /// <param name="request">Bulk import request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResultDto> BulkImportAsync(BulkImportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk export of collections and media items
    /// </summary>
    /// <param name="request">Bulk export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResultDto> BulkExportAsync(BulkExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk update of collections and media items
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResultDto> BulkUpdateAsync(BulkUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk delete of collections and media items
    /// </summary>
    /// <param name="request">Bulk delete request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResultDto> BulkDeleteAsync(BulkDeleteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk validation of collections and media items
    /// </summary>
    /// <param name="request">Bulk validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResultDto> BulkValidateAsync(BulkValidationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the progress of a bulk operation
    /// </summary>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation progress</returns>
    Task<BulkOperationProgress> GetOperationProgressAsync(ObjectId operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running bulk operation
    /// </summary>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation was cancelled successfully</returns>
    Task<bool> CancelOperationAsync(ObjectId operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the result of a completed bulk operation
    /// </summary>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResultDto> GetOperationResultAsync(ObjectId operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bulk operations for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bulk operations</returns>
    Task<List<BulkOperationProgress>> GetUserOperationsAsync(ObjectId userId, CancellationToken cancellationToken = default);
}
