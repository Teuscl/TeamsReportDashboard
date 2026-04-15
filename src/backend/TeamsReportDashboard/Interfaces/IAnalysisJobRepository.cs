using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Entities.Enums;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface IAnalysisJobRepository
{
    void Update(AnalysisJob job);
    Task AddAsync(AnalysisJob job);
    Task<AnalysisJob?> GetByIdAsync(Guid id);
    Task<List<AnalysisJob>> GetPendingJobsAsync(CancellationToken ct = default);
    Task<IEnumerable<AnalysisJob>> GetAllOrderedByCreationDateAsync(CancellationToken ct = default);
    Task DeleteAsync(AnalysisJob job);
    Task UpdateJobsStatusAtomicAsync(IEnumerable<Guid> jobIds, JobStatus status, CancellationToken ct = default);

    /// <summary>
    /// Resets all jobs stuck in Processing back to Pending.
    /// Called on worker startup to recover from crashes or ungraceful shutdowns.
    /// Returns the number of jobs reset.
    /// </summary>
    Task<int> ResetStuckProcessingJobsAsync(CancellationToken ct = default);
}