// QuantumBands.Application/Features/Wallets/Commands/AdminDeposit/AdminDirectDepositRequest.cs
namespace QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;

public class AdminDirectDepositRequest
{
    public int UserId { get; set; }
    public decimal Amount { get; set; } // Số tiền USD
    public required string CurrencyCode { get; set; } // Mặc định "USD"
    public required string Description { get; set; }
    public string? ReferenceId { get; set; } // Optional
}