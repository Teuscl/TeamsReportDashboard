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

    public async Task<Requester?> GetRequesterAsync(int id) => 
        await _context.Requesters
            .Include(r => r.Department) 
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task CreateRequesterAsync(Requester requester) => 
        await _context.Requesters.AddAsync(requester);

    public void UpdateRequester(Requester requester)
    {
        if (requester == null) throw new ArgumentNullException(nameof(requester));
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
        // A forma antiga e não traduzível:
        // return await _context.Requesters
        //     .FirstOrDefaultAsync(r => String.Equals(r.Email, email, StringComparison.InvariantCultureIgnoreCase));

        // 👇 A FORMA CORRETA E TRADUZÍVEL 👇
        // Converte tanto o email do banco quanto o email do parâmetro para maiúsculas antes de comparar.
        return await _context.Requesters
            .FirstOrDefaultAsync(r => r.Email.ToUpper() == email.ToUpper());
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