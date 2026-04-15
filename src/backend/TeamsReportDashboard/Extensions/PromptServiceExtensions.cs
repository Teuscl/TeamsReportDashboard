using FluentValidation;
using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Backend.Models.PromptDto;
using TeamsReportDashboard.Backend.Repositories;
using TeamsReportDashboard.Backend.Services.Prompt.Read;
using TeamsReportDashboard.Backend.Services.Prompt.Update;

namespace TeamsReportDashboard.Backend.Extensions;

public static class PromptServiceExtensions
{
    public static IServiceCollection AddPromptServices(this IServiceCollection services)
    {
        services.AddScoped<ISystemPromptRepository, SystemPromptRepository>();

        services.AddScoped<IValidator<PromptDto>, UpdatePromptValidator>();

        services.AddScoped<IGetPromptService, GetPromptService>();
        services.AddScoped<IGetPromptVersionService, GetPromptVersionService>();
        services.AddScoped<IUpdatePromptService, UpdatePromptService>();

        return services;
    }
}
