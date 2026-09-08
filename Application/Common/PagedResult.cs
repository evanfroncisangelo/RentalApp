namespace RentalApp.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }

    public static PagedResult<T> Create(IReadOnlyList<T> source, int pageNumber, int pageSize)
    {
        var validPageSize = pageSize <= 0 ? 10 : pageSize;
        var totalCount = source.Count;
        var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)validPageSize), 1);
        var validPageNumber = Math.Min(Math.Max(pageNumber, 1), totalPages);

        var items = source
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = validPageNumber,
            PageSize = validPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
