using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("BuyOrderId", Name = "IX_ShareTrades_BuyOrderID")]
[Index("BuyerUserId", Name = "IX_ShareTrades_BuyerUserID")]
[Index("SellOrderId", Name = "IX_ShareTrades_SellOrderID")]
[Index("SellerUserId", Name = "IX_ShareTrades_SellerUserID")]
[Index("TradingAccountId", "TradeDate", Name = "IX_ShareTrades_TradingAccountID_TradeDate", IsDescending = new[] { false, true })]
public partial class ShareTrade
{
    [Key]
    [Column("TradeID")]
    public long TradeId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    [Column("BuyOrderID")]
    public long BuyOrderId { get; set; }

    [Column("SellOrderID")]
    public long? SellOrderId { get; set; }

    [Column("InitialShareOfferingID")]
    public int? InitialShareOfferingId { get; set; }

    [Column("BuyerUserID")]
    public int BuyerUserId { get; set; }

    [Column("SellerUserID")]
    public int SellerUserId { get; set; }

    public long QuantityTraded { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal TradePrice { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal BuyerFeeAmount { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal SellerFeeAmount { get; set; }

    public DateTime TradeDate { get; set; }

    [ForeignKey("BuyOrderId")]
    [InverseProperty("ShareTradeBuyOrders")]
    public virtual ShareOrder BuyOrder { get; set; } = null!;

    [ForeignKey("BuyerUserId")]
    [InverseProperty("ShareTradeBuyerUsers")]
    public virtual User BuyerUser { get; set; } = null!;

    [ForeignKey("InitialShareOfferingId")]
    [InverseProperty("ShareTrades")]
    public virtual InitialShareOffering? InitialShareOffering { get; set; }

    [ForeignKey("SellOrderId")]
    [InverseProperty("ShareTradeSellOrders")]
    public virtual ShareOrder? SellOrder { get; set; }

    [ForeignKey("SellerUserId")]
    [InverseProperty("ShareTradeSellerUsers")]
    public virtual User SellerUser { get; set; } = null!;

    [ForeignKey("TradingAccountId")]
    [InverseProperty("ShareTrades")]
    public virtual TradingAccount TradingAccount { get; set; } = null!;
}
