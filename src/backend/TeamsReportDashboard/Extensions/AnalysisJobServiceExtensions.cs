using FluentValidation;
using TeamsReportDashboard.Backend.Models.Job;
using TeamsReportDashboard.Backend.Services.AnalysisJob;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Delete;
using TeamsReportDashboard.Backend.Services.AnalysisJob.JobSynchronization;
using TeamsReportDashboard.Backend.Services.AnalysisJob.ProcessCompletedJob;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Query;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Start;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Update;
using TeamsReportDashboard.Backend.Services.JobSynchronization;
using TeamsReportDashboard.Backend.Services.ProcessCompletedJob;
using TeamsReportDashboard.Backend.Services.Start;

namespace TeamsReportDashboard.Backend.Extensions;

public static class AnalysisJobServiceExtensions
{
    public static IServiceCollection AddAnalysisJobServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<UpdateAnalysisJobDto>, UpdateAnalysisValidator>();
        services.AddScoped<IValidator<StartJobAnalysisDto>, StartAnalysisValidator>();

        services.AddScoped<IReportProcessorService, ReportProcessorService>();
        services.AddScoped<IJobManagementService, JobManagementService>();
        services.AddScoped<IJobResultOrchestrator, JobResultOrchestrator>();
        services.AddScoped<IAnalysisJobQueryService, AnalysisJobQueryService>();
        services.AddScoped<IStartAnalysisService, StartAnalysisService>();
        services.AddScoped<IUpdateAnalysisService, UpdateAnalysisService>();
        services.AddScoped<IDeleteJobService, DeleteJobService>();

        services.AddHostedService<AnalysisJobWorker>();

        return services;
    }
}
