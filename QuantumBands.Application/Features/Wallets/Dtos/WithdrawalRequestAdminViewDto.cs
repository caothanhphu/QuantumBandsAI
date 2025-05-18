
// QuantumBands.Application/Features/Admin/Wallets/Dtos/WithdrawalRequestAdminViewDto.cs
namespace QuantumBands.Application.Features.Wallets.Dtos;

public class WithdrawalRequestAdminViewDto
{
    public long TransactionId { get; set; }
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string UserEmail { get; set; }
    public decimal Amount { get; set; }
    public required string CurrencyCode { get; set; }
    public required string Status { get; set; }
    public string? WithdrawalMethodDetails { get; set; }
    public string? UserNotes { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? AdminNotes { get; set; } // Thường là null cho danh sách pending
}