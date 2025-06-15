using TeamsReportDashboard.Backend.Models.DepartmentDto;

namespace TeamsReportDashboard.Backend.Services.Department.Create;

public interface ICreateDepartmentService
{
    public Task<CreateDepartmentDto> Execute(CreateDepartmentDto createDepartmentDto);
}