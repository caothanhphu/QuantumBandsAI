// QuantumBands.Application/Interfaces/Repositories/IShareOrderStatusRepository.cs
using QuantumBands.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Application.Interfaces.Repositories;

public interface IShareOrderStatusRepository : IGenericRepository<ShareOrderStatus>
{
    Task<ShareOrderStatus?> GetByNameAsync(string statusName, CancellationToken cancellationToken = default);
}