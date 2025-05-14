// QuantumBands.Application/Features/Authentication/Commands/ResetPassword/ResetPasswordRequest.cs
namespace QuantumBands.Application.Features.Authentication.Commands.ResetPassword;

public class ResetPasswordRequest
{
    public required string Email { get; set; }
    public required string ResetToken { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}