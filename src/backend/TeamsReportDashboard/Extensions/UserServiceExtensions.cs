using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;
using TeamsReportDashboard.Backend.Services.User.ForgotPassword;
using TeamsReportDashboard.Backend.Services.User.ResetForgottenPassword;
using TeamsReportDashboard.Backend.Services.User.ResetPassword;
using TeamsReportDashboard.Backend.Services.User.Update;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;
using TeamsReportDashboard.Services;
using TeamsReportDashboard.Services.User.Create;
using TeamsReportDashboard.Services.User.Delete;
using TeamsReportDashboard.Services.User.Read;
using TeamsReportDashboard.Services.User.Update;

namespace TeamsReportDashboard.Backend.Extensions;

public static class UserServiceExtensions
{
    public static IServiceCollection AddUserServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateUserDto>, CreateUserValidator>();
        services.AddScoped<IValidator<UpdateUserDto>, UpdateUserValidator>();
        services.AddScoped<IValidator<ChangeMyPasswordDto>, ChangeMyPasswordValidator>();
        services.AddScoped<IValidator<ResetPasswordDto>, ResetPasswordValidator>();
        services.AddScoped<IValidator<ForgotPasswordDto>, ForgotPasswordValidator>();
        services.AddScoped<IValidator<ResetForgottenPasswordDto>, ResetForgottenPasswordValidator>();

        services.AddScoped<ICreateUserService, CreateUserService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IDeleteUserService, DeleteUserService>();
        services.AddScoped<IGetUsersService, GetUsersService>();
        services.AddScoped<IUpdateUserService, UpdateUserService>();
        services.AddScoped<IChangeMyPasswordService, ChangeMyPasswordService>();
        services.AddScoped<IResetPasswordService, ResetPasswordService>();
        services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
        services.AddScoped<IResetForgottenPasswordService, ResetForgottenPasswordService>();

        return services;
    }
}
