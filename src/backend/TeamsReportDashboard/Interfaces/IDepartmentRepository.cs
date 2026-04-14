using TeamsReportDashboard.Backend.Entities;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetDepartmentAsync(Guid id);
    Task CreateDepartmentAsync(Department department); 
    void UpdateDepartment(Department department);      
    Task DeleteDepartmentAsync(Guid id);     
    
    Task<int> CountAsync();

}