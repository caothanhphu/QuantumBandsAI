using FluentValidation;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories;
using System.Globalization;

namespace QuantumBands.Application.Features.Admin.SystemSettings.Commands.UpdateSystemSetting
{
    public class UpdateSystemSettingRequestValidator : AbstractValidator<UpdateSystemSettingRequest>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSystemSettingRequestValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.SettingValue)
                .NotEmpty().WithMessage("Setting value is required.")
                .MaximumLength(1000).WithMessage("Setting value cannot exceed 1000 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        }

        public bool ValidateValueForDataType(string settingValue, string dataType)
        {
            if (string.IsNullOrEmpty(dataType) || string.IsNullOrEmpty(settingValue))
                return false;

            return dataType.ToLowerInvariant() switch
            {
                "string" => true, // Any string is valid
                "int" => int.TryParse(settingValue, out _),
                "decimal" => decimal.TryParse(settingValue, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
                "boolean" => bool.TryParse(settingValue, out _) || 
                            settingValue.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                            settingValue.Equals("false", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}