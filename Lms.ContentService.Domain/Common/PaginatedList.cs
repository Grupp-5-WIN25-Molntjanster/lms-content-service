namespace Lms.ContentService.Domain.Common;

/// <summary>
/// Generic paginated list for domain layer.
/// Used by repository to return paginated results.
/// 
/// WHY IN DOMAIN:
/// - Repository interfaces are in Domain
/// - Pagination is a domain concern (how we retrieve data)
/// - Can be used across all services (reusable pattern)
/// </summary>
public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public PaginatedList<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        var mappedItems = Items.Select(selector).ToList();
        return new PaginatedList<TResult>(mappedItems, TotalCount, PageNumber, PageSize);
    }
}