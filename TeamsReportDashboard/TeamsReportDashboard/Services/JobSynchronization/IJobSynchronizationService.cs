using TeamsReportDashboard.Backend.Models.ReprocessResponseDto;

namespace TeamsReportDashboard.Backend.Services.JobSynchronization;

public interface IJobSynchronizationService
{
    // Vamos retornar um DTO simples com a mensagem de sucesso
    Task<ReprocessResponseDto> ReprocessJobAsync(Guid jobId);
}