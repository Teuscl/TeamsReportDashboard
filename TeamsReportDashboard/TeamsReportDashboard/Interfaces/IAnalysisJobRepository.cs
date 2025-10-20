using TeamsReportDashboard.Backend.Entities;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface IAnalysisJobRepository
{
    void Update(AnalysisJob job);
    Task AddAsync(AnalysisJob job);
    Task<AnalysisJob?> GetByIdAsync(Guid id);
    Task<List<AnalysisJob>> GetPendingJobsAsync();
    Task<IEnumerable<AnalysisJob>> GetAllOrderedByCreationDateAsync();
    Task DeleteAsync(AnalysisJob job);
}