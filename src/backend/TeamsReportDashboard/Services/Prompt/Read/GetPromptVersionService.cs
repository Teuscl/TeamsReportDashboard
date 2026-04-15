using TeamsReportDashboard.Backend.Models.PromptDto;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Prompt.Read;

public class GetPromptVersionService : IGetPromptVersionService
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPromptVersionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PromptVersionDetailDto?> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var prompt = await _unitOfWork.SystemPromptRepository.GetByIdAsync(id, ct);
        if (prompt is null) return null;

        return new PromptVersionDetailDto(
            prompt.Id,
            prompt.Content,
            prompt.CreatedAt,
            prompt.CreatedByUser?.Email);
    }
}
