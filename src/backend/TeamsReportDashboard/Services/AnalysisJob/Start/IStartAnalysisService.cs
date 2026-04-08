using TeamsReportDashboard.Backend.Models.Job;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Start;

public interface IStartAnalysisService
{
    Task<Guid> ExecuteAsync(StartJobAnalysisDto dto);
}