namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands.ManualSnapshotTrigger;

public class ManualSnapshotTriggerResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AccountsProcessed { get; set; }
    public int AccountsSkipped { get; set; }
    public int AccountsFailed { get; set; }
    public decimal TotalProfitDistributed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ManualSnapshotAccountResult> AccountResults { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class ManualSnapshotAccountResult
{
    public int TradingAccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Success", "Skipped", "Failed"
    public string? Message { get; set; }
    public decimal? ProfitDistributed { get; set; }
    public int? ShareholdersCount { get; set; }
    public long? SnapshotId { get; set; }
} 