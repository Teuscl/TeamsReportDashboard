using TeamsReportDashboard.Backend.Models.DepartmentDto;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Department.Read;

public class GetDepartmentsService : IGetDepartmentsService
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDepartmentsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DepartmentResponseDto>> GetDepartmentsAsync()
    {
        var departments = await _unitOfWork.DepartmentRepository.GetAllAsync();
        return departments.Select(d => new DepartmentResponseDto(d.Id, d.Name, d.CreatedAt));
    }

    public async Task<DepartmentResponseDto> Get(Guid id)
    {
        var dep = await _unitOfWork.DepartmentRepository.GetDepartmentAsync(id);
        if (dep == null)
            throw new KeyNotFoundException($"Department with id {id} not found");
        return new DepartmentResponseDto(dep.Id, dep.Name, dep.CreatedAt);
    }
}