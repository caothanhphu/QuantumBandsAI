// QuantumBands.Infrastructure/Persistence/UnitOfWork.cs
using QuantumBands.Application.Interfaces;
using QuantumBands.Infrastructure.Persistence.DataContext;
using QuantumBands.Infrastructure.Persistence.Repositories; // Nơi chứa UserRoleRepository
using QuantumBands.Domain.Entities; // Thêm using này
using System.Threading.Tasks; // Thêm using này
using System.Threading; // Thêm using này

namespace QuantumBands.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly FinixAIDbContext _context;
    public IUserRoleRepository UserRoles { get; private set; }
    // public IUserRepository Users { get; private set; }
    public IGenericRepository<User> Users { get; private set; }
    public IGenericRepository<Wallet> Wallets { get; private set; }
    public IGenericRepository<WalletTransaction> WalletTransactions { get; private set; }

    public UnitOfWork(FinixAIDbContext context)
    {
        _context = context;
        UserRoles = new UserRoleRepository(_context); // Giả sử UserRoleRepository đã có
        Users = new GenericRepository<User>(_context); // Khởi tạo generic repository
        Wallets = new GenericRepository<Wallet>(_context); // Khởi tạo generic repository
        WalletTransactions = new GenericRepository<WalletTransaction>(_context); // Khởi tạo generic repository
    }

    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}