// QuantumBands.Application/Interfaces/IUnitOfWork.cs
using QuantumBands.Application.Interfaces.Repositories;
using QuantumBands.Domain.Entities;

namespace QuantumBands.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRoleRepository UserRoles { get; }
    IGenericRepository<User> Users { get; }
    IGenericRepository<Wallet> Wallets { get; }
    IGenericRepository<WalletTransaction> WalletTransactions { get; }
    IGenericRepository<TradingAccount> TradingAccounts { get; }
    IGenericRepository<InitialShareOffering> InitialShareOfferings { get; }
    IGenericRepository<EaopenPosition> EAOpenPositions { get; } // Thêm mới
    IGenericRepository<EaclosedTrade> EAClosedTrades { get; } // Thêm mới
    IGenericRepository<TradingAccountSnapshot> TradingAccountSnapshots { get; } // Thêm mới
    IGenericRepository<SharePortfolio> SharePortfolios { get; } // Thêm mới
    IGenericRepository<ShareOrder> ShareOrders { get; }
    IGenericRepository<ShareOrderType> ShareOrderTypes { get; }
    IGenericRepository<ShareOrderSide> ShareOrderSides { get; }
    IGenericRepository<ShareOrderStatus> ShareOrderStatuses { get; }
    IGenericRepository<ShareTrade> ShareTrades { get; } // Thêm repo mới
    IGenericRepository<ProfitDistributionLog> ProfitDistributionLogs { get; } // Thêm repo mới
    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}