using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuantumBands.Infrastructure.Persistence.DataContext.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShareOrderSides",
                columns: table => new
                {
                    OrderSideID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SideName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ShareOrd__903F74128F1A11B1", x => x.OrderSideID);
                });

            migrationBuilder.CreateTable(
                name: "ShareOrderStatuses",
                columns: table => new
                {
                    OrderStatusID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ShareOrd__BC674F41CC21CCBD", x => x.OrderStatusID);
                });

            migrationBuilder.CreateTable(
                name: "ShareOrderTypes",
                columns: table => new
                {
                    OrderTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ShareOrd__23AC264C907B9CD8", x => x.OrderTypeID);
                });

            migrationBuilder.CreateTable(
                name: "TransactionTypes",
                columns: table => new
                {
                    TransactionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsCredit = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Transact__20266CEB66053C83", x => x.TransactionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserRole__8AFACE3AA7A0796F", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CCAC4CD84DCB", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_RoleID",
                        column: x => x.RoleID,
                        principalTable: "UserRoles",
                        principalColumn: "RoleID");
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    SettingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SettingDataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "String"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsEditableByAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedByUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SystemSe__54372AFDA4FA27A3", x => x.SettingID);
                    table.ForeignKey(
                        name: "FK_SystemSettings_UpdatedByUserID",
                        column: x => x.UpdatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "TradingAccounts",
                columns: table => new
                {
                    TradingAccountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EAName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, defaultValue: "Quantum Bands AI"),
                    BrokerPlatformIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InitialCapital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalSharesIssued = table.Column<long>(type: "bigint", nullable: false),
                    CurrentNetAssetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentSharePrice = table.Column<decimal>(type: "decimal(38,22)", nullable: true, computedColumnSql: "(case when [TotalSharesIssued]>(0) then [CurrentNetAssetValue]/[TotalSharesIssued] else (0) end)", stored: true),
                    ManagementFeeRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TradingA__83BE6AADE220CC83", x => x.TradingAccountID);
                    table.ForeignKey(
                        name: "FK_TradingAccounts_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    WalletID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false, defaultValue: "USD"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Wallets__84D4F92EA98FAE17", x => x.WalletID);
                    table.ForeignKey(
                        name: "FK_Wallets_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "EAClosedTrades",
                columns: table => new
                {
                    ClosedTradeID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradingAccountID = table.Column<int>(type: "int", nullable: false),
                    EATicketID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TradeType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    VolumeLots = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    OpenTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosePrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    CloseTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Swap = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0.00m),
                    Commission = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0.00m),
                    RealizedPAndL = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsProcessedForDailyPAndL = table.Column<bool>(type: "bit", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EAClosed__9C45B584F04B6DEB", x => x.ClosedTradeID);
                    table.ForeignKey(
                        name: "FK_EAClosedTrades_TradingAccountID",
                        column: x => x.TradingAccountID,
                        principalTable: "TradingAccounts",
                        principalColumn: "TradingAccountID");
                });

            migrationBuilder.CreateTable(
                name: "EAOpenPositions",
                columns: table => new
                {
                    OpenPositionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradingAccountID = table.Column<int>(type: "int", nullable: false),
                    EATicketID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TradeType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    VolumeLots = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    OpenTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentMarketPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Swap = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0.00m),
                    Commission = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0.00m),
                    FloatingPAndL = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EAOpenPo__A2749288012FED3A", x => x.OpenPositionID);
                    table.ForeignKey(
                        name: "FK_EAOpenPositions_TradingAccountID",
                        column: x => x.TradingAccountID,
                        principalTable: "TradingAccounts",
                        principalColumn: "TradingAccountID");
                });

            migrationBuilder.CreateTable(
                name: "InitialShareOfferings",
                columns: table => new
                {
                    OfferingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradingAccountID = table.Column<int>(type: "int", nullable: false),
                    AdminUserID = table.Column<int>(type: "int", nullable: false),
                    SharesOffered = table.Column<long>(type: "bigint", nullable: false),
                    SharesSold = table.Column<long>(type: "bigint", nullable: false),
                    OfferingPricePerShare = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    FloorPricePerShare = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    CeilingPricePerShare = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    OfferingStartDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    OfferingEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__InitialS__3500D7CD2B65639F", x => x.OfferingID);
                    table.ForeignKey(
                        name: "FK_InitialShareOfferings_AdminUserID",
                        column: x => x.AdminUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_InitialShareOfferings_TradingAccountID",
                        column: x => x.TradingAccountID,
                        principalTable: "TradingAccounts",
                        principalColumn: "TradingAccountID");
                });

            migrationBuilder.CreateTable(
                name: "SharePortfolios",
                columns: table => new
                {
                    PortfolioID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    TradingAccountID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    AverageBuyPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SharePor__6D3A139D5175265C", x => x.PortfolioID);
                    table.ForeignKey(
                        name: "FK_SharePortfolios_TradingAccountID",
                        column: x => x.TradingAccountID,
                        principalTable: "TradingAccounts",
                        principalColumn: "TradingAccountID");
                    table.ForeignKey(
                        name: "FK_SharePortfolios_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "TradingAccountSnapshots",
                columns: table => new
                {
                    SnapshotID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradingAccountID = table.Column<int>(type: "int", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpeningNAV = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RealizedPAndLForTheDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnrealizedPAndLForTheDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ManagementFeeDeducted = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitDistributed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingNAV = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingSharePrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TradingA__664F570B04BADEE1", x => x.SnapshotID);
                    table.ForeignKey(
                        name: "FK_TradingAccountSnapshots_TradingAccountID",
                        column: x => x.TradingAccountID,
                        principalTable: "TradingAccounts",
                        principalColumn: "TradingAccountID");
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletID = table.Column<int>(type: "int", nullable: false),
                    TransactionTypeID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ReferenceID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Completed"),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    RelatedTransactionID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WalletTr__55433A4BB64337D4", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_RelatedTransactionID",
                        column: x => x.RelatedTransactionID,
                        principalTable: "WalletTransactions",
                        principalColumn: "TransactionID");
                    table.ForeignKey(
                        name: "FK_WalletTransactions_TransactionTypeID",
                        column: x => x.TransactionTypeID,
                        principalTable: "TransactionTypes",
                        principalColumn: "TransactionTypeID");
                    table.ForeignKey(
                        name: "FK_WalletTransactions_WalletID",
                        column: x => x.WalletID,
                        principalTable: "Wallets",
                        principalColumn: "WalletID");
                });

            migrationBuilder.CreateIndex(
                name: "UQ_EAClosedTrades_Account_Ticket",
                table: "EAClosedTrades",
                columns: new[] { "TradingAccountID", "EATicketID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_EAOpenPositions_Account_Ticket",
                table: "EAOpenPositions",
                columns: new[] { "TradingAccountID", "EATicketID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitialShareOfferings_AdminUserID",
                table: "InitialShareOfferings",
                column: "AdminUserID");

            migrationBuilder.CreateIndex(
                name: "IX_InitialShareOfferings_TradingAccountID",
                table: "InitialShareOfferings",
                column: "TradingAccountID");

            migrationBuilder.CreateIndex(
                name: "UQ__ShareOrd__8D8D27303DD8CB84",
                table: "ShareOrderSides",
                column: "SideName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__ShareOrd__05E7698A1417F64A",
                table: "ShareOrderStatuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__ShareOrd__D4E7DFA8F376B605",
                table: "ShareOrderTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharePortfolios_TradingAccountID",
                table: "SharePortfolios",
                column: "TradingAccountID");

            migrationBuilder.CreateIndex(
                name: "UQ_SharePortfolios_User_TradingAccount",
                table: "SharePortfolios",
                columns: new[] { "UserID", "TradingAccountID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedByUserID",
                table: "SystemSettings",
                column: "UpdatedByUserID");

            migrationBuilder.CreateIndex(
                name: "UQ__SystemSe__01E719ADD25DB536",
                table: "SystemSettings",
                column: "SettingKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingAccounts_CreatedByUserID",
                table: "TradingAccounts",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "UQ__TradingA__406E0D2EB419829E",
                table: "TradingAccounts",
                column: "AccountName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TradingAccountSnapshots_Account_Date",
                table: "TradingAccountSnapshots",
                columns: new[] { "TradingAccountID", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Transact__D4E7DFA81202EEAE",
                table: "TransactionTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__UserRole__8A2B6160E9226D17",
                table: "UserRoles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__536C85E46C1E47F9",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D10534121601FF",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Wallets__1788CCAD0AF96292",
                table: "Wallets",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_RelatedTransactionID",
                table: "WalletTransactions",
                column: "RelatedTransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_TransactionTypeID",
                table: "WalletTransactions",
                column: "TransactionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletID",
                table: "WalletTransactions",
                column: "WalletID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EAClosedTrades");

            migrationBuilder.DropTable(
                name: "EAOpenPositions");

            migrationBuilder.DropTable(
                name: "InitialShareOfferings");

            migrationBuilder.DropTable(
                name: "ShareOrderSides");

            migrationBuilder.DropTable(
                name: "ShareOrderStatuses");

            migrationBuilder.DropTable(
                name: "ShareOrderTypes");

            migrationBuilder.DropTable(
                name: "SharePortfolios");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TradingAccountSnapshots");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "TradingAccounts");

            migrationBuilder.DropTable(
                name: "TransactionTypes");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UserRoles");
        }
    }
}
