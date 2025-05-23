using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Table("EAOpenPositions")]
[Index("TradingAccountId", "EaticketId", Name = "UQ_EAOpenPositions_Account_Ticket", IsUnique = true)]
public partial class EAOpenPosition
{
    [Key]
    [Column("OpenPositionID")]
    public long OpenPositionId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    [Column("EATicketID")]
    [StringLength(50)]
    [Unicode(false)]
    public string EaTicketId { get; set; } = null!;

    [StringLength(20)]
    public string Symbol { get; set; } = null!;

    [StringLength(10)]
    public string TradeType { get; set; } = null!;

    [Column(TypeName = "decimal(10, 2)")]
    public decimal VolumeLots { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal OpenPrice { get; set; }

    public DateTime OpenTime { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal CurrentMarketPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Swap { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Commission { get; set; }

    [Column("FloatingPAndL", TypeName = "decimal(18, 2)")]
    public decimal FloatingPAndL { get; set; }

    public DateTime LastUpdateTime { get; set; }

    [ForeignKey("TradingAccountId")]
    [InverseProperty("EaopenPositions")]
    public virtual TradingAccount TradingAccount { get; set; } = null!;
}
