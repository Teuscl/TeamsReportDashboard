namespace TeamsReportDashboard.Backend.Services.AnalysisJob.JobSynchronization;

public interface IJobResultOrchestrator
{
    Task SyncAndProcessJobResultAsync(Entities.AnalysisJob job);
}