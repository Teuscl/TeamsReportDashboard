using FluentValidation;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.Update;

public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(u => u.Email).NotEmpty().EmailAddress().WithMessage("Email is required");
        RuleFor(u => u.Name).NotEmpty().WithMessage("Name is required");
    }
}