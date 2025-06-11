using FluentValidation;
using TeamsReportDashboard.Backend.Models.ReportDto;

namespace TeamsReportDashboard.Backend.Services.Report.Update; 
public class UpdateReportValidator : AbstractValidator<UpdateReportDto>
{
    public UpdateReportValidator()
    {
        // Para RequesterName: se fornecido (não nulo), não pode ser vazio e deve respeitar o tamanho máximo.
        RuleFor(x => x.RequesterId)
            .NotEmpty().WithMessage("O solicitante é obrigatório.");

        // Para TechnicianName: se fornecido (não nulo), não pode ser vazio e deve respeitar o tamanho máximo.
        // Isso permite que o cliente envie `null` para TechnicianName para limpá-lo, se a entidade permitir.
        RuleFor(x => x.TechnicianName)
            .NotEmpty().WithMessage("O nome do técnico não pode ser vazio se um valor for fornecido.")
            .MaximumLength(50).WithMessage("O nome do técnico deve ter no máximo 50 caracteres.")
            .When(x => x.TechnicianName != null);
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("O nome do técnico não pode ser vazio se um valor for fornecido.")
            .MaximumLength(50).WithMessage("O nome do técnico deve ter no máximo 50 caracteres.")
            .When(x => x.Category != null);

        // Para RequestDate: se fornecida, não pode ser no futuro.
        // A validação de "NotEmpty" implícita já é coberta por DateTime? tendo um valor.
        RuleFor(x => x.RequestDate)
            .LessThanOrEqualTo(DateTime.Now)
            .WithMessage("A data da solicitação não pode ser no futuro.")
            .When(x => x.RequestDate.HasValue);

        // Para ReportedProblem: se fornecido, não pode ser vazio e deve respeitar o tamanho.
        RuleFor(x => x.ReportedProblem)
            .NotEmpty().WithMessage("O problema relatado não pode ser vazio se fornecido para atualização.")
            .MaximumLength(255).WithMessage("O problema relatado deve ter no máximo 255 caracteres.")
            .When(x => x.ReportedProblem != null);

        // Para FirstResponseTime: se fornecido, deve ser positivo.
        RuleFor(x => x.FirstResponseTime)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("O tempo da primeira resposta deve ser um valor positivo.")
            .When(x => x.FirstResponseTime.HasValue);

        // Para AverageHandlingTime: se fornecido, deve ser positivo.
        RuleFor(x => x.AverageHandlingTime)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("O tempo médio de atendimento deve ser um valor positivo.")
            .When(x => x.AverageHandlingTime.HasValue);
    }
}