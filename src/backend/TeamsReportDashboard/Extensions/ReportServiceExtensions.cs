using FluentValidation;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Backend.Services.Report.Create;
using TeamsReportDashboard.Backend.Services.Report.Delete;
using TeamsReportDashboard.Backend.Services.Report.Read;
using TeamsReportDashboard.Backend.Services.Report.Update;

namespace TeamsReportDashboard.Backend.Extensions;

public static class ReportServiceExtensions
{
    public static IServiceCollection AddReportServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateReportDto>, CreateReportValidator>();
        services.AddScoped<IValidator<UpdateReportDto>, UpdateReportValidator>();

        services.AddScoped<ICreateReportService, CreateReportService>();
        services.AddScoped<IUpdateReportService, UpdateReportService>();
        services.AddScoped<IGetReportService, GetReportService>();
        services.AddScoped<IDeleteReportService, DeleteReportService>();

        return services;
    }
}
