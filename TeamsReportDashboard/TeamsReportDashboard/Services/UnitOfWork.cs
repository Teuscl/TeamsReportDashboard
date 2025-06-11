using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Data;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Repositories;

namespace TeamsReportDashboard.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IUserRepository _userRepository;
    private IReportRepository _reportRepository;

    public UnitOfWork(AppDbContext context,IUserRepository userRepository, IReportRepository reportRepository)
    {
        _context = context;
        _userRepository = userRepository;
        _reportRepository = reportRepository;
    }
    
    public IUserRepository UserRepository => _userRepository ??= new UserRepository(_context);
    public IReportRepository ReportRepository => _reportRepository ??= new ReportRepository(_context);

    public async Task<int> CommitAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}