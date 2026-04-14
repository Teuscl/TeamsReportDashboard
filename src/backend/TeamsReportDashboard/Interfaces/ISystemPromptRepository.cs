using TeamsReportDashboard.Backend.Entities;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface ISystemPromptRepository
{
    Task<SystemPrompt?> GetLatestAsync();
    Task<IReadOnlyList<SystemPrompt>> GetHistoryAsync(int limit = 10);
    Task AddAsync(SystemPrompt prompt);
}
