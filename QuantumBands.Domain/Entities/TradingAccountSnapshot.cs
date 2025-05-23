using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("TradingAccountId", "SnapshotDate", Name = "UQ_TradingAccountSnapshots_Account_Date", IsUnique = true)]
public partial class TradingAccountSnapshot
{
    [Key]
    [Column("SnapshotID")]
    public long SnapshotId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    public DateOnly SnapshotDate { get; set; }

    [Column("OpeningNAV", TypeName = "decimal(18, 2)")]
    public decimal OpeningNAV { get; set; }

    [Column("RealizedPAndLForTheDay", TypeName = "decimal(18, 2)")]
    public decimal RealizedPandLforTheDay { get; set; }

    [Column("UnrealizedPAndLForTheDay", TypeName = "decimal(18, 2)")]
    public decimal UnrealizedPandLforTheDay { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ManagementFeeDeducted { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProfitDistributed { get; set; }

    [Column("ClosingNAV", TypeName = "decimal(18, 2)")]
    public decimal ClosingNav { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal ClosingSharePrice { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("TradingAccountId")]
    [InverseProperty("TradingAccountSnapshots")]
    public virtual TradingAccount TradingAccount { get; set; } = null!;
}
