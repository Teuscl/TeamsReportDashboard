using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Report.Read;

public class GetReportService : IGetReportService
{
    private IUnitOfWork _unitOfWork;

    public GetReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<IEnumerable<ReportDto>> GetAll()
    {
        var reports = await _unitOfWork.ReportRepository.GetAllAsync();
        
        // Mapeia a Entidade para o DTO
        return reports.Select(report => new ReportDto
        {
            Id = report.Id,
            RequesterId = report.RequesterId,
            RequesterName = report.Requester?.Name, // Pega o nome do objeto aninhado
            RequesterEmail = report.Requester?.Email, // Pega o email do objeto aninhado
            TechnicianName = report.TechnicianName,
            RequestDate = report.RequestDate,
            ReportedProblem = report.ReportedProblem,
            Category = report.Category,
            FirstResponseTime = report.FirstResponseTime,
            AverageHandlingTime = report.AverageHandlingTime
        });
    }

    public async Task<ReportDto> Get(int id)
    {
        var report = await _unitOfWork.ReportRepository.GetReportAsync(id);
        if (report == null)
            throw new KeyNotFoundException("User not found");
        return new ReportDto
        {
            Id = report.Id,
            RequesterId = report.RequesterId,
            RequesterName = report.Requester?.Name, // Pega o nome do objeto aninhado
            RequesterEmail = report.Requester?.Email, // Pega o email do objeto aninhado
            TechnicianName = report.TechnicianName,
            RequestDate = report.RequestDate,
            ReportedProblem = report.ReportedProblem,
            Category = report.Category,
            FirstResponseTime = report.FirstResponseTime,
            AverageHandlingTime = report.AverageHandlingTime
        };
    }
    
}