// QuantumBands.Application/Features/Wallets/Commands/BankDeposit/BankDepositInfoResponse.cs
namespace QuantumBands.Application.Features.Wallets.Commands.BankDeposit;

public class BankDepositInfoResponse
{
    public long TransactionId { get; set; }
    public decimal RequestedAmountUSD { get; set; }
    public decimal AmountVND { get; set; }
    public decimal ExchangeRate { get; set; }
    public required string BankName { get; set; }
    public required string AccountHolder { get; set; }
    public required string AccountNumber { get; set; }
    public required string ReferenceCode { get; set; }
}