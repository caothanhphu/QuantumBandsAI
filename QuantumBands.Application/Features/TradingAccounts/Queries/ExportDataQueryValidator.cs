using FluentValidation;
using QuantumBands.Application.Features.TradingAccounts.Dtos;

namespace QuantumBands.Application.Features.TradingAccounts.Queries
{
    /// <summary>
    /// FluentValidation validator for ExportDataQuery.
    /// Ensures that export requests contain valid parameters and meet business rules.
    /// Part of SCRUM-101 implementation for data export functionality.
    /// </summary>
    public class ExportDataQueryValidator : AbstractValidator<ExportDataQuery>
    {
        /// <summary>
        /// Initializes validation rules for export data queries.
        /// Validates export type, format, date ranges, symbols, template, and email parameters.
        /// </summary>
        public ExportDataQueryValidator()
        {
            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid export type");

            RuleFor(x => x.Format)
                .IsInEnum()
                .WithMessage("Invalid export format");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("StartDate must be less than or equal to EndDate");

            RuleFor(x => x.EndDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.EndDate.HasValue)
                .WithMessage("EndDate cannot be in the future");

            RuleFor(x => x.Symbols)
                .MaximumLength(200)
                .WithMessage("Symbols list cannot exceed 200 characters");

            RuleFor(x => x.Template)
                .MaximumLength(50)
                .WithMessage("Template name cannot exceed 50 characters");

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Invalid email address format");

            // Date range validation - maximum 1 year
            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Date range cannot exceed 365 days");

            // PDF format validation
            RuleFor(x => x.Format)
                .Must((query, format) => ValidatePdfFormat(query, format))
                .WithMessage("PDF format is only supported for Statistics and PerformanceReport types");
        }

        /// <summary>
        /// Validates that the date range between StartDate and EndDate does not exceed 365 days.
        /// This prevents performance issues and ensures reasonable export sizes.
        /// </summary>
        /// <param name="query">The export query to validate</param>
        /// <returns>True if date range is valid or not specified, false if exceeds limit</returns>
        private static bool HaveValidDateRange(ExportDataQuery query)
        {
            if (!query.StartDate.HasValue || !query.EndDate.HasValue)
                return true;

            var daysDifference = (query.EndDate.Value - query.StartDate.Value).TotalDays;
            return daysDifference <= 365;
        }

        /// <summary>
        /// Validates that PDF format is only used with compatible export types.
        /// PDF generation is complex and only supported for certain report types.
        /// </summary>
        /// <param name="query">The export query to validate</param>
        /// <param name="format">The export format being validated</param>
        /// <returns>True if format is valid for the export type, false otherwise</returns>
        private static bool ValidatePdfFormat(ExportDataQuery query, ExportFormat format)
        {
            if (format != ExportFormat.PDF)
                return true;

            return query.Type == ExportType.Statistics || 
                   query.Type == ExportType.PerformanceReport || 
                   query.Type == ExportType.RiskReport;
        }
    }
}