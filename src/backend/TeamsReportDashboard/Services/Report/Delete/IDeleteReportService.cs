namespace TeamsReportDashboard.Backend.Services.Report.Delete;

public interface IDeleteReportService
{
    Task Execute(int reportId);
}