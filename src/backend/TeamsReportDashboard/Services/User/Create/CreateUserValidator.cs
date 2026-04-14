using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.Create;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(user => user.Name).NotEmpty().MaximumLength(50);
        RuleFor(user => user.Email).NotEmpty().EmailAddress().MaximumLength(100).WithMessage("Invalid Email Address");
        RuleFor(user => user.Password.Length).GreaterThanOrEqualTo(8).WithMessage("Password must be at least 8 characters");
    }
}