using TeamsReportDashboard.Backend.Models.Job;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Update;

public interface IUpdateAnalysisService
{
    Task ExecuteAsync(Guid jobId, UpdateAnalysisJobDto dto);
}