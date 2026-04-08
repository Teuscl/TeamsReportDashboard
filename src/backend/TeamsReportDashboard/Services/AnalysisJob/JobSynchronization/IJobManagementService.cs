using TeamsReportDashboard.Backend.Models.ReprocessResponseDto;

namespace TeamsReportDashboard.Backend.Services.JobSynchronization;

public interface IJobManagementService
{
    Task<ReprocessResponseDto> ReprocessJobAsync(Guid jobId);
}