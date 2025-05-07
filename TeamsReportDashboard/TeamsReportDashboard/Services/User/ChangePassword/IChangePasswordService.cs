using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.ChangePassword;

public interface IChangePasswordService
{
    public Task Execute(int userId, ChangePasswordDto changePasswordDto);
}