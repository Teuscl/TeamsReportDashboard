using TeamsReportDashboard.Entities;
using System.Collections.Generic;
using System.Linq.Expressions; // Para List
using System.Threading.Tasks;
using TeamsReportDashboard.Backend.Entities; // Para Task

namespace TeamsReportDashboard.Interfaces;

public interface IReportRepository
{
    Task<List<Report>> GetAllAsync();
    Task<Report?> GetReportAsync(Guid id);
    Task CreateReportAsync(Report report); // Assinatura corrigida
    void UpdateReport(Report report);      // Assinatura corrigida
    Task DeleteReportAsync(Guid id);        // Assinatura corrigida
    
    Task<int> CountAsync(Expression<Func<Report, bool>> predicate); 
    
    IQueryable<Report> GetAll(); 
    
    Task<bool> HasReportsForRequesterAsync(Guid requesterId);

    Task DeleteByJobIdAsync(Guid jobId);
}