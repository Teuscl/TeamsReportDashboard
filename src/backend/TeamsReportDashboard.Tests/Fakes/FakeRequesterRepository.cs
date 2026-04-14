using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeRequesterRepository : IRequesterRepository
{
    private readonly List<Requester> _requesters = [];
    public int CreateRangeCallCount { get; private set; }

    public void Seed(params Requester[] requesters) => _requesters.AddRange(requesters);

    public Task<List<Requester>> GetAllAsync() =>
        Task.FromResult<List<Requester>>([.. _requesters]);

    public Task<Requester?> GetRequesterAsync(Guid id) =>
        Task.FromResult(_requesters.FirstOrDefault(r => r.Id == id));

    public Task CreateRequesterAsync(Requester requester)
    {
        _requesters.Add(requester);
        return Task.CompletedTask;
    }

    public void UpdateRequester(Requester requester)
    {
        var index = _requesters.FindIndex(r => r.Id == requester.Id);
        if (index >= 0) _requesters[index] = requester;
    }

    public Task DeleteRequesterAsync(Guid id)
    {
        _requesters.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id) =>
        Task.FromResult(_requesters.Any(r => r.Id == id));

    public Task<Requester?> GetByEmailAsync(string email) =>
        Task.FromResult(_requesters.FirstOrDefault(r => r.Email == email));

    public Task<int> CountAsync() =>
        Task.FromResult(_requesters.Count);

    public Task CreateRequesterRangeAsync(IEnumerable<Requester> requesters)
    {
        CreateRangeCallCount++;
        _requesters.AddRange(requesters);
        return Task.CompletedTask;
    }
}
