using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;
public enum OrderStatus
{
    Open,
    PartiallyFilled,
    Filled,
    Cancelled,
    Expired,
    PendingExecution // Thêm trạng thái này nếu cần cho lệnh Market
}

[Index("StatusName", Name = "UQ__ShareOrd__05E7698A1417F64A", IsUnique = true)]
public partial class ShareOrderStatus
{
    [Key]
    [Column("OrderStatusID")]
    public int OrderStatusId { get; set; }

    [StringLength(20)]
    public string StatusName { get; set; } = null!;
}
