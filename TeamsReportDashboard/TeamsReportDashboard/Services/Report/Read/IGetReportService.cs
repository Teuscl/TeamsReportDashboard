namespace TeamsReportDashboard.Backend.Services.Report.Read;

public interface IGetReportService
{
    Task<IEnumerable<Entities.Report>> GetAll();
    Task<Entities.Report> Get(int id);
}