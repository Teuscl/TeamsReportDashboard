using TeamsReportDashboard.Backend.Models.ReportDto;

namespace TeamsReportDashboard.Backend.Services.Report.Read;

public interface IGetReportService
{
    Task<IEnumerable<ReportDto>> GetAll();
    Task<ReportDto> Get(int id);
}