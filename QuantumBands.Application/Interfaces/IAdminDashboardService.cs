// QuantumBands.Application/Interfaces/IAdminDashboardService.cs
using QuantumBands.Application.Features.Admin.Dashboard.Dtos;
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<(AdminDashboardSummaryDto? Summary, string? ErrorMessage)> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}