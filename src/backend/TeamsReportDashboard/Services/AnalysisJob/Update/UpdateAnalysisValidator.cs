using FluentValidation;
using TeamsReportDashboard.Backend.Models.Job;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Update;

public class UpdateAnalysisValidator : AbstractValidator<UpdateAnalysisJobDto>
{
    public UpdateAnalysisValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Job's name cannot be empty")
            .MaximumLength(100).WithMessage("Job's name cannot be longer than 100 characters");;
    }
}