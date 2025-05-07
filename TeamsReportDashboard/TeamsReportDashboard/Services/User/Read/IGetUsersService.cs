namespace TeamsReportDashboard.Services.User.Read;

public interface IGetUsersService
{
    Task<IEnumerable<Entities.User>> GetAll();
    Task<Entities.User> Get(int id);
}