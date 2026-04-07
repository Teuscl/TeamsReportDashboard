using FluentValidation;
using TeamsReportDashboard.Backend.Models.Requester;
using TeamsReportDashboard.Backend.Services.Requester.BulkCreate;
using TeamsReportDashboard.Backend.Services.Requester.Create;
using TeamsReportDashboard.Backend.Services.Requester.Delete;
using TeamsReportDashboard.Backend.Services.Requester.Read;
using TeamsReportDashboard.Backend.Services.Requester.Update;

namespace TeamsReportDashboard.Backend.Extensions;

public static class RequesterServiceExtensions
{
    public static IServiceCollection AddRequesterServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateRequesterDto>, CreateRequesterValidator>();
        services.AddScoped<IValidator<UpdateRequesterDto>, UpdateRequesterValidator>();

        services.AddScoped<IGetRequestersService, GetRequestersService>();
        services.AddScoped<ICreateRequesterService, CreateRequesterService>();
        services.AddScoped<IUpdateRequesterService, UpdateRequesterService>();
        services.AddScoped<IDeleteRequesterService, DeleteRequesterService>();
        services.AddScoped<IBulkCreateRequesterService, BulkCreateRequesterService>();

        return services;
    }
}
