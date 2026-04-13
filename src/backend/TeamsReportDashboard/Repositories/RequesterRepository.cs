using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Backend.Repositories;

public class RequesterRepository : IRequesterRepository
{
    private readonly AppDbContext _context;

    public RequesterRepository(AppDbContext context)
    {
        _context = context;
    }
        
    public async Task<List<Requester>> GetAllAsync() => 
        await _context.Requesters
            .Include(r => r.Department)
            .OrderBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Requester?> GetRequesterAsync(Guid id) => 
        await _context.Requesters
            .Include(r => r.Department) 
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task CreateRequesterAsync(Requester requester) => 
        await _context.Requesters.AddAsync(requester);

    public void UpdateRequester(Requester requester)
    {
        if (requester == null) throw new ArgumentNullException(nameof(requester));
        _context.Requesters.Update(requester);
    }

    public async Task DeleteRequesterAsync(Guid id)
    {
        await _context.Requesters.Where(r => r.Id == id).ExecuteDeleteAsync();
    }

    public async Task<bool> ExistsAsync(Guid id) => 
        await _context.Requesters.AnyAsync(r => r.Id == id);

    
    public async Task<Requester?> GetByEmailAsync(string email)
    {
        return await _context.Requesters
            .FirstOrDefaultAsync(r => EF.Functions.ILike(r.Email, email));
    }
    
    public async Task<int> CountAsync()
    {
        return await _context.Requesters.CountAsync();
    }
    
    public async Task CreateRequesterRangeAsync(IEnumerable<Requester> requesters)
    {
        await _context.Requesters.AddRangeAsync(requesters);
    }
}