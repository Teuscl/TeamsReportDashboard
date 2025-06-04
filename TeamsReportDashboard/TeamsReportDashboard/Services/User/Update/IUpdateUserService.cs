using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Backend.Services.User.Update;

public interface IUpdateUserService
{
    Task Execute(UpdateUserDto updateUserDto);
}