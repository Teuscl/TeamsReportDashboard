using TeamsReportDashboard.Backend.Models.ReportDto;

namespace TeamsReportDashboard.Backend.Services.Report.Create;

public interface ICreateReportService
{
     Task<CreateReportDto> Execute(CreateReportDto createReportDto);
}