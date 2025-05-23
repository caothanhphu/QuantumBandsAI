// QuantumBands.Domain/Entities/ShareOrder.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuantumBands.Domain.Entities;

public class ShareOrder
{
    [Key] // Đánh dấu là khóa chính
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Giá trị được tự động sinh bởi database
    public long OrderId { get; set; } // Tên thuộc tính PascalCase

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!; // Navigation property tới User

    public int TradingAccountId { get; set; }
    public virtual TradingAccount TradingAccount { get; set; } = null!; // Navigation property tới TradingAccount

    public int OrderSideId { get; set; }
    public virtual ShareOrderSide ShareOrderSide { get; set; } = null!; // Navigation property

    public int OrderTypeId { get; set; }
    public virtual ShareOrderType ShareOrderType { get; set; } = null!; // Navigation property

    public int OrderStatusId { get; set; }
    public virtual ShareOrderStatus ShareOrderStatus { get; set; } = null!; // Navigation property

    public long QuantityOrdered { get; set; }
    public long QuantityFilled { get; set; } // DEFAULT 0 được xử lý bởi DB hoặc khi khởi tạo đối tượng

    [Column(TypeName = "decimal(18, 8)")] // Chỉ định kiểu dữ liệu SQL
    public decimal? LimitPrice { get; set; } // Nullable

    [Column(TypeName = "decimal(18, 8)")]
    public decimal? AverageFillPrice { get; set; } // Nullable

    [Column(TypeName = "decimal(6, 5)")]
    public decimal? TransactionFeeRate { get; set; } // Nullable

    [Column(TypeName = "decimal(18, 8)")]
    public decimal? TransactionFeeAmount { get; set; } // Nullable

    public DateTime OrderDate { get; set; } // DEFAULT GETUTCDATE() được xử lý bởi DB hoặc khi khởi tạo đối tượng
    public DateTime? ExpirationDate { get; set; } // Nullable
    public DateTime UpdatedAt { get; set; } // DEFAULT GETUTCDATE() được xử lý bởi DB hoặc khi khởi tạo đối tượng

    // Constructor để khởi tạo giá trị mặc định nếu cần (ví dụ: cho các trường không tự động bởi DB)
    public ShareOrder()
    {
        OrderDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        QuantityFilled = 0; // Mặc định trong C# nếu DB không set
    }
}