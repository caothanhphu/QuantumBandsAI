using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("UserId", "TradingAccountId", Name = "UQ_SharePortfolios_User_TradingAccount", IsUnique = true)]
public partial class SharePortfolio
{
    [Key]
    [Column("PortfolioID")]
    public int PortfolioId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    public long Quantity { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal AverageBuyPrice { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    [ForeignKey("TradingAccountId")]
    [InverseProperty("SharePortfolios")]
    public virtual TradingAccount TradingAccount { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("SharePortfolios")]
    public virtual User User { get; set; } = null!;
}
