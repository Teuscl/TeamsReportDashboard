using System.Linq.Expressions;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeReportRepository : IReportRepository
{
    private readonly List<Report> _reports = [];
    public int CreateCallCount { get; private set; }
    public int UpdateCallCount { get; private set; }

    public void Seed(params Report[] reports) => _reports.AddRange(reports);

    public Task<List<Report>> GetAllAsync() =>
        Task.FromResult<List<Report>>([.. _reports]);

    public Task<Report?> GetReportAsync(Guid id) =>
        Task.FromResult(_reports.FirstOrDefault(r => r.Id == id));

    public Task CreateReportAsync(Report report)
    {
        CreateCallCount++;
        _reports.Add(report);
        return Task.CompletedTask;
    }

    public void UpdateReport(Report report)
    {
        UpdateCallCount++;
        var index = _reports.FindIndex(r => r.Id == report.Id);
        if (index >= 0) _reports[index] = report;
    }

    public Task DeleteReportAsync(Guid id)
    {
        _reports.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(Expression<Func<Report, bool>> predicate) =>
        Task.FromResult(_reports.Count(predicate.Compile()));

    public IQueryable<Report> GetAll() => _reports.AsQueryable();

    public Task<bool> HasReportsForRequesterAsync(Guid requesterId) =>
        Task.FromResult(_reports.Any(r => r.RequesterId == requesterId));

    public Task DeleteByJobIdAsync(Guid jobId)
    {
        _reports.RemoveAll(r => r.AnalysisJobId == jobId);
        return Task.CompletedTask;
    }
}
