// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/UpdateTradingAccountRequest.cs
namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;

public class UpdateTradingAccountRequest
{
    public string? Description { get; set; }
    public string? EaName { get; set; }
    public decimal? ManagementFeeRate { get; set; }
    public bool? IsActive { get; set; }
}