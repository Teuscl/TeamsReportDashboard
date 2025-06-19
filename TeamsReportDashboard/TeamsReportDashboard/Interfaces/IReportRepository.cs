using TeamsReportDashboard.Entities;
using System.Collections.Generic;
using System.Linq.Expressions; // Para List
using System.Threading.Tasks;
using TeamsReportDashboard.Backend.Entities; // Para Task

namespace TeamsReportDashboard.Interfaces;

public interface IReportRepository
{
    Task<List<Report>> GetAllAsync();
    Task<Report?> GetReportAsync(int id);
    Task CreateReportAsync(Report report); // Assinatura corrigida
    void UpdateReport(Report report);      // Assinatura corrigida
    Task DeleteReportAsync(int id);        // Assinatura corrigida
    
    Task<int> CountAsync(Expression<Func<Report, bool>> predicate); 
    
    IQueryable<Report> GetAll(); 
}