using TeamsReportDashboard.Backend.Models.Requester;

namespace TeamsReportDashboard.Backend.Services.Requester.Read;

public interface IGetRequestersService
{
    Task<IEnumerable<RequesterDto>> GetAll();
    Task<RequesterDto?> Get(int id);
}