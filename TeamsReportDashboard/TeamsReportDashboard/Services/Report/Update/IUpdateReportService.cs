using TeamsReportDashboard.Backend.Models.ReportDto;

namespace TeamsReportDashboard.Backend.Services.Report.Update;

public interface IUpdateReportService
{
    Task Execute(int id, UpdateReportDto updateReportDto);
}