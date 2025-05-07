namespace TeamsReportDashboard.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository UserRepository { get; }
    
    Task<int> CommitAsync();
}