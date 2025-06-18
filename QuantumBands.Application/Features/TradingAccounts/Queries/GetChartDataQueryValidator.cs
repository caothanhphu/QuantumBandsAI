// QuantumBands.Application/Features/TradingAccounts/Queries/GetChartDataQueryValidator.cs
using FluentValidation;
using QuantumBands.Application.Features.TradingAccounts.Enums;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

/// <summary>
/// Validator for GetChartDataQuery to ensure valid input parameters
/// </summary>
public class GetChartDataQueryValidator : AbstractValidator<GetChartDataQuery>
{
    public GetChartDataQueryValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Chart type must be one of: balance, equity, growth, drawdown");
        
        RuleFor(x => x.Period)
            .IsInEnum()
            .WithMessage("Period must be one of: 1M, 3M, 6M, 1Y, ALL");
        
        RuleFor(x => x.Interval)
            .IsInEnum()
            .WithMessage("Interval must be one of: daily, weekly, monthly");
    }
}