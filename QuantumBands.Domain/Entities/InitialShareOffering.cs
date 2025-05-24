using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

public partial class InitialShareOffering
{
    [Key]
    [Column("OfferingID")]
    public int OfferingId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    [Column("AdminUserID")]
    public int AdminUserId { get; set; }

    public long SharesOffered { get; set; }

    public long SharesSold { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal OfferingPricePerShare { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal? FloorPricePerShare { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal? CeilingPricePerShare { get; set; }

    public DateTime OfferingStartDate { get; set; }

    public DateTime? OfferingEndDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AdminUserId")]
    [InverseProperty("InitialShareOfferings")]
    public virtual User AdminUser { get; set; } = null!;

    [InverseProperty("InitialShareOffering")]
    public virtual ICollection<ShareTrade> ShareTrades { get; set; } = new List<ShareTrade>();

    [ForeignKey("TradingAccountId")]
    [InverseProperty("InitialShareOfferings")]
    public virtual TradingAccount TradingAccount { get; set; } = null!;
}
