using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("TypeName", Name = "UQ__ShareOrd__D4E7DFA8F376B605", IsUnique = true)]
public partial class ShareOrderType
{
    [Key]
    [Column("OrderTypeID")]
    public int OrderTypeId { get; set; }

    [StringLength(20)]
    public string TypeName { get; set; } = null!;

    [InverseProperty("OrderType")]
    public virtual ICollection<ShareOrder> ShareOrders { get; set; } = new List<ShareOrder>();
}
