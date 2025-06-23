using TeamsReportDashboard.Backend.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Requester.Delete;

public class DeleteRequesterService : IDeleteRequesterService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRequesterService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(int id)
    {
        var requester = await _unitOfWork.RequesterRepository.GetRequesterAsync(id);
        if (requester == null)
        {
            throw new KeyNotFoundException("Solicitante não encontrado.");
        }
        
        var hasReports = await _unitOfWork.ReportRepository.HasReportsForRequesterAsync(id);
        if (hasReports)
        {
            // Lança a sua nova exceção customizada
            throw new ConflictException("Este solicitante não pode ser excluído pois possui relatórios associados.");
        }


        await _unitOfWork.RequesterRepository.DeleteRequesterAsync(id);
        await _unitOfWork.CommitAsync();
    }
}