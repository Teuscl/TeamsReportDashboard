using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeAnalysisJobRepository : IAnalysisJobRepository
{
    private readonly List<AnalysisJob> _store = [];

    public IReadOnlyList<AnalysisJob> Store => _store;

    public Task AddAsync(AnalysisJob job)
    {
        _store.Add(job);
        return Task.CompletedTask;
    }

    public void Update(AnalysisJob job)
    {
        var idx = _store.FindIndex(j => j.Id == job.Id);
        if (idx >= 0) _store[idx] = job;
    }

    public Task<AnalysisJob?> GetByIdAsync(Guid id) =>
        Task.FromResult(_store.FirstOrDefault(j => j.Id == id));

    public Task<List<AnalysisJob>> GetPendingJobsAsync(CancellationToken ct = default) =>
        Task.FromResult(_store.Where(j => j.Status == JobStatus.Pending).ToList());

    public Task<IEnumerable<AnalysisJob>> GetAllOrderedByCreationDateAsync(CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<AnalysisJob>>(_store.OrderByDescending(j => j.CreatedAt));

    public Task DeleteAsync(AnalysisJob job)
    {
        _store.Remove(job);
        return Task.CompletedTask;
    }

    public Task UpdateJobsStatusAtomicAsync(IEnumerable<Guid> jobIds, JobStatus status, CancellationToken ct = default)
    {
        foreach (var id in jobIds)
        {
            var job = _store.FirstOrDefault(j => j.Id == id);
            if (job is not null) job.Status = status;
        }
        return Task.CompletedTask;
    }

    public Task<int> ResetStuckProcessingJobsAsync(CancellationToken ct = default)
    {
        var stuck = _store.Where(j => j.Status == JobStatus.Processing).ToList();
        foreach (var j in stuck) j.Status = JobStatus.Pending;
        return Task.FromResult(stuck.Count);
    }
}
