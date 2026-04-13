using TeamsReportDashboard.Backend.Models.DepartmentDto;

namespace TeamsReportDashboard.Backend.Services.Department.Update;

public interface IUpdateDepartmentService
{
    Task Execute(Guid id, UpdateDepartmentDto departmentDto);
}