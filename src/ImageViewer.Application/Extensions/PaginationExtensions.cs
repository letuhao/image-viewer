using ImageViewer.Application.DTOs.Common;

namespace ImageViewer.Application.Extensions;

/// <summary>
/// Pagination extensions
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Apply pagination to IQueryable
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, PaginationRequestDto pagination)
    {
        return query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize);
    }

    /// <summary>
    /// Apply sorting to IQueryable
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortBy, string? sortDirection)
    {
        if (string.IsNullOrEmpty(sortBy))
            return query;

        var direction = sortDirection?.ToLower() == "desc" ? "desc" : "asc";
        
        // This is a simplified implementation
        // In a real application, you would use reflection or expression trees
        // to dynamically build the sorting expression
        return query;
    }

    /// <summary>
    /// Create pagination response
    /// </summary>
    public static PaginationResponseDto<T> ToPaginationResponse<T>(
        this IEnumerable<T> data,
        int totalCount,
        PaginationRequestDto pagination)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize);
        
        return new PaginationResponseDto<T>
        {
            Data = data,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalPages = totalPages,
            HasNextPage = pagination.Page < totalPages,
            HasPreviousPage = pagination.Page > 1
        };
    }
}