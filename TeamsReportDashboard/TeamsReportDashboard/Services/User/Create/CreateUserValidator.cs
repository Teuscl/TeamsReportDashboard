using FluentValidation;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.Create;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(user => user.Name).NotEmpty().MaximumLength(50);
        RuleFor(user => user.Email).NotEmpty().EmailAddress().MaximumLength(50).WithMessage("Invalid Email Address");
        RuleFor(user => user.Password.Length).GreaterThanOrEqualTo(8).WithMessage("Password must be at least 6 characters");
    }
}