using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("UserId", Name = "UQ__Wallets__1788CCAD0AF96292", IsUnique = true)]
public partial class Wallet
{
    [Key]
    [Column("WalletID")]
    public int WalletId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal Balance { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string CurrencyCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Wallet")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Wallet")]
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
