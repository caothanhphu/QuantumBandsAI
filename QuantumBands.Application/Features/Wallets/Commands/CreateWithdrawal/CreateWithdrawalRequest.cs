// QuantumBands.Application/Features/Wallets/Commands/CreateWithdrawal/CreateWithdrawalRequest.cs
namespace QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;

public class CreateWithdrawalRequest
{
    public decimal Amount { get; set; }
    public required string CurrencyCode { get; set; } // Mặc định là "USD"
    public required string WithdrawalMethodDetails { get; set; }
    public string? Notes { get; set; }
}