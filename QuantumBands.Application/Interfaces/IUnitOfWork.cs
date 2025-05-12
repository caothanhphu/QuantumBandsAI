// QuantumBands.Application/Interfaces/IUnitOfWork.cs
using QuantumBands.Domain.Entities; // Thêm using này

namespace QuantumBands.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRoleRepository UserRoles { get; }
    IGenericRepository<User> Users { get; } // Sử dụng generic repository cho User
    IGenericRepository<Wallet> Wallets { get; } // Sử dụng generic repository cho Wallet
                                                // Thêm các repository khác ở đây

    Task<int> CompleteAsync(CancellationToken cancellationToken = default); // Thêm CancellationToken
}