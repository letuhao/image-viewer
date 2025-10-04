using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for Library operations
/// </summary>
public interface ILibraryRepository : IRepository<Library>
{
    #region Query Methods
    
    Task<Library> GetByPathAsync(string path);
    Task<IEnumerable<Library>> GetByOwnerIdAsync(ObjectId ownerId);
    Task<IEnumerable<Library>> GetPublicLibrariesAsync();
    Task<IEnumerable<Library>> GetActiveLibrariesAsync();
    Task<long> GetLibraryCountAsync();
    Task<long> GetActiveLibraryCountAsync();
    
    #endregion
    
    #region Search Methods
    
    Task<IEnumerable<Library>> SearchLibrariesAsync(string query);
    Task<IEnumerable<Library>> GetLibrariesByFilterAsync(LibraryFilter filter);
    
    #endregion
    
    #region Statistics Methods
    
    Task<LibraryStatistics> GetLibraryStatisticsAsync();
    Task<IEnumerable<Library>> GetTopLibrariesByActivityAsync(int limit = 10);
    Task<IEnumerable<Library>> GetRecentLibrariesAsync(int limit = 10);
    
    #endregion
}

/// <summary>
/// Library filter for advanced queries
/// </summary>
public class LibraryFilter
{
    public ObjectId? OwnerId { get; set; }
    public bool? IsPublic { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastActivityAfter { get; set; }
    public DateTime? LastActivityBefore { get; set; }
    public string? Path { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }
}

/// <summary>
/// Library statistics for reporting
/// </summary>
public class LibraryStatistics
{
    public long TotalLibraries { get; set; }
    public long ActiveLibraries { get; set; }
    public long PublicLibraries { get; set; }
    public long NewLibrariesThisMonth { get; set; }
    public long NewLibrariesThisWeek { get; set; }
    public long NewLibrariesToday { get; set; }
    public Dictionary<ObjectId, long> LibrariesByOwner { get; set; } = new();
    public Dictionary<string, long> LibrariesByTag { get; set; } = new();
    public Dictionary<string, long> LibrariesByCategory { get; set; } = new();
}