﻿// QuantumBands.Application/Interfaces/ITradingAccountService.cs
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Interfaces;

public interface ITradingAccountService
{
    Task<(TradingAccountDto? Account, string? ErrorMessage)> CreateTradingAccountAsync(CreateTradingAccountRequest request, ClaimsPrincipal adminUser, CancellationToken cancellationToken = default);
    Task<(InitialShareOfferingDto? Offering, string? ErrorMessage)> CreateInitialShareOfferingAsync(int tradingAccountId, CreateInitialShareOfferingRequest request, ClaimsPrincipal adminUser, CancellationToken cancellationToken = default);
    Task<PaginatedList<TradingAccountDto>> GetPublicTradingAccountsAsync(GetPublicTradingAccountsQuery query, CancellationToken cancellationToken = default);
    Task<(TradingAccountDetailDto? Detail, string? ErrorMessage)> GetTradingAccountDetailsAsync(int accountId, GetTradingAccountDetailsQuery query, CancellationToken cancellationToken = default);
    Task<(TradingAccountDto? Account, string? ErrorMessage)> UpdateTradingAccountAsync(int accountId, UpdateTradingAccountRequest request, ClaimsPrincipal adminUser, CancellationToken cancellationToken = default);
    Task<(PaginatedList<InitialShareOfferingDto>? Offerings, string? ErrorMessage)> GetInitialShareOfferingsAsync(int tradingAccountId, GetInitialOfferingsQuery query, CancellationToken cancellationToken = default);

}