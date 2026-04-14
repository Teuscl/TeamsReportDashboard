using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.ResetPassword;

public interface IResetPasswordService
{
    public Task Execute(Guid userId, ResetPasswordDto resetPasswordDto);
}