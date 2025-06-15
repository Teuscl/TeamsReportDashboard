using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Department.Delete;

public class DeleteDepartmentService : IDeleteDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDepartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(int id)
    {
        await _unitOfWork.DepartmentRepository.DeleteDepartmentAsync(id);
        await _unitOfWork.CommitAsync();
    }
}