// QuantumBands.Application/Features/Wallets/Dtos/WithdrawalRequestDto.cs
namespace QuantumBands.Application.Features.Wallets.Dtos;

public class WithdrawalRequestDto
{
    public long WithdrawalRequestId { get; set; } // Sẽ là TransactionID
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public required string CurrencyCode { get; set; }
    public required string Status { get; set; }
    public required string WithdrawalMethodDetails { get; set; }
    public string? Notes { get; set; }
    public DateTime RequestedAt { get; set; }
}