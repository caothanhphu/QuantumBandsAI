namespace QuantumBands.Application.Features.Admin.TradingAccounts.Queries.GetSnapshotStatus;

public class SnapshotStatusResponse
{
    public DateTime Date { get; set; }
    public int TotalAccounts { get; set; }
    public int CompletedCount { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalProfitDistributed { get; set; }
    public int TotalShareholdersAffected { get; set; }
    public List<AccountSnapshotStatus> Accounts { get; set; } = new();
}

public class AccountSnapshotStatus
{
    public int TradingAccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public bool SnapshotExists { get; set; }
    public long? SnapshotId { get; set; }
    public decimal? ProfitDistributed { get; set; }
    public int? ShareholdersCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty; // "Completed", "Pending", "Failed"
    public string? Reason { get; set; }
    public decimal? OpeningNAV { get; set; }
    public decimal? ClosingNAV { get; set; }
    public decimal? RealizedPAndL { get; set; }
    public decimal? ManagementFee { get; set; }
} 