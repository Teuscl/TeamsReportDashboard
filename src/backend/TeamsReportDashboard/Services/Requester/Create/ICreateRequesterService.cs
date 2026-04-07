using TeamsReportDashboard.Backend.Models.Requester;

namespace TeamsReportDashboard.Backend.Services.Requester.Create;

public interface ICreateRequesterService
{
    Task<CreateRequesterDto> Execute(CreateRequesterDto dto);
}