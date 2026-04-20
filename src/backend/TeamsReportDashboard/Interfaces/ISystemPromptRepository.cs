using TeamsReportDashboard.Backend.Entities;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface ISystemPromptRepository
{
    Task<SystemPrompt?> GetLatestAsync();
    Task<SystemPrompt?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SystemPrompt>> GetHistoryAsync(int limit = 50);
    Task AddAsync(SystemPrompt prompt);
}
