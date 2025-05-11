// QuantumBands.Infrastructure/Persistence/UnitOfWork.cs
using QuantumBands.Application.Interfaces;
using QuantumBands.Infrastructure.Persistence.DataContext;
using QuantumBands.Infrastructure.Persistence.Repositories; // Nơi chứa UserRoleRepository

namespace QuantumBands.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly FinixAIDbContext _context;
    public IUserRoleRepository UserRoles { get; private set; }
    // public IUserRepository Users { get; private set; }

    public UnitOfWork(FinixAIDbContext context)
    {
        _context = context;
        UserRoles = new UserRoleRepository(_context); // Khởi tạo repository
        // Users = new UserRepository(_context);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}