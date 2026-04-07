using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Services.User.Read;

public interface IGetUsersService
{
    Task<IEnumerable<UserDto>> GetAll();
    Task<UserDto> Get(int id);
}