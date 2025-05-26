using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QuantumBands.Domain.Entities;

namespace QuantumBands.Infrastructure.Persistence.DataContext;

public partial class FinixAIDbContext : DbContext
{
    public FinixAIDbContext()
    {
    }

    public FinixAIDbContext(DbContextOptions<FinixAIDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<EaclosedTrade> EaclosedTrades { get; set; }

    public virtual DbSet<EaopenPosition> EaopenPositions { get; set; }

    public virtual DbSet<InitialShareOffering> InitialShareOfferings { get; set; }

    public virtual DbSet<ProfitDistributionLog> ProfitDistributionLogs { get; set; }

    public virtual DbSet<ShareOrder> ShareOrders { get; set; }

    public virtual DbSet<ShareOrderSide> ShareOrderSides { get; set; }

    public virtual DbSet<ShareOrderStatus> ShareOrderStatuses { get; set; }

    public virtual DbSet<ShareOrderType> ShareOrderTypes { get; set; }

    public virtual DbSet<SharePortfolio> SharePortfolios { get; set; }

    public virtual DbSet<ShareTrade> ShareTrades { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<TradingAccount> TradingAccounts { get; set; }

    public virtual DbSet<TradingAccountSnapshot> TradingAccountSnapshots { get; set; }

    public virtual DbSet<TransactionType> TransactionTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EaclosedTrade>(entity =>
        {
            entity.HasKey(e => e.ClosedTradeId).HasName("PK__EAClosed__9C45B584F04B6DEB");

            entity.Property(e => e.Commission).HasDefaultValue(0.00m);
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Swap).HasDefaultValue(0.00m);

            entity.HasOne(d => d.TradingAccount).WithMany(p => p.EaclosedTrades)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EAClosedTrades_TradingAccountID");
        });

        modelBuilder.Entity<EaopenPosition>(entity =>
        {
            entity.HasKey(e => e.OpenPositionId).HasName("PK__EAOpenPo__A2749288012FED3A");

            entity.Property(e => e.Commission).HasDefaultValue(0.00m);
            entity.Property(e => e.LastUpdateTime).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Swap).HasDefaultValue(0.00m);

            entity.HasOne(d => d.TradingAccount).WithMany(p => p.EaopenPositions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EAOpenPositions_TradingAccountID");
        });

        modelBuilder.Entity<InitialShareOffering>(entity =>
        {
            entity.HasKey(e => e.OfferingId).HasName("PK__InitialS__3500D7CD2B65639F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.OfferingStartDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.AdminUser).WithMany(p => p.InitialShareOfferings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InitialShareOfferings_AdminUserID");

            entity.HasOne(d => d.TradingAccount).WithMany(p => p.InitialShareOfferings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InitialShareOfferings_TradingAccountID");
        });

        modelBuilder.Entity<ProfitDistributionLog>(entity =>
        {
            entity.HasKey(e => e.DistributionLogId).HasName("PK__ProfitDi__64005710E76B9257");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.TradingAccount).WithMany(p => p.ProfitDistributionLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProfitDistributionLogs_TradingAccountID");

            entity.HasOne(d => d.TradingAccountSnapshot).WithMany(p => p.ProfitDistributionLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProfitDistributionLogs_SnapshotID");

            entity.HasOne(d => d.User).WithMany(p => p.ProfitDistributionLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProfitDistributionLogs_UserID");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.ProfitDistributionLogs).HasConstraintName("FK_ProfitDistributionLogs_WalletTransactionID");
        });

        modelBuilder.Entity<ShareOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__ShareOrd__C3905BAF6F52EEFD");

            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.ShareOrderSide).WithMany(p => p.ShareOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareOrders_OrderSideID");

            entity.HasOne(d => d.ShareOrderStatus).WithMany(p => p.ShareOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareOrders_OrderStatusID");

            entity.HasOne(d => d.ShareOrderType).WithMany(p => p.ShareOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareOrders_OrderTypeID");

            entity.HasOne(d => d.TradingAccount).WithMany(p => p.ShareOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareOrders_TradingAccountID");

            entity.HasOne(d => d.User).WithMany(p => p.ShareOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareOrders_UserID");
        });

        modelBuilder.Entity<ShareOrderSide>(entity =>
        {
            entity.HasKey(e => e.OrderSideId).HasName("PK__ShareOrd__903F74128F1A11B1");
        });

        modelBuilder.Entity<ShareOrderStatus>(entity =>
        {
            entity.HasKey(e => e.OrderStatusId).HasName("PK__ShareOrd__BC674F41CC21CCBD");
        });

        modelBuilder.Entity<ShareOrderType>(entity =>
        {
            entity.HasKey(e => e.OrderTypeId).HasName("PK__ShareOrd__23AC264C907B9CD8");
        });

        modelBuilder.Entity<SharePortfolio>(entity =>
        {
            entity.HasKey(e => e.PortfolioId).HasName("PK__SharePor__6D3A139D5175265C");

            entity.Property(e => e.LastUpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.TradingAccount).WithMany(p => p.SharePortfolios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SharePortfolios_TradingAccountID");

            entity.HasOne(d => d.User).WithMany(p => p.SharePortfolios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SharePortfolios_UserID");
        });

        modelBuilder.Entity<ShareTrade>(entity =>
        {
            entity.HasKey(e => e.TradeId).HasName("PK__ShareTra__3028BABB7AC7A68D");

            entity.Property(e => e.TradeDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.BuyOrder).WithMany(p => p.ShareTradeBuyOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareTrades_BuyOrderID");

            entity.HasOne(d => d.BuyerUser).WithMany(p => p.ShareTradeBuyerUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareTrades_BuyerUserID");

            entity.HasOne(d => d.InitialShareOffering).WithMany(p => p.ShareTrades).HasConstraintName("FK_ShareTrades_InitialShareOfferingID");

            entity.HasOne(d => d.SellOrder).WithMany(p => p.ShareTradeSellOrders).HasConstraintName("FK_ShareTrades_SellOrderID");

            entity.HasOne(d => d.SellerUser).WithMany(p => p.ShareTradeSellerUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareTrades_SellerUserID");

            entity.HasOne(d => d.TradingAccount).WithMany(p => p.ShareTrades)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShareTrades_TradingAccountID");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__SystemSe__54372AFDA4FA27A3");

            entity.Property(e => e.IsEditableByAdmin).HasDefaultValue(true);
            entity.Property(e => e.LastUpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SettingDataType).HasDefaultValue("String");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.SystemSettings).HasConstraintName("FK_SystemSettings_UpdatedByUserID");
        });

        modelBuilder.Entity<TradingAccount>(entity =>
        {
            entity.HasKey(e => e.TradingAccountId).HasName("PK__TradingA__83BE6AADE220CC83");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CurrentSharePrice).HasComputedColumnSql("(case when [TotalSharesIssued]>(0) then [CurrentNetAssetValue]/[TotalSharesIssued] else (0) end)", true);
            entity.Property(e => e.Eaname).HasDefaultValue("Quantum Bands AI");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.TradingAccounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TradingAccounts_CreatedByUserID");
        });

        modelBuilder.Entity<TradingAccountSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId).HasName("PK__TradingA__664F570B04BADEE1");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.TradingAccount).WithMany(p => p.TradingAccountSnapshots)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TradingAccountSnapshots_TradingAccountID");
        });

        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.HasKey(e => e.TransactionTypeId).HasName("PK__Transact__20266CEB66053C83");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC4CD84DCB");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_RoleID");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__UserRole__8AFACE3AA7A0796F");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__84D4F92EA98FAE17");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CurrencyCode).HasDefaultValue("USD");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithOne(p => p.Wallet)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wallets_UserID");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__WalletTr__55433A4BB64337D4");

            entity.Property(e => e.CurrencyCode).HasDefaultValue("USD");
            entity.Property(e => e.TransactionDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.RelatedTransaction).WithMany(p => p.InverseRelatedTransaction).HasConstraintName("FK_WalletTransactions_RelatedTransactionID");

            entity.HasOne(d => d.TransactionType).WithMany(p => p.WalletTransactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletTransactions_TransactionTypeID");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletTransactions_WalletID");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
