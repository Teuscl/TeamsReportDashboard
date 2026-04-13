namespace TeamsReportDashboard.Backend.Services.Department.Delete;

public interface IDeleteDepartmentService
{
    Task Execute(Guid id);
}