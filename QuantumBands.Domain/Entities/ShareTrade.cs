// QuantumBands.Domain/Entities/ShareTrade.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuantumBands.Domain.Entities;

public class ShareTrade
{
    [Key]
    public long TradeId { get; set; }

    public int TradingAccountId { get; set; }
    public virtual TradingAccount TradingAccount { get; set; } = null!;

    public long BuyOrderId { get; set; } // ID của lệnh mua
    public virtual ShareOrder BuyOrder { get; set; } = null!;

    public long SellOrderId { get; set; } // ID của lệnh bán (hoặc null nếu khớp với InitialShareOffering)
    public virtual ShareOrder? SellOrder { get; set; } // Nullable nếu bán từ Admin/Offering

    public int? InitialShareOfferingId { get; set; } // Nullable, ID của đợt chào bán nếu khớp với nó
    public virtual InitialShareOffering? InitialShareOffering { get; set; }

    public int BuyerUserId { get; set; }
    public virtual User BuyerUser { get; set; } = null!;

    public int SellerUserId { get; set; } // Có thể là ID của Admin nếu bán từ InitialShareOffering
    public virtual User SellerUser { get; set; } = null!;

    public long QuantityTraded { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal TradePrice { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal BuyerFeeAmount { get; set; } = 0; // Phí người mua

    [Column(TypeName = "decimal(18, 8)")]
    public decimal SellerFeeAmount { get; set; } = 0; // Phí người bán

    public DateTime TradeDate { get; set; } = DateTime.UtcNow;
}