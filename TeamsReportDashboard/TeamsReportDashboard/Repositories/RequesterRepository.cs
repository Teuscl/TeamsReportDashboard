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
    
    public async Task<List<Requester>> GetAllAsync() => await _context.Requesters.ToListAsync();
    

    public async Task<Requester> GetRequesterAsync(int id)
    {
        try
        {
            var requester = await _context.Requesters.FindAsync(id);
            if (requester == null)
                throw new KeyNotFoundException($"Requester with id {id} not found");
            return requester;

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }
    

    public async Task CreateRequesterAsync(Requester requester) => await _context.Requesters.AddAsync(requester);
    

    public async Task<bool> UpdateRequesterAsync(Requester requester)
    {
        if(requester == null)
            throw new ArgumentNullException(nameof(requester));
        _context.Requesters.Update(requester); // Marca toda a entidade como modificada
        // Considere buscar e atualizar propriedades específicas se for mais eficiente
        try
        {
            return await _context.SaveChangesAsync() > 0;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Logar ex, tratar concorrência
            Console.WriteLine($"Error updating report: Concurrency issue. {ex.Message}");
            throw; // Ou retornar false / manipular de outra forma
        }
    }

    public async Task<bool> DeleteRequesterAsync(int id)
    {
        var requester = await _context.Requesters.FindAsync(id);
        if (requester == null)
        {
            return false; // Ou lançar KeyNotFoundException se preferir que o chamador trate
        }
        _context.Requesters.Remove(requester);
        return await _context.SaveChangesAsync() > 0;
    }

}