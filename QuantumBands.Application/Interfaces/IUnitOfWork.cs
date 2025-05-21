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

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}