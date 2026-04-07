using TeamsReportDashboard.Backend.Models.Job;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Query;

public interface IAnalysisJobQueryService
{
    
    Task<AnalysisJobDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<AnalysisJobDto>> GetAllAsync();
}