// QuantumBands.Infrastructure/Persistence/Repositories/TransactionTypeRepository.cs
using Microsoft.EntityFrameworkCore;
using QuantumBands.Application.Interfaces.Repositories;
using QuantumBands.Domain.Entities;
using QuantumBands.Infrastructure.Persistence.DataContext;
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Infrastructure.Persistence.Repositories;

public class TransactionTypeRepository : GenericRepository<TransactionType>, ITransactionTypeRepository
{
    public TransactionTypeRepository(FinixAIDbContext context) : base(context)
    {
    }

    public async Task<TransactionType?> GetByNameAsync(string typeName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                tt => tt.TypeName.ToLower() == typeName.ToLower(),
                cancellationToken);
    }
}