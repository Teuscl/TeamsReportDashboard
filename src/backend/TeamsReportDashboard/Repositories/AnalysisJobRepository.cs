using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Backend.Repositories;

public class AnalysisJobRepository  : IAnalysisJobRepository
{
    private readonly AppDbContext _context;

    public AnalysisJobRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public void Update(AnalysisJob job)
    {
        _context.AnalysisJobs.Update(job);
    }

    public Task AddAsync(AnalysisJob job)
    {
        _context.AnalysisJobs.Add(job);
        return Task.CompletedTask;
    }

    public async Task<AnalysisJob?> GetByIdAsync(Guid id)
    {
        return await _context.AnalysisJobs.FindAsync(id);
    }

    public async Task<List<AnalysisJob>> GetPendingJobsAsync(CancellationToken ct = default)
    {
        return await _context.AnalysisJobs
            .Where(j => j.Status == JobStatus.Pending)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AnalysisJob>> GetAllOrderedByCreationDateAsync(CancellationToken ct = default) =>
        await _context.AnalysisJobs
            .AsNoTracking()
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public Task DeleteAsync(AnalysisJob job)
    {
        _context.AnalysisJobs.Remove(job);
        return Task.CompletedTask;
    }
}