namespace TeamsReportDashboard.Backend.Services.Requester.Delete;

public interface IDeleteRequesterService
{
    Task Execute(Guid id);
}