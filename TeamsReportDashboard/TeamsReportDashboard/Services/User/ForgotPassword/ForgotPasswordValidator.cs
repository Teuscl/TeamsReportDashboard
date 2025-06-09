using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.ForgotPassword;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordValidator() {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}