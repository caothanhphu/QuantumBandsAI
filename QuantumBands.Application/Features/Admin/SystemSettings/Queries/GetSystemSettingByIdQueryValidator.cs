using FluentValidation;

namespace QuantumBands.Application.Features.Admin.SystemSettings.Queries
{
    public class GetSystemSettingByIdQueryValidator : AbstractValidator<GetSystemSettingByIdQuery>
    {
        public GetSystemSettingByIdQueryValidator()
        {
            RuleFor(x => x.SettingId)
                .GreaterThan(0).WithMessage("Setting ID must be greater than 0.");
        }
    }
}