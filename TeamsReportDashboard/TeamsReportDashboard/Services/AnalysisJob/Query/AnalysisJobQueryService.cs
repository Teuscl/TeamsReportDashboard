using TeamsReportDashboard.Backend.Models.Job;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Query;

public class AnalysisJobQueryService : IAnalysisJobQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public AnalysisJobQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    public async Task<AnalysisJobDto?> GetByIdAsync(Guid id)
    {
        var job = await _unitOfWork.AnalysisJobRepository.GetByIdAsync(id);
        if (job == null) return null;

        // ✅ CORRIGIDO: Mapeia a entidade para o DTO
        return new AnalysisJobDto
        {
            Id = job.Id,
            Name = job.Name,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            ErrorMessage = job.ErrorMessage
        };
    }

    public async Task<IEnumerable<AnalysisJobDto>> GetAllAsync()
    {
        var jobs = await _unitOfWork.AnalysisJobRepository.GetAllOrderedByCreationDateAsync();

        // ✅ CORRIGIDO: Mapeia a lista de entidades para uma lista de DTOs
        return jobs.Select(job => new AnalysisJobDto
        {
            Id = job.Id,
            Name = job.Name,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            ErrorMessage = job.ErrorMessage
        });
    }
}
