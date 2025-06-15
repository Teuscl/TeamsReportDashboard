using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Department.Read;

public class GetDepartmentsService : IGetDepartmentsService
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDepartmentsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<IEnumerable<Entities.Department>> GetDepartmentsAsync()
    {
        return await _unitOfWork.DepartmentRepository.GetAllAsync();
        
    }

    public async Task<Entities.Department> Get(int id)
    {
        var dep = await _unitOfWork.DepartmentRepository.GetDepartmentAsync(id);
        return dep;
    }
}