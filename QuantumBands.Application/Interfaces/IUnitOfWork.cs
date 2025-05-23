// QuantumBands.Application/Interfaces/IUnitOfWork.cs
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
    IGenericRepository<EAOpenPosition> EAOpenPositions { get; } // Thêm mới
    IGenericRepository<EAClosedTrade> EAClosedTrades { get; } // Thêm mới
    IGenericRepository<TradingAccountSnapshot> TradingAccountSnapshots { get; } // Thêm mới
    IGenericRepository<SharePortfolio> SharePortfolios { get; } // Thêm mới
    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}