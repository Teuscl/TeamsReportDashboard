using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.Create;

public interface ICreateReportService
{
     Task<CreateReportDto> Execute(CreateReportDto createReportDto);
}