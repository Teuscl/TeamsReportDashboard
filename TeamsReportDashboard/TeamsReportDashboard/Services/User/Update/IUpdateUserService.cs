using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.Update;

public interface IUpdateUserService
{
    Task Execute(UpdateUserDto updateUserDto);
}