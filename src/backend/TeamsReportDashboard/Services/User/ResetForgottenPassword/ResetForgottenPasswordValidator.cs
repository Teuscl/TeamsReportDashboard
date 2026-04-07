using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.ResetForgottenPassword;

public class ResetForgottenPasswordValidator : AbstractValidator<ResetForgottenPasswordDto>
{
    public ResetForgottenPasswordValidator() {
        
        //Check if new password match the criteria
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("New Password must be at least 8 characters");
        
        //Check if the new password and the confirmation are equal
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}