using TeamsReportDashboard.Backend.Models.PromptDto;

namespace TeamsReportDashboard.Backend.Services.Prompt.Read;

public interface IGetPromptVersionService
{
    Task<PromptVersionDetailDto?> ExecuteAsync(Guid id, CancellationToken ct = default);
}
