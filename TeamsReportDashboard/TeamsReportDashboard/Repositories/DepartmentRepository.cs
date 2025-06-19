using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Backend.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;

    public DepartmentRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<int> CountAsync()
    {
        return await _context.Requesters.CountAsync();
    }

    public async Task<List<Department>> GetAllAsync() => await _context.Departments.ToListAsync();
   

    public async Task<Department?> GetDepartmentAsync(int id) => await _context.Departments.FirstOrDefaultAsync(x => x.Id == id);
    

    public async Task CreateDepartmentAsync(Department department)
    {
        if (department is null)
            throw new ArgumentNullException(nameof(department));
        await _context.Departments.AddAsync(department);
    }

    public void UpdateDepartment(Department department)
    {
        if (department is null)
            throw new ArgumentNullException(nameof(department));
        _context.Departments.Update(department);
    }

    public async Task DeleteDepartmentAsync(int id)
    {
       var department = await _context.Departments.FirstOrDefaultAsync(x => x.Id == id);
       if( department is not null)
        _context.Departments.Remove(department);
    }
}