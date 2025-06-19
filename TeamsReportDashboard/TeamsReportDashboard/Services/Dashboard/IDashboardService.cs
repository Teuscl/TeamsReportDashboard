using TeamsReportDashboard.Backend.Models.Dashboard;

namespace TeamsReportDashboard.Backend.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardDataAsync();
}