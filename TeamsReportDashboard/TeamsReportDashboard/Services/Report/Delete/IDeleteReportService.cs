namespace TeamsReportDashboard.Services.User.Delete;

public interface IDeleteReportService
{
    Task Execute(int reportId);
}