using System.ComponentModel.DataAnnotations;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands.ManualSnapshotTrigger;

public class ManualSnapshotTriggerRequest
{
    [Required]
    [DataType(DataType.Date)]
    public DateTime TargetDate { get; set; }

    /// <summary>
    /// Trading Account IDs to process. If null or empty, all active accounts will be processed.
    /// </summary>
    public List<int>? TradingAccountIds { get; set; }

    /// <summary>
    /// Whether to force recalculate if snapshot already exists for the date
    /// </summary>
    public bool ForceRecalculate { get; set; } = false;

    /// <summary>
    /// Reason for manual trigger (required for audit purposes)
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes for admin reference
    /// </summary>
    [StringLength(1000)]
    public string? AdminNotes { get; set; }
} 