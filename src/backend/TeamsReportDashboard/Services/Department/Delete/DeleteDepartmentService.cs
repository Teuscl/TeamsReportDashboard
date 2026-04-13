using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Department.Delete;

public class DeleteDepartmentService : IDeleteDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDepartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(Guid id)
    {
        await _unitOfWork.DepartmentRepository.DeleteDepartmentAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }
}