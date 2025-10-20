namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Delete;

public interface IDeleteJobService
{
    Task Execute(Guid jobId);
}