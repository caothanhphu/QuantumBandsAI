// QuantumBands.Infrastructure/Persistence/Repositories/ShareOrderStatusRepository.cs
using Microsoft.EntityFrameworkCore;
using QuantumBands.Application.Interfaces.Repositories;
using QuantumBands.Domain.Entities;
using QuantumBands.Infrastructure.Persistence.DataContext;
using System; // For StringComparison
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Infrastructure.Persistence.Repositories;

public class ShareOrderStatusRepository : GenericRepository<ShareOrderStatus>, IShareOrderStatusRepository
{
    public ShareOrderStatusRepository(FinixAIDbContext context) : base(context)
    {
    }

    public async Task<ShareOrderStatus?> GetByNameAsync(string statusName, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.StatusName.Equals(statusName, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }
}