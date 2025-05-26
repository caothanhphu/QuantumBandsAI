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
        if (string.IsNullOrEmpty(statusName))
        {
            return null;
        }
        string statusNameLower = statusName.ToLower(); // Chuyển tham số sang chữ thường
        return await _dbSet.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusNameLower, cancellationToken);
    }
}