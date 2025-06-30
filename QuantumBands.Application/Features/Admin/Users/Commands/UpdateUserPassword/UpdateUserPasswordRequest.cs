// QuantumBands.Application/Features/Admin/Users/Commands/UpdateUserPassword/UpdateUserPasswordRequest.cs
namespace QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserPassword;

public class UpdateUserPasswordRequest
{
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
    public string? Reason { get; set; }
}