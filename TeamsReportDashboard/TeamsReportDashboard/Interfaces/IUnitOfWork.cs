using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository UserRepository { get; }
    IReportRepository ReportRepository { get; }
    IRequesterRepository RequesterRepository { get; }
    IDepartmentRepository DepartmentRepository { get; }
    
    Task<int> CommitAsync();
}