// QuantumBands.Infrastructure/Persistence/UnitOfWork.cs
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // Thêm using cho ISystemSettingRepository
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
    public IGenericRepository<EaopenPosition> EAOpenPositions { get; private set; }
    public IGenericRepository<EaclosedTrade> EAClosedTrades { get; private set; }
    public IGenericRepository<TradingAccountSnapshot> TradingAccountSnapshots { get; private set; }
    public IGenericRepository<SharePortfolio> SharePortfolios { get; private set; }
    public IGenericRepository<ShareOrder> ShareOrders { get; private set; }
    public IGenericRepository<ShareOrderType> ShareOrderTypes { get; private set; }
    public IGenericRepository<ShareOrderSide> ShareOrderSides { get; private set; }
    public IGenericRepository<ShareOrderStatus> ShareOrderStatuses { get; private set; }
    public IGenericRepository<ShareTrade> ShareTrades { get; private set; } // Thêm repo mới
    public IGenericRepository<ProfitDistributionLog> ProfitDistributionLogs { get; private set; } // Thêm repo mới
    public IGenericRepository<TransactionType> TransactionTypes { get; private set; } // Thêm TransactionType repository
    public ISystemSettingRepository SystemSettings { get; private set; } // Thêm SystemSetting repository

    public UnitOfWork(FinixAIDbContext context)
    {
        _context = context;
        UserRoles = new UserRoleRepository(_context); // Giả sử UserRoleRepository đã có
        Users = new GenericRepository<User>(_context); // Khởi tạo generic repository
        Wallets = new GenericRepository<Wallet>(_context); // Khởi tạo generic repository
        WalletTransactions = new GenericRepository<WalletTransaction>(_context); // Khởi tạo generic repository
        InitialShareOfferings = new GenericRepository<InitialShareOffering>(_context); // Khởi tạo generic repository
        TradingAccounts = new GenericRepository<TradingAccount>(_context); // Khởi tạo generic repository
        EAOpenPositions = new GenericRepository<EaopenPosition>(_context); // Khởi tạo generic repository
        EAClosedTrades = new GenericRepository<EaclosedTrade>(_context); // Khởi tạo generic repository
        TradingAccountSnapshots = new GenericRepository<TradingAccountSnapshot>(_context); // Khởi tạo generic repository
        SharePortfolios = new GenericRepository<SharePortfolio>(_context); // Khởi tạo generic repository
        ShareOrders = new GenericRepository<ShareOrder>(_context); // Khởi tạo generic repository
        ShareOrderTypes = new GenericRepository<ShareOrderType>(_context); // Khởi tạo generic repository
        ShareOrderSides = new GenericRepository<ShareOrderSide>(_context); // Khởi tạo generic repository
        ShareOrderStatuses = new GenericRepository<ShareOrderStatus>(_context); // Khởi tạo generic repository
        ShareTrades = new GenericRepository<ShareTrade>(_context); // Khởi tạo generic repository
        ProfitDistributionLogs = new GenericRepository<ProfitDistributionLog>(_context); // Khởi tạo generic repository
        TransactionTypes = new GenericRepository<TransactionType>(_context); // Khởi tạo TransactionType repository
        SystemSettings = new SystemSettingRepository(_context); // Khởi tạo SystemSetting repository
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