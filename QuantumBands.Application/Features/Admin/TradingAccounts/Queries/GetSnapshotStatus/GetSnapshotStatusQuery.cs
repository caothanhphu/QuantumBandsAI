using System.ComponentModel.DataAnnotations;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Queries.GetSnapshotStatus;

public class GetSnapshotStatusQuery
{
    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    /// <summary>
    /// Optional filter by specific trading account IDs
    /// </summary>
    public List<int>? TradingAccountIds { get; set; }
} 