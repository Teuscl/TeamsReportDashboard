using FluentValidation;
using TeamsReportDashboard.Backend.Models.DepartmentDto;
using TeamsReportDashboard.Backend.Models.ReportDto;

namespace TeamsReportDashboard.Backend.Services.Department.Create;

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Name).MaximumLength(30).NotEmpty().WithMessage("Name is required");
    }
}