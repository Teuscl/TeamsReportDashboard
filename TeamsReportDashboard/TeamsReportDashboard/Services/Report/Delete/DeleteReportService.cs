using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Services.User.Delete;

public class DeleteReportService : IDeleteReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task Execute(int id)
    {
        await _unitOfWork.ReportRepository.DeleteReportAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }
}