// QuantumBands.Domain/Entities/ShareTrade.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuantumBands.Domain.Entities;

public partial class ShareTrade // Đảm bảo tên class và namespace khớp với các file bạn cung cấp
{
    [Key]
    [Column("TradeID")]
    public long TradeId { get; set; }

    [Column("TradingAccountID")]
    public int TradingAccountId { get; set; }
    [ForeignKey("TradingAccountId")]
    [InverseProperty("ShareTrades")] // Trỏ đến ICollection<ShareTrade> trong TradingAccount
    public virtual TradingAccount TradingAccount { get; set; } = null!;

    [Column("BuyOrderID")]
    public long BuyOrderId { get; set; }
    [ForeignKey("BuyOrderId")]
    [InverseProperty("BuyTrades")] // Đổi tên collection trong ShareOrder thành BuyTrades
    public virtual ShareOrder BuyOrder { get; set; } = null!;

    [Column("SellOrderID")]
    public long? SellOrderId { get; set; } // Nullable
    [ForeignKey("SellOrderId")]
    [InverseProperty("SellTrades")] // Đổi tên collection trong ShareOrder thành SellTrades
    public virtual ShareOrder? SellOrder { get; set; }

    [Column("InitialShareOfferingID")]
    public int? InitialShareOfferingId { get; set; } // Nullable
    [ForeignKey("InitialShareOfferingId")]
    [InverseProperty("ShareTrades")] // Trỏ đến ICollection<ShareTrade> trong InitialShareOffering
    public virtual InitialShareOffering? InitialShareOffering { get; set; }

    [Column("BuyerUserID")]
    public int BuyerUserId { get; set; }
    [ForeignKey("BuyerUserId")]
    [InverseProperty("ShareTradeBuyerUsers")] // Trỏ đến ICollection<ShareTrade> trong User
    public virtual User BuyerUser { get; set; } = null!;

    [Column("SellerUserID")]
    public int SellerUserId { get; set; }
    [ForeignKey("SellerUserId")]
    [InverseProperty("ShareTradeSellerUsers")] // Trỏ đến ICollection<ShareTrade> trong User
    public virtual User SellerUser { get; set; } = null!;

    public long QuantityTraded { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal TradePrice { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal BuyerFeeAmount { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal SellerFeeAmount { get; set; }

    public DateTime TradeDate { get; set; }

    public ShareTrade()
    {
        TradeDate = DateTime.UtcNow;
        BuyerFeeAmount = 0;
        SellerFeeAmount = 0;
    }
}