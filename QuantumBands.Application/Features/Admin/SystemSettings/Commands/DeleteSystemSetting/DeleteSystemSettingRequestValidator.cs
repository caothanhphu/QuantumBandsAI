using FluentValidation;

namespace QuantumBands.Application.Features.Admin.SystemSettings.Commands.DeleteSystemSetting
{
    public class DeleteSystemSettingRequestValidator : AbstractValidator<DeleteSystemSettingRequest>
    {
        public DeleteSystemSettingRequestValidator()
        {
            RuleFor(x => x.SettingId)
                .GreaterThan(0).WithMessage("Setting ID must be greater than 0.");
        }
    }
}