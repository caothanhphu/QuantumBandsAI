using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

public partial class WalletTransaction
{
    [Key]
    [Column("TransactionID")]
    public long TransactionId { get; set; }

    [Column("WalletID")]
    public int WalletId { get; set; }

    [Column("TransactionTypeID")]
    public int TransactionTypeId { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal BalanceBefore { get; set; }

    [Column(TypeName = "decimal(18, 8)")]
    public decimal BalanceAfter { get; set; }

    [Column("ReferenceID")]
    [StringLength(100)]
    public string? ReferenceId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    [Column("RelatedTransactionID")]
    public long? RelatedTransactionId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string CurrencyCode { get; set; } = "USD"; // Thêm vào, khớp với DEFAULT trong DB
    public DateTime UpdatedAt { get; set; }         // Thêm vào

    public string? WithdrawalMethodDetails { get; set; }
    public string? UserProvidedNotes { get; set; }

    [InverseProperty("RelatedTransaction")]
    public virtual ICollection<WalletTransaction> InverseRelatedTransaction { get; set; } = new List<WalletTransaction>();

    [ForeignKey("RelatedTransactionId")]
    [InverseProperty("InverseRelatedTransaction")]
    public virtual WalletTransaction? RelatedTransaction { get; set; }

    [ForeignKey("TransactionTypeId")]
    [InverseProperty("WalletTransactions")]
    public virtual TransactionType TransactionType { get; set; } = null!;

    [ForeignKey("WalletId")]
    [InverseProperty("WalletTransactions")]
    public virtual Wallet Wallet { get; set; } = null!;
}
