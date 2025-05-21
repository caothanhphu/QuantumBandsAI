// QuantumBands.Application/Features/Admin/Users/Queries/GetAdminUsersQuery.cs
namespace QuantumBands.Application.Features.Admin.Users.Queries;

public class GetAdminUsersQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";
    public string? SearchTerm { get; set; }
    public int? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsEmailVerified { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    private const int MaxPageSize = 100;
    public int ValidatedPageSize
    {
        get => (PageSize > MaxPageSize || PageSize <= 0) ? MaxPageSize : PageSize;
        set => PageSize = value;
    }
    public int ValidatedPageNumber
    {
        get => PageNumber <= 0 ? 1 : PageNumber;
        set => PageNumber = value;
    }
}