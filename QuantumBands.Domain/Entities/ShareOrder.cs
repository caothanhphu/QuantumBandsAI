// QuantumBands.Domain/Entities/ShareOrder.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;
// using Microsoft.EntityFrameworkCore; // Không cần thiết trong entity class

namespace QuantumBands.Domain.Entities;

// Các [Index] attribute có thể giữ nguyên hoặc để EF Core scaffold tự tạo trong DbContext
[Index("OrderDate", Name = "IX_ShareOrders_OrderDate", IsDescending = new[] { true })] // Sửa AllDescending
[Index("TradingAccountId", "OrderStatusId", Name = "IX_ShareOrders_TradingAccountID_StatusID")]
[Index("UserId", Name = "IX_ShareOrders_UserID")]
public partial class ShareOrder
{
    [Key]
    [Column("OrderID")]
    public long OrderId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }
    [ForeignKey("UserId")] // Data annotation cho FK
    [InverseProperty("ShareOrders")] // Trỏ đến ICollection<ShareOrder> trong User entity
    public virtual User User { get; set; } = null!;

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }
    [ForeignKey("TradingAccountId")]
    [InverseProperty("ShareOrders")] // Trỏ đến ICollection<ShareOrder> trong TradingAccount entity
    public virtual TradingAccount TradingAccount { get; set; } = null!;

    [Column("OrderSideID")]
    public int OrderSideId { get; set; }
    [ForeignKey("OrderSideId")]
    [InverseProperty("ShareOrders")] // Trỏ đến ICollection<ShareOrder> trong ShareOrderSide entity
    public virtual ShareOrderSide ShareOrderSide { get; set; } = null!;

    [Column("OrderTypeID")]
    public int OrderTypeId { get; set; }
    [ForeignKey("OrderTypeId")]
    [InverseProperty("ShareOrders")] // Trỏ đến ICollection<ShareOrder> trong ShareOrderType entity
    public virtual ShareOrderType ShareOrderType { get; set; } = null!;

    [Column("OrderStatusID")]
    public int OrderStatusId { get; set; }
    [ForeignKey("OrderStatusId")]
    [InverseProperty("ShareOrders")] // Trỏ đến ICollection<ShareOrder> trong ShareOrderStatus entity
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

    [InverseProperty("BuyOrder")] // Trỏ đến BuyOrder trong ShareTrade
    public virtual ICollection<ShareTrade> BuyTrades { get; set; } = new List<ShareTrade>();

    [InverseProperty("SellOrder")] // Trỏ đến SellOrder trong ShareTrade
    public virtual ICollection<ShareTrade> SellTrades { get; set; } = new List<ShareTrade>();

    public ShareOrder()
    {
        OrderDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        QuantityFilled = 0;
    }
}