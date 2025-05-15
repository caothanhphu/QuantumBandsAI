using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("Username", Name = "UQ__Users__536C85E46C1E47F9", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D10534121601FF", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(100)]
    public string Username { get; set; } = null!;

    [StringLength(256)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [StringLength(200)]
    public string? FullName { get; set; }

    [Column("RoleID")]
    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public bool IsEmailVerified { get; set; }

    public bool TwoFactorEnabled { get; set; }
    [StringLength(256)]
    public string? TwoFactorSecretKey { get; set; } = null!;

    public DateTime? LastLoginDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    [StringLength(256)]
    public string? EmailVerificationToken { get; set; } = null!;
    public DateTime? PasswordResetTokenExpiry { get; set; }
    [StringLength(256)]
    public string? PasswordResetToken { get; set; } = null!;
    public DateTime? RefreshTokenExpiry { get; set; }
    [StringLength(256)]
    public string? RefreshToken { get; set; } = null!;

    [InverseProperty("AdminUser")]
    public virtual ICollection<InitialShareOffering> InitialShareOfferings { get; set; } = new List<InitialShareOffering>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual UserRole Role { get; set; } = null!;

    [InverseProperty("User")]
    public virtual ICollection<SharePortfolio> SharePortfolios { get; set; } = new List<SharePortfolio>();

    [InverseProperty("UpdatedByUser")]
    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<TradingAccount> TradingAccounts { get; set; } = new List<TradingAccount>();

    [InverseProperty("User")]
    public virtual Wallet? Wallet { get; set; }
}
