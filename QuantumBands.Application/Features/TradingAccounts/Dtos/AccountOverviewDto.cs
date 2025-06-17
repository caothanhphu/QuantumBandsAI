// QuantumBands.Application/Features/TradingAccounts/Dtos/AccountOverviewDto.cs
namespace QuantumBands.Application.Features.TradingAccounts.Dtos;

public class AccountOverviewDto
{
    public required AccountInfoDto AccountInfo { get; set; }
    public required BalanceInfoDto BalanceInfo { get; set; }
    public required PerformanceKPIsDto PerformanceKPIs { get; set; }
}

public class AccountInfoDto
{
    public string AccountId { get; set; } = null!;
    public string AccountName { get; set; } = null!;
    public string Login { get; set; } = null!;
    public string Server { get; set; } = null!;
    public string AccountType { get; set; } = null!; // Real|Demo
    public string TradingPlatform { get; set; } = "MT5";
    public bool HedgingAllowed { get; set; }
    public decimal Leverage { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime LastActivity { get; set; }
    public string Status { get; set; } = null!; // Active|Inactive|Suspended
}

public class BalanceInfoDto
{
    public decimal CurrentBalance { get; set; }
    public decimal CurrentEquity { get; set; }
    public decimal FreeMargin { get; set; }
    public decimal MarginLevel { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal InitialDeposit { get; set; }
}

public class PerformanceKPIsDto
{
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; } // percentage
    public decimal ProfitFactor { get; set; }
    public decimal MaxDrawdown { get; set; } // percentage
    public decimal MaxDrawdownAmount { get; set; }
    public decimal GrowthPercent { get; set; }
    public int ActiveDays { get; set; }
}
