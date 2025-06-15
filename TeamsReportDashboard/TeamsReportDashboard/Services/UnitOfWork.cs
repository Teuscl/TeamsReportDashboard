using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Backend.Repositories;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IUserRepository _userRepository;
    private IReportRepository _reportRepository;
    private IRequesterRepository _requesterRepository;
    private IDepartmentRepository _departmentRepository;

    public UnitOfWork(AppDbContext context,IUserRepository userRepository, IReportRepository reportRepository, IRequesterRepository requesterRepository, IDepartmentRepository departmentRepository)
    {
        _context = context;
        _userRepository = userRepository;
        _reportRepository = reportRepository;
        _requesterRepository = requesterRepository;
        _departmentRepository = departmentRepository;
    }
    
    public IUserRepository UserRepository => _userRepository ??= new UserRepository(_context);
    public IReportRepository ReportRepository => _reportRepository ??= new ReportRepository(_context);
    
    public IRequesterRepository RequesterRepository => _requesterRepository ??= new RequesterRepository(_context);
    public IDepartmentRepository DepartmentRepository => _departmentRepository ??= new DepartmentRepository(_context);

    public async Task<int> CommitAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}