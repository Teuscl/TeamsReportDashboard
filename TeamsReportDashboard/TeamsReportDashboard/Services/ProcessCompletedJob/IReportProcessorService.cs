using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.PythonApiDto;

namespace TeamsReportDashboard.Backend.Services.ProcessCompletedJob;

public interface IReportProcessorService
{
    Task ProcessAnalysisResult(AnalysisJob job, PythonApiDto.PythonResultResponse result);
}