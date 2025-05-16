// QuantumBands.Application/Common/Models/PaginatedList.cs
using Microsoft.EntityFrameworkCore; // For ToListAsync

namespace QuantumBands.Application.Common.Models;

public class PaginatedList<T>
{
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public long TotalCount { get; } // Sử dụng long cho total count
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public List<T> Items { get; }

    public PaginatedList(List<T> items, long count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
    }

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var count = await source.LongCountAsync(cancellationToken); // Đếm tổng số mục
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}