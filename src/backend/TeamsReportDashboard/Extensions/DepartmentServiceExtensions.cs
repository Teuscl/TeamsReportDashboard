using FluentValidation;
using TeamsReportDashboard.Backend.Models.DepartmentDto;
using TeamsReportDashboard.Backend.Services.Department.Create;
using TeamsReportDashboard.Backend.Services.Department.Delete;
using TeamsReportDashboard.Backend.Services.Department.Read;
using TeamsReportDashboard.Backend.Services.Department.Update;

namespace TeamsReportDashboard.Backend.Extensions;

public static class DepartmentServiceExtensions
{
    public static IServiceCollection AddDepartmentServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateDepartmentDto>, CreateDepartmentValidator>();
        services.AddScoped<IValidator<UpdateDepartmentDto>, UpdateDepartmentValidator>();

        services.AddScoped<IGetDepartmentsService, GetDepartmentsService>();
        services.AddScoped<ICreateDepartmentService, CreateDepartmentService>();
        services.AddScoped<IDeleteDepartmentService, DeleteDepartmentService>();
        services.AddScoped<IUpdateDepartmentService, UpdateDepartmentService>();

        return services;
    }
}
