using FluentValidation;
using TeamsReportDashboard.Backend.Models.PromptDto;

namespace TeamsReportDashboard.Backend.Services.Prompt.Update;

public class UpdatePromptValidator : AbstractValidator<PromptDto>
{
    public UpdatePromptValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("O conteúdo do prompt não pode estar vazio.")
            .MinimumLength(20).WithMessage("O prompt deve ter pelo menos 20 caracteres.")
            .MaximumLength(50_000).WithMessage("O prompt não pode exceder 50.000 caracteres.");
    }
}
