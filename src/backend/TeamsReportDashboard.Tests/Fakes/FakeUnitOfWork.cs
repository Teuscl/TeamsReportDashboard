using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public FakeUserRepository UserRepo { get; } = new();
    public IUserRepository UserRepository => UserRepo;

    // Other repositories not needed for auth tests
    public IReportRepository ReportRepository => throw new NotSupportedException("Not needed in auth tests.");
    public IRequesterRepository RequesterRepository => throw new NotSupportedException("Not needed in auth tests.");
    public IDepartmentRepository DepartmentRepository => throw new NotSupportedException("Not needed in auth tests.");
    public IAnalysisJobRepository AnalysisJobRepository => throw new NotSupportedException("Not needed in auth tests.");

    public int SaveChangesCallCount { get; private set; }

    public Task BeginTransactionAsync() => Task.CompletedTask;
    public Task CommitAsync() => Task.CompletedTask;
    public Task RollbackAsync() => Task.CompletedTask;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }

    public void Dispose() { }
}
