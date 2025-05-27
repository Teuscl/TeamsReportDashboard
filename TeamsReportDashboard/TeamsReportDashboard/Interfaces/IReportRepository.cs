using TeamsReportDashboard.Entities;
using System.Collections.Generic; // Para List
using System.Threading.Tasks; // Para Task

namespace TeamsReportDashboard.Interfaces;

public interface IReportRepository
{
    Task<List<Report>> GetReportsAsync();
    Task<Report> GetReportAsync(int id);
    Task<Report> CreateReportAsync(Report report);
    Task<bool> UpdateReportAsync(Report report);
    Task<bool> DeleteReportAsync(int id);
}