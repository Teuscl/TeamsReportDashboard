using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
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
}