using ImageViewer.Domain.DTOs;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Service for cleaning up __MACOSX metadata files from collections
/// </summary>
public interface IMacOSXCleanupService
{
    /// <summary>
    /// Cleans up all __MACOSX metadata files from all collections
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup result with statistics</returns>
    Task<MacOSXCleanupResult> CleanupMacOSXFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews what would be cleaned up without making changes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview of what would be cleaned up</returns>
    Task<MacOSXCleanupPreview> PreviewMacOSXCleanupAsync(CancellationToken cancellationToken = default);
}
