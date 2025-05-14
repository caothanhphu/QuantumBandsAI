// QuantumBands.Application/Features/Users/Commands/ChangePassword/ChangePasswordRequest.cs
namespace QuantumBands.Application.Features.Users.Commands.ChangePassword;

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}