using TeamsReportDashboard.Data;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Repositories;

namespace TeamsReportDashboard.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IUserRepository _userRepository;

    public UnitOfWork(AppDbContext context,IUserRepository userRepository)
    {
        _context = context;
        _userRepository = userRepository;
    }
    
    public IUserRepository UserRepository => _userRepository ??= new UserRepository(_context);
    
    public async Task<int> CommitAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}