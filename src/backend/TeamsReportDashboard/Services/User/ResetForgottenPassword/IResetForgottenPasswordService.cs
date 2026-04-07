using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.ResetForgottenPassword;

public interface IResetForgottenPasswordService
{
    Task Execute(ResetForgottenPasswordDto forgottenPasswordDto);
}