using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Delete;

public class DeleteJobService :  IDeleteJobService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteJobService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task Execute(Guid jobId)
    {
        var jobToDelete = await _unitOfWork.AnalysisJobRepository.GetByIdAsync(jobId);

        if (jobToDelete == null)
        {
            throw new KeyNotFoundException($"Job {jobId} not found");
        }
        await _unitOfWork.AnalysisJobRepository.DeleteAsync(jobToDelete);
        await _unitOfWork.SaveChangesAsync();
    }
}