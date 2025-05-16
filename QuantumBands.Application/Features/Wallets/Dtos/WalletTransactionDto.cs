// QuantumBands.Application/Features/Wallets/Dtos/WalletTransactionDto.cs
namespace QuantumBands.Application.Features.Wallets.Dtos;

public class WalletTransactionDto
{
    public long TransactionId { get; set; }
    public required string TransactionTypeName { get; set; }
    public decimal Amount { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceId { get; set; }
    public string? PaymentMethod { get; set; } // Thêm mới
    public string? ExternalTransactionId { get; set; } // Thêm mới
    public string? Description { get; set; }
    public required string Status { get; set; }
    public DateTime TransactionDate { get; set; }
}