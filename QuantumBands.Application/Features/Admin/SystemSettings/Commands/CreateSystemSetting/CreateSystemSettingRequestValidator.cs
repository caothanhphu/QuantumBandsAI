using FluentValidation;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories;
using System.Globalization;

namespace QuantumBands.Application.Features.Admin.SystemSettings.Commands.CreateSystemSetting
{
    public class CreateSystemSettingRequestValidator : AbstractValidator<CreateSystemSettingRequest>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateSystemSettingRequestValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.SettingKey)
                .NotEmpty().WithMessage("Setting key is required.")
                .Length(1, 100).WithMessage("Setting key must be between 1 and 100 characters.")
                .MustAsync(BeUniqueSettingKey).WithMessage("Setting key already exists.");

            RuleFor(x => x.SettingValue)
                .NotEmpty().WithMessage("Setting value is required.")
                .MaximumLength(1000).WithMessage("Setting value cannot exceed 1000 characters.");

            RuleFor(x => x.SettingDataType)
                .NotEmpty().WithMessage("Setting data type is required.")
                .Must(BeValidDataType).WithMessage("Setting data type must be one of: string, int, decimal, boolean.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

            RuleFor(x => x)
                .Must(HaveValidValueForDataType).WithMessage("Setting value format is invalid for the specified data type.");
        }

        private async Task<bool> BeUniqueSettingKey(string settingKey, CancellationToken cancellationToken)
        {
            var existingSetting = await _unitOfWork.SystemSettings.GetSettingByKeyAsync(settingKey, cancellationToken);
            return existingSetting == null;
        }

        private bool BeValidDataType(string dataType)
        {
            var validTypes = new[] { "string", "int", "decimal", "boolean" };
            return validTypes.Contains(dataType?.ToLowerInvariant());
        }

        private bool HaveValidValueForDataType(CreateSystemSettingRequest request)
        {
            if (string.IsNullOrEmpty(request.SettingDataType) || string.IsNullOrEmpty(request.SettingValue))
                return false;

            return request.SettingDataType.ToLowerInvariant() switch
            {
                "string" => true, // Any string is valid
                "int" => int.TryParse(request.SettingValue, out _),
                "decimal" => decimal.TryParse(request.SettingValue, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
                "boolean" => bool.TryParse(request.SettingValue, out _) || 
                            request.SettingValue.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                            request.SettingValue.Equals("false", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}