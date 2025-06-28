namespace QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;

public class ProfitDistributionLogDto
{
    public long DistributionLogId { get; set; }
    public long TradingAccountSnapshotId { get; set; }
    public int TradingAccountId { get; set; }
    public string TradingAccountName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public DateTime DistributionDate { get; set; }
    public long SharesHeldAtDistribution { get; set; }
    public decimal ProfitPerShareDistributed { get; set; }
    public decimal TotalAmountDistributed { get; set; }
    public long? WalletTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class ProfitDistributionHistorySummary
{
    public DateTime Date { get; set; }
    public int TradingAccountId { get; set; }
    public string TradingAccountName { get; set; } = string.Empty;
    public decimal TotalDistributed { get; set; }
    public int ShareholdersCount { get; set; }
    public decimal ManagementFee { get; set; }
    public string Status { get; set; } = string.Empty;
    public long SnapshotId { get; set; }
} 