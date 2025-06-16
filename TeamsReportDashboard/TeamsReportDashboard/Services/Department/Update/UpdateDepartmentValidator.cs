using FluentValidation;
using TeamsReportDashboard.Backend.Models.DepartmentDto;

namespace TeamsReportDashboard.Backend.Services.Department.Update;

public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentDto>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(x => x.Name).MaximumLength(30).NotEmpty().WithMessage("Department name cannot be empty");
    }
}