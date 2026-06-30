namespace DotNetAdmin.Core.Helpers;

public static class PaginationHelper
{
    public static async Task<PaginationResult<T>> PaginateAsync<T>(
        IQueryable<T> query,
        int page = 1,
        int pageSize = 10) where T : class
    {
        page = Math.Max(1, page);
        pageSize = pageSize is > 0 and <= 100 ? pageSize : 10;

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginationResult<T>
        {
            Data = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public static List<int?> GetWindowedPages(int currentPage, int totalPages, int window = 2)
    {
        var pages = new List<int?>();
        if (totalPages <= 1) { pages.Add(1); return pages; }

        pages.Add(1);
        if (currentPage - window > 2) pages.Add(null);

        for (var i = Math.Max(2, currentPage - window); i <= Math.Min(totalPages - 1, currentPage + window); i++)
            pages.Add(i);

        if (currentPage + window < totalPages - 1) pages.Add(null);
        if (totalPages > 1 && !pages.Contains(totalPages)) pages.Add(totalPages);

        return pages;
    }
}

public class PaginationResult<T>
{
    public List<T> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int To => Math.Min(Page * PageSize, TotalCount);
}
