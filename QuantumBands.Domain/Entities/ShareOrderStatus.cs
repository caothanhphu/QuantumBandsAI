using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("StatusName", Name = "UQ__ShareOrd__05E7698A1417F64A", IsUnique = true)]
public partial class ShareOrderStatus
{
    [Key]
    [Column("OrderStatusID")]
    public int OrderStatusId { get; set; }

    [StringLength(20)]
    public string StatusName { get; set; } = null!;

    [InverseProperty("ShareOrderStatus")]
    public virtual ICollection<ShareOrder> ShareOrders { get; set; } = new List<ShareOrder>();
}
