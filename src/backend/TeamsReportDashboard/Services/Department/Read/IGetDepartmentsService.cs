using TeamsReportDashboard.Backend.Models.DepartmentDto;

namespace TeamsReportDashboard.Backend.Services.Department.Read;

public interface IGetDepartmentsService
{
    Task<IEnumerable<DepartmentResponseDto>> GetDepartmentsAsync();
    Task<DepartmentResponseDto> Get(int id);
}