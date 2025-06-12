using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Interfaces;
// Para List

// Para Task

namespace TeamsReportDashboard.Backend.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Report>> GetAllAsync() =>
        await _context.Reports
            .Include(r => r.Requester)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Report?> GetReportAsync(int id) =>
        await _context.Reports
            .Include(r => r.Requester)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task CreateReportAsync(Report report)
    {
        if(report == null) throw new ArgumentNullException(nameof(report));
        await _context.Reports.AddAsync(report);
    }

    public void UpdateReport(Report report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        _context.Reports.Update(report);
    }

    public async Task DeleteReportAsync(int id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report != null)
        {
            _context.Reports.Remove(report);
        }
    }
}