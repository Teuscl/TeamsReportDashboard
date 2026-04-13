using TeamsReportDashboard.Backend.Models.Requester;

namespace TeamsReportDashboard.Backend.Services.Requester.Update;

public interface IUpdateRequesterService
{
    Task Execute(Guid id, UpdateRequesterDto dto);
}