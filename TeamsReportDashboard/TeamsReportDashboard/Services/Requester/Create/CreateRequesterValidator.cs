using FluentValidation;
using TeamsReportDashboard.Backend.Models.Requester;

namespace TeamsReportDashboard.Backend.Services.Requester.Create;

public class CreateRequesterValidator : AbstractValidator<CreateRequesterDto>
{
    public CreateRequesterValidator()
    {
        RuleFor(requester => requester.Email)
            .NotEmpty()
            .MaximumLength(100)
            .EmailAddress()
            .WithMessage("Email is required");
        RuleFor(requester => requester.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Name is required");

        RuleFor(requester => requester.DepartmentId)
            .GreaterThan(0);
    }
}