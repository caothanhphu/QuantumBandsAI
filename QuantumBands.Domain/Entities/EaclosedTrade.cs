using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuantumBands.Domain.Entities;

[Table("EAClosedTrades")]
[Index("TradingAccountId", "EaticketId", Name = "UQ_EAClosedTrades_Account_Ticket", IsUnique = true)]
public partial class EaclosedTrade
{
    [Key]
    [Column("ClosedTradeID")]
    public long ClosedTradeId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    [Column("EATicketID")]
    [StringLength(50)]
    [Unicode(false)]
    public string EaticketId { get; set; } = null!;

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
    public decimal ClosePrice { get; set; }

    public DateTime CloseTime { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Swap { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Commission { get; set; }

    [Column("RealizedPAndL", TypeName = "decimal(18, 2)")]
    public decimal RealizedPandL { get; set; }

    [Column("IsProcessedForDailyPAndL")]
    public bool IsProcessedForDailyPandL { get; set; }

    public DateTime RecordedAt { get; set; }

    [ForeignKey("TradingAccountId")]
    [InverseProperty("EaclosedTrades")]
    public virtual TradingAccount TradingAccount { get; set; } = null!;
}
