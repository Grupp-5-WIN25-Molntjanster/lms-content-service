namespace Lms.ContentService.Application.Common;

/// <summary>
/// Standard pagination request parameters.
/// Used by all controllers that return paginated results.
/// 
/// WHY:
/// - Consistent pagination across all endpoints
/// - Validation built-in (min/max page size)
/// - Prevents abuse (max 100 items per page)
/// </summary>
public class PaginationRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>Page number (1-based). Default: 1</summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>Items per page. Min: 1, Max: 100. Default: 10</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => value
        };
    }
}