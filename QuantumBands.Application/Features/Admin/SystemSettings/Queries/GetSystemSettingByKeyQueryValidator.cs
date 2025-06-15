using FluentValidation;

namespace QuantumBands.Application.Features.Admin.SystemSettings.Queries
{
    public class GetSystemSettingByKeyQueryValidator : AbstractValidator<GetSystemSettingByKeyQuery>
    {
        public GetSystemSettingByKeyQueryValidator()
        {
            RuleFor(x => x.SettingKey)
                .NotEmpty().WithMessage("Setting key is required.")
                .Length(1, 100).WithMessage("Setting key must be between 1 and 100 characters.");
        }
    }
}