using TeamsReportDashboard.Backend.Models.PythonApiDto;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.ProcessCompletedJob;

public interface IReportProcessorService
{
    Task ProcessAnalysisResult(Entities.AnalysisJob job, PythonApiDto.PythonResultResponse result);
}