using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("OrderDate", Name = "IX_ShareOrders_OrderDate", AllDescending = true)]
[Index("TradingAccountId", "OrderStatusId", Name = "IX_ShareOrders_TradingAccountID_StatusID")]
[Index("UserId", Name = "IX_ShareOrders_UserID")]
public partial class ShareOrder
{
    [Key]
    [Column("OrderID")]
    public long OrderId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }

    [Column("OrderSideID")]
    public int OrderSideId { get; set; }
    public virtual ShareOrderSide ShareOrderSide { get; set; } = null!; // Thuộc tính Navigation

    [Column("OrderTypeID")]
    public int OrderTypeId { get; set; }
    public virtual ShareOrderType ShareOrderType { get; set; } = null!;

    [Column("OrderStatusID")]
    public int OrderStatusId { get; set; }
    public virtual ShareOrderStatus ShareOrderStatus { get; set; } = null!;

    public long QuantityOrdered { get; set; }

    public long QuantityFilled { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal? LimitPrice { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal? AverageFillPrice { get; set; }

    [Column(TypeName = "decimal(6, 5)")]
    public decimal? TransactionFeeRate { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal? TransactionFeeAmount { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("OrderSideId")]
    [InverseProperty("ShareOrders")]
    public virtual ShareOrderSide OrderSide { get; set; } = null!;

    [ForeignKey("OrderStatusId")]
    [InverseProperty("ShareOrders")]
    public virtual ShareOrderStatus OrderStatus { get; set; } = null!;

    [ForeignKey("OrderTypeId")]
    [InverseProperty("ShareOrders")]
    public virtual ShareOrderType OrderType { get; set; } = null!;

    [InverseProperty("BuyOrder")]
    public virtual ICollection<ShareTrade> ShareTradeBuyOrders { get; set; } = new List<ShareTrade>();

    [InverseProperty("SellOrder")]
    public virtual ICollection<ShareTrade> ShareTradeSellOrders { get; set; } = new List<ShareTrade>();

    [ForeignKey("TradingAccountId")]
    [InverseProperty("ShareOrders")]
    public virtual TradingAccount TradingAccount { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("ShareOrders")]
    public virtual User User { get; set; } = null!;
    // Navigation properties cho ShareTrades (nếu cần cho lỗi này)
    public virtual ICollection<ShareTrade> BuyTrades { get; set; } = new List<ShareTrade>();
    public virtual ICollection<ShareTrade> SellTrades { get; set; } = new List<ShareTrade>();


    public ShareOrder()
    {
        OrderDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        QuantityFilled = 0;
    }
}
