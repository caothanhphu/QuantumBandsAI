using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("TradingAccountSnapshotId", Name = "IX_ProfitDistributionLogs_SnapshotID")]
[Index("UserId", "DistributionDate", Name = "IX_ProfitDistributionLogs_UserID_Date", IsDescending = new[] { false, true })]
public partial class ProfitDistributionLog
{
    [Key]
    [Column("DistributionLogID")]
    public long DistributionLogId { get; set; }

    [Column("TradingAccountSnapshotID")]
    public long TradingAccountSnapshotId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    public DateOnly DistributionDate { get; set; }

    public long SharesHeldAtDistribution { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal ProfitPerShareDistributed { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmountDistributed { get; set; }

    [Column("WalletTransactionID")]
    public long? WalletTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("TradingAccountId")]
    [InverseProperty("ProfitDistributionLogs")]
    public virtual TradingAccount TradingAccount { get; set; } = null!;

    [ForeignKey("TradingAccountSnapshotId")]
    [InverseProperty("ProfitDistributionLogs")]
    public virtual TradingAccountSnapshot TradingAccountSnapshot { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("ProfitDistributionLogs")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("WalletTransactionId")]
    [InverseProperty("ProfitDistributionLogs")]
    public virtual WalletTransaction? WalletTransaction { get; set; }
}
