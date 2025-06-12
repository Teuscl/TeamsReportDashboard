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
        await _context.Requesters.AsNoTracking().ToListAsync();

    public async Task<Requester?> GetRequesterAsync(int id) => 
        await _context.Requesters.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    public async Task CreateRequesterAsync(Requester requester) => 
        await _context.Requesters.AddAsync(requester);

    public void UpdateRequester(Requester requester)
    {
        if(requester == null) throw new ArgumentNullException(nameof(requester));
        _context.Requesters.Update(requester);
    }

    public async Task DeleteRequesterAsync(int id)
    {
        var requester = await _context.Requesters.FindAsync(id);
        if (requester != null)
        {
            _context.Requesters.Remove(requester);
        }
    }

    public async Task<bool> ExistsAsync(int id) => 
        await _context.Requesters.AnyAsync(r => r.Id == id);

    public async Task<Requester?> GetByEmailAsync(string email)
    {
        return await _context.Requesters
            .FirstOrDefaultAsync(r => String.Equals(r.Email, email, StringComparison.InvariantCultureIgnoreCase));
    }
}