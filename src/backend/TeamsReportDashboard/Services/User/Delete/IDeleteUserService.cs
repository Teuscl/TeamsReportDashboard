namespace TeamsReportDashboard.Services.User.Delete;

public interface IDeleteUserService
{
    Task Execute(int userId);
}