using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeDepartmentRepository : IDepartmentRepository
{
    private readonly List<Department> _departments = [];

    public void Seed(params Department[] departments) => _departments.AddRange(departments);

    public Task<List<Department>> GetAllAsync() =>
        Task.FromResult<List<Department>>([.. _departments]);

    public Task<Department?> GetDepartmentAsync(Guid id) =>
        Task.FromResult(_departments.FirstOrDefault(d => d.Id == id));

    public Task CreateDepartmentAsync(Department department)
    {
        _departments.Add(department);
        return Task.CompletedTask;
    }

    public void UpdateDepartment(Department department)
    {
        var index = _departments.FindIndex(d => d.Id == department.Id);
        if (index >= 0) _departments[index] = department;
    }

    public Task DeleteDepartmentAsync(Guid id)
    {
        _departments.RemoveAll(d => d.Id == id);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync() =>
        Task.FromResult(_departments.Count);
}
