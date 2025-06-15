namespace TeamsReportDashboard.Backend.Services.Department.Read;

public interface IGetDepartmentsService
{
    public Task<IEnumerable<Entities.Department>> GetDepartmentsAsync();
    public Task<Entities.Department> Get(int id);
}