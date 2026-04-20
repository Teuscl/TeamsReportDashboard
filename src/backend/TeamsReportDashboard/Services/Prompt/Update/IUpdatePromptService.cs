using TeamsReportDashboard.Backend.Models.PromptDto;

namespace TeamsReportDashboard.Backend.Services.Prompt.Update;

public interface IUpdatePromptService
{
    Task ExecuteAsync(PromptDto request, Guid updatedByUserId, CancellationToken ct = default);
}
