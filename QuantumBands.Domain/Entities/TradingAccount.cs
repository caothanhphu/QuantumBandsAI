using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("AccountName", Name = "UQ__TradingA__406E0D2EB419829E", IsUnique = true)]
public partial class TradingAccount
{
    [Key]
    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    [StringLength(100)]
    public string AccountName { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Column("EAName")]
    [StringLength(100)]
    public string? Eaname { get; set; }

    [StringLength(100)]
    public string? BrokerPlatformIdentifier { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal InitialCapital { get; set; }

    public long TotalSharesIssued { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CurrentNetAssetValue { get; set; }

    [Column(TypeName = "decimal(38, 22)")]
    public decimal? CurrentSharePrice { get; set; }

    [Column(TypeName = "decimal(5, 4)")]
    public decimal ManagementFeeRate { get; set; }

    public bool IsActive { get; set; }

    [Column("CreatedByUserID")]
    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("TradingAccounts")]
    public virtual User CreatedByUser { get; set; } = null!;

    [InverseProperty("TradingAccount")]
    public virtual ICollection<EaclosedTrade> EaclosedTrades { get; set; } = new List<EaclosedTrade>();

    [InverseProperty("TradingAccount")]
    public virtual ICollection<EaopenPosition> EaopenPositions { get; set; } = new List<EaopenPosition>();

    [InverseProperty("TradingAccount")]
    public virtual ICollection<InitialShareOffering> InitialShareOfferings { get; set; } = new List<InitialShareOffering>();

    [InverseProperty("TradingAccount")]
    public virtual ICollection<SharePortfolio> SharePortfolios { get; set; } = new List<SharePortfolio>();

    [InverseProperty("TradingAccount")]
    public virtual ICollection<TradingAccountSnapshot> TradingAccountSnapshots { get; set; } = new List<TradingAccountSnapshot>();
}
