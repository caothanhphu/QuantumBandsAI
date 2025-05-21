// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/CreateTradingAccountRequest.cs
namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
public class CreateTradingAccountRequest
{
    public required string AccountName { get; set; }
    public string? Description { get; set; }
    public string? EaName { get; set; }
    public string? BrokerPlatformIdentifier { get; set; }
    public decimal InitialCapital { get; set; }
    public long TotalSharesIssued { get; set; }
    public decimal ManagementFeeRate { get; set; }
}