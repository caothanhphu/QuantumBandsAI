using System.ComponentModel.DataAnnotations;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands.RecalculateProfitDistribution;

public class RecalculateProfitDistributionRequest
{
    /// <summary>
    /// Reason for recalculation (required for audit purposes)
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Whether to reverse existing profit distribution before recalculating
    /// </summary>
    public bool ReverseExisting { get; set; } = true;

    /// <summary>
    /// Additional admin notes
    /// </summary>
    [StringLength(1000)]
    public string? AdminNotes { get; set; }
}

public class RecalculateProfitDistributionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ProfitDistributionSummary? OldDistribution { get; set; }
    public ProfitDistributionSummary? NewDistribution { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public DateTime RecalculatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Errors { get; set; } = new();
}

public class ProfitDistributionSummary
{
    public decimal TotalDistributed { get; set; }
    public int ShareholdersCount { get; set; }
    public decimal ManagementFee { get; set; }
    public decimal RealizedPAndL { get; set; }
    public decimal ProfitPerShare { get; set; }
} 