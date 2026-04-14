using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public FakeUserRepository UserRepo { get; } = new();
    public FakeRequesterRepository RequesterRepo { get; } = new();
    public FakeDepartmentRepository DepartmentRepo { get; } = new();

    public IUserRepository UserRepository => UserRepo;
    public IRequesterRepository RequesterRepository => RequesterRepo;
    public IDepartmentRepository DepartmentRepository => DepartmentRepo;

    public FakeReportRepository ReportRepo { get; } = new();
    public IReportRepository ReportRepository => ReportRepo;

    // Repository not needed for unit tests
    public IAnalysisJobRepository AnalysisJobRepository => throw new NotSupportedException("Not needed in unit tests.");

    public FakeSystemPromptRepository SystemPromptRepo { get; } = new();
    public ISystemPromptRepository SystemPromptRepository => SystemPromptRepo;

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
