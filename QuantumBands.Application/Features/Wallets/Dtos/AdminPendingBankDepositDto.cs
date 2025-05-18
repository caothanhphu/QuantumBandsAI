// QuantumBands.Application/Features/Wallets/Dtos/AdminPendingBankDepositDto.cs
namespace QuantumBands.Application.Features.Wallets.Dtos;

public class AdminPendingBankDepositDto
{
    public long TransactionId { get; set; }
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string UserEmail { get; set; }
    public decimal AmountUSD { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal? AmountVND { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? ReferenceCode { get; set; }
    public required string PaymentMethod { get; set; }
    public required string Status { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Description { get; set; }
}