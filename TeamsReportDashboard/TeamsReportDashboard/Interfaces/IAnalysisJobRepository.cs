using TeamsReportDashboard.Backend.Entities;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface IAnalysisJobRepository
{
    void Update(AnalysisJob job);
}