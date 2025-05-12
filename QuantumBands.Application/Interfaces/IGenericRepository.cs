// QuantumBands.Application/Interfaces/IGenericRepository.cs
using System.Linq.Expressions;

namespace QuantumBands.Application.Interfaces;

// Đảm bảo T là một class (entity)
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity); // EF Core theo dõi thay đổi, nên thường không cần async
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<int> CountAsync();
    IQueryable<T> Query();

}