using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Report.Read;

public class GetReportService : IGetReportService
{
    private IUnitOfWork _unitOfWork;

    public GetReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<IEnumerable<Entities.Report>> GetAll() => await _unitOfWork.ReportRepository.GetAllAsync();

    public async Task<Entities.Report> Get(int id)
    {
        var report = await _unitOfWork.ReportRepository.GetReportAsync(id);
        if (report == null)
            throw new KeyNotFoundException("User not found");
        return report;
    }
    
}