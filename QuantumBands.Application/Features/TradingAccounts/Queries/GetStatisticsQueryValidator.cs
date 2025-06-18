// QuantumBands.Application/Features/TradingAccounts/Queries/GetStatisticsQueryValidator.cs
using FluentValidation;
using QuantumBands.Application.Features.TradingAccounts.Enums;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetStatisticsQueryValidator : AbstractValidator<GetStatisticsQuery>
{
    public GetStatisticsQueryValidator()
    {
        RuleFor(x => x.Period)
            .IsInEnum()
            .WithMessage("Period must be a valid time period (1M, 3M, 6M, 1Y, ALL)");

        RuleFor(x => x.Symbols)
            .Must(BeValidSymbolList)
            .When(x => !string.IsNullOrEmpty(x.Symbols))
            .WithMessage("Symbols must be a comma-separated list of valid symbols without spaces");

        RuleFor(x => x.Benchmark)
            .Must(BeValidSymbol)
            .When(x => !string.IsNullOrEmpty(x.Benchmark))
            .WithMessage("Benchmark must be a valid symbol");
    }

    private static bool BeValidSymbolList(string? symbols)
    {
        if (string.IsNullOrEmpty(symbols))
            return true;

        var symbolArray = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return symbolArray.All(BeValidSymbol);
    }

    private static bool BeValidSymbol(string? symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return false;

        // Basic symbol validation - alphanumeric, max 20 chars, no spaces
        return symbol.Length <= 20 && 
               symbol.All(char.IsLetterOrDigit) && 
               !string.IsNullOrWhiteSpace(symbol);
    }
}