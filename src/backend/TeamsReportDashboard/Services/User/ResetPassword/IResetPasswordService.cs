using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.ResetPassword;

public interface IResetPasswordService
{
    public Task Execute(int userId, ResetPasswordDto resetPasswordDto);
}