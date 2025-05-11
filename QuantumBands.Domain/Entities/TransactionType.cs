using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("TypeName", Name = "UQ__Transact__D4E7DFA81202EEAE", IsUnique = true)]
public partial class TransactionType
{
    [Key]
    [Column("TransactionTypeID")]
    public int TransactionTypeId { get; set; }

    [StringLength(100)]
    public string TypeName { get; set; } = null!;

    public bool IsCredit { get; set; }

    [InverseProperty("TransactionType")]
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
