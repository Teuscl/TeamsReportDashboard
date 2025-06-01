using FluentValidation;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;

public class ChangeMyPasswordValidator : AbstractValidator<ChangeMyPasswordDto>
{
    public ChangeMyPasswordValidator()
    {
        //Validate if the old password was entered
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("Old Password is required");
        
        //Check if new password match the criteria
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("New Password must be at least 8 characters");
        
        //Check if the new password and the confirmation are equal
        RuleFor(x => x.NewPasswordConfirm)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");

    }
}