using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.ForgotPassword;

public interface IForgotPasswordService 
{
    public Task Execute(ForgotPasswordDto forgotPasswordDto);
}