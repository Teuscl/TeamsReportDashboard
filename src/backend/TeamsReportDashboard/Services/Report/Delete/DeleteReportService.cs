using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Report.Delete;

public class DeleteReportService : IDeleteReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task Execute(Guid id)
    {
        await _unitOfWork.ReportRepository.DeleteReportAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }
}