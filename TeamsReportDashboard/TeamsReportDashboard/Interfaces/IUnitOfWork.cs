namespace TeamsReportDashboard.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository UserRepository { get; }
    IReportRepository ReportRepository { get; }
    
    Task<int> CommitAsync();
}