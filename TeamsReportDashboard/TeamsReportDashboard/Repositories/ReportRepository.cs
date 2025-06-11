using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Data;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Interfaces;
using System.Collections.Generic; // Para List
using System.Threading.Tasks;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities; // Para Task

namespace TeamsReportDashboard.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Report>> GetAllAsync() =>
        await _context.Reports.AsNoTracking().ToListAsync(); // Adicionado AsNoTracking

    public async Task<Report?> GetReportAsync(int id) 
    {
        return await _context.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Report> CreateReportAsync(Report report)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report));
        await _context.Reports.AddAsync(report);
        await _context.SaveChangesAsync();
        return report; // Retorna a entidade criada (com ID preenchido)
    }

    public async Task<bool> UpdateReportAsync(Report report)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report));

        // Opcional: verificar se o relatório existe primeiro se o 'report' pode ser uma nova instância não rastreada
        // var existingReport = await _context.Reports.FindAsync(report.Id);
        // if (existingReport == null) return false; // ou lançar exceção

        // Definir UpdatedAt
        // report.UpdatedAt = DateTime.UtcNow; // Se o 'report' é uma entidade já rastreada
        // _context.Entry(existingReport).CurrentValues.SetValues(report); // Se 'report' é DTO ou não rastreado
        // existingReport.UpdatedAt = DateTime.UtcNow;

        _context.Reports.Update(report); // Marca toda a entidade como modificada
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
        // Outras DbUpdateException podem ocorrer
    }

    public async Task<bool> DeleteReportAsync(int id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null)
        {
            return false; // Ou lançar KeyNotFoundException se preferir que o chamador trate
        }
        _context.Reports.Remove(report);
        return await _context.SaveChangesAsync() > 0;
    }
}