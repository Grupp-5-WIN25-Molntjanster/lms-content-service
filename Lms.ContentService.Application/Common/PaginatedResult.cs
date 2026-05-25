namespace Lms.ContentService.Application.Common;

/// <summary>
/// Standard paginated response returned to clients.
/// Includes metadata for UI pagination controls.
/// 
/// WHY:
/// - Consistent response shape for all paginated endpoints
/// - Frontend can render pagination controls without guesswork
/// - Follows industry standard (Microsoft, GitHub, Stripe all do this)
/// </summary>
public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }

    public PaginatedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
    }
}