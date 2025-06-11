using FluentValidation;
using TeamsReportDashboard.Backend.Models.ReportDto;

namespace TeamsReportDashboard.Backend.Services.Report.Create;

public class CreateReportValidator : AbstractValidator<CreateReportDto>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.RequesterId)
            .NotEmpty().WithMessage("O solicitante é obrigatório.");
        

        RuleFor(x => x.TechnicianName)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.TechnicianName))
            .WithMessage("O nome do técnico deve ter no máximo 50 caracteres.");

        RuleFor(x => x.RequestDate)
            .NotEmpty().WithMessage("A data da solicitação é obrigatória.")
            .LessThanOrEqualTo(DateTime.Now).When(x => x.RequestDate != default(DateTime)) // Sugestão: não pode ser no futuro
            .WithMessage("A data da solicitação não pode ser no futuro.");

        RuleFor(x => x.ReportedProblem)
            .NotEmpty().WithMessage("O problema relatado é obrigatório.")
            .MaximumLength(255).WithMessage("O problema relatado deve ter no máximo 255 caracteres.");
        
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("A categoria do problema relatado é obrigatório.")
            .MaximumLength(255).WithMessage("A categoria do problema relatado deve ter no máximo 255 caracteres.");

        RuleFor(x => x.FirstResponseTime)
            .NotEmpty().WithMessage("O tempo da primeira resposta é obrigatório.")
            .GreaterThan(TimeSpan.Zero).WithMessage("O tempo da primeira resposta deve ser um valor positivo."); // Sugestão

        RuleFor(x => x.AverageHandlingTime)
            .NotEmpty().WithMessage("O tempo médio de atendimento é obrigatório.")
            .GreaterThan(TimeSpan.Zero).WithMessage("O tempo médio de atendimento deve ser um valor positivo."); // Sugestão
    }
}