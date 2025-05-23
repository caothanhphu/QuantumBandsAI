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

    public IGenericRepository<InitialShareOffering> InitialShareOfferings { get; private set; }
    public IGenericRepository<TradingAccount> TradingAccounts { get; private set; }
    public IGenericRepository<EAOpenPosition> EAOpenPositions { get; private set; }
    public IGenericRepository<EAClosedTrade> EAClosedTrades { get; private set; }
    public IGenericRepository<TradingAccountSnapshot> TradingAccountSnapshots { get; private set; }
    public IGenericRepository<SharePortfolio> SharePortfolios { get; private set; }

    public UnitOfWork(FinixAIDbContext context)
    {
        _context = context;
        UserRoles = new UserRoleRepository(_context); // Giả sử UserRoleRepository đã có
        Users = new GenericRepository<User>(_context); // Khởi tạo generic repository
        Wallets = new GenericRepository<Wallet>(_context); // Khởi tạo generic repository
        WalletTransactions = new GenericRepository<WalletTransaction>(_context); // Khởi tạo generic repository
        InitialShareOfferings = new GenericRepository<InitialShareOffering>(_context); // Khởi tạo generic repository
        TradingAccounts = new GenericRepository<TradingAccount>(_context); // Khởi tạo generic repository
        EAOpenPositions = new GenericRepository<EAOpenPosition>(_context); // Khởi tạo generic repository
        EAClosedTrades = new GenericRepository<EAClosedTrade>(_context); // Khởi tạo generic repository
        TradingAccountSnapshots = new GenericRepository<TradingAccountSnapshot>(_context); // Khởi tạo generic repository
        SharePortfolios = new GenericRepository<SharePortfolio>(_context); // Khởi tạo generic repository
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