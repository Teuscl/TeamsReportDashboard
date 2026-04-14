using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeSystemPromptRepository : ISystemPromptRepository
{
    private readonly List<SystemPrompt> _store = [];

    public Task<SystemPrompt?> GetLatestAsync() =>
        Task.FromResult(_store.OrderByDescending(p => p.CreatedAt).FirstOrDefault());

    public Task<IReadOnlyList<SystemPrompt>> GetHistoryAsync(int limit = 10) =>
        Task.FromResult<IReadOnlyList<SystemPrompt>>(
            _store.OrderByDescending(p => p.CreatedAt).Take(limit).ToList());

    public Task AddAsync(SystemPrompt prompt)
    {
        _store.Add(prompt);
        return Task.CompletedTask;
    }
}
