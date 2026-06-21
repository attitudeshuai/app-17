namespace MedCabinet.Application.DTOs.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

public class PagedQueryParams
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
