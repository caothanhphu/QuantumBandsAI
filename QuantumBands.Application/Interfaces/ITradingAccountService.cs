// QuantumBands.Application/Interfaces/ITradingAccountService.cs
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Application.Interfaces;

public interface ITradingAccountService
{
    Task<(TradingAccountDto? Account, string? ErrorMessage)> CreateTradingAccountAsync(CreateTradingAccountRequest request, ClaimsPrincipal adminUser, CancellationToken cancellationToken = default);
    Task<(InitialShareOfferingDto? Offering, string? ErrorMessage)> CreateInitialShareOfferingAsync(int tradingAccountId, CreateInitialShareOfferingRequest request, ClaimsPrincipal adminUser, CancellationToken cancellationToken = default);
}