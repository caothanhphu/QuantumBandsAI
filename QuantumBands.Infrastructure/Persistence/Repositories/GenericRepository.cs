// QuantumBands.Infrastructure/Persistence/Repositories/GenericRepository.cs
using Microsoft.EntityFrameworkCore;
using QuantumBands.Application.Interfaces;
using QuantumBands.Infrastructure.Persistence.DataContext; // Namespace của DbContext
using System.Linq.Expressions;

namespace QuantumBands.Infrastructure.Persistence.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly FinixAIDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(FinixAIDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity,cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        _dbSet.Attach(entity); // Đính kèm entity vào context
        _context.Entry(entity).State = EntityState.Modified; // Đánh dấu là đã thay đổi
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }
    // Lưu ý: Bạn cần một Unit of Work pattern hoặc gọi _context.SaveChangesAsync()
    // từ Application layer (service) để lưu các thay đổi vào database.
    // Repository không nên tự gọi SaveChangesAsync().
}