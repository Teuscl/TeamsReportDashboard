using TeamsReportDashboard.Backend.Entities;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetDepartmentAsync(int id);
    Task CreateDepartmentAsync(Department department); 
    void UpdateDepartment(Department department);      
    Task DeleteDepartmentAsync(int id);        
}