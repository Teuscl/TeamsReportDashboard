using FluentValidation;
using TeamsReportDashboard.Backend.Models.Job;

namespace TeamsReportDashboard.Backend.Services.Start;

public class StartAnalysisValidator : AbstractValidator<StartJobAnalysisDto>
{
    private const int MaxFileSize = 1024 * 1024 * 200;
    
    public StartAnalysisValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
        
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(file => file.FileName.ToLower().EndsWith(".zip"))
            .WithMessage("File must be a zip file")
            .Must(file => file.Length > 0)
            .WithMessage("File cannot be empty")
            .Must(file => file.Length <= MaxFileSize)
            .WithMessage($"File size is too large. Must be equal or lower than {MaxFileSize / 1024 / 1024}MB");
    }
}