namespace TeamsReportDashboard.Backend.Services.Requester.Read;

public interface IGetRequestersService
{
    Task<IEnumerable<Entities.Requester>> GetAll();
    Task<Entities.Requester?> Get(int id);
}