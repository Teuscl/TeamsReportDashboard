using TeamsReportDashboard.Backend.Models.PromptDto;

namespace TeamsReportDashboard.Backend.Services.Prompt.Read;

public interface IGetPromptService
{
    Task<PromptResponseDto> ExecuteAsync(CancellationToken ct = default);
}
