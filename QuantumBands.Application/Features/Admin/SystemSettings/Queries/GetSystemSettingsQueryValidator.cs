using FluentValidation;

namespace QuantumBands.Application.Features.Admin.SystemSettings.Queries
{
    public class GetSystemSettingsQueryValidator : AbstractValidator<GetSystemSettingsQuery>
    {
        public GetSystemSettingsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

            RuleFor(x => x.SortBy)
                .Must(BeValidSortField).WithMessage("Sort by must be one of: SettingKey, SettingDataType, LastUpdatedAt, IsEditableByAdmin.");

            RuleFor(x => x.SortOrder)
                .Must(BeValidSortOrder).WithMessage("Sort order must be 'asc' or 'desc'.");

            RuleFor(x => x.SearchTerm)
                .MaximumLength(200).WithMessage("Search term cannot exceed 200 characters.");

            RuleFor(x => x.DataType)
                .Must(BeValidDataType).When(x => !string.IsNullOrEmpty(x.DataType))
                .WithMessage("Data type must be one of: string, int, decimal, boolean.");
        }

        private bool BeValidSortField(string sortBy)
        {
            var validSortFields = new[] { "SettingKey", "SettingDataType", "LastUpdatedAt", "IsEditableByAdmin" };
            return validSortFields.Contains(sortBy);
        }

        private bool BeValidSortOrder(string sortOrder)
        {
            return sortOrder?.ToLowerInvariant() is "asc" or "desc";
        }

        private bool BeValidDataType(string dataType)
        {
            var validTypes = new[] { "string", "int", "decimal", "boolean" };
            return validTypes.Contains(dataType?.ToLowerInvariant());
        }
    }
}