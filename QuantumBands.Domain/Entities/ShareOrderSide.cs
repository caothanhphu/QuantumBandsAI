using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("SideName", Name = "UQ__ShareOrd__8D8D27303DD8CB84", IsUnique = true)]
public partial class ShareOrderSide
{
    [Key]
    [Column("OrderSideID")]
    public int OrderSideId { get; set; }

    [StringLength(10)]
    public string SideName { get; set; } = null!;

    [InverseProperty("ShareOrderSide")]
    public virtual ICollection<ShareOrder> ShareOrders { get; set; } = new List<ShareOrder>();
}
