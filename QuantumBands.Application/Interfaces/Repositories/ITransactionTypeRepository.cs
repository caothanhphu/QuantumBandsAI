// QuantumBands.Application/Interfaces/Repositories/ITransactionTypeRepository.cs
using QuantumBands.Domain.Entities; // Assuming TransactionType entity is here
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Application.Interfaces.Repositories;

public interface ITransactionTypeRepository : IGenericRepository<TransactionType>
{
    Task<TransactionType?> GetByNameAsync(string typeName, CancellationToken cancellationToken = default);
}