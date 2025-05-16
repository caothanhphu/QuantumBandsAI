// QuantumBands.Application/Features/Wallets/Dtos/WalletDto.cs
namespace QuantumBands.Application.Features.Wallets.Dtos;

public class WalletDto
{
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public decimal Balance { get; set; }
    public required string CurrencyCode { get; set; }
    public required string EmailForQrCode { get; set; }
    public DateTime UpdatedAt { get; set; }
}