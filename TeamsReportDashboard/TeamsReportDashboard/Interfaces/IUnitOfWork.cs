using Microsoft.EntityFrameworkCore.Storage; // Adicione esta using
using System.Threading.Tasks;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository UserRepository { get; }
        IReportRepository ReportRepository { get; }
        IRequesterRepository RequesterRepository { get; }
        IDepartmentRepository DepartmentRepository { get; }
        
        IAnalysisJobRepository AnalysisJobRepository { get; }

        // Métodos para transação
        Task BeginTransactionAsync();
        Task CommitAsync(); // Renomeado para clareza
        Task RollbackAsync();
        
        // Seu método CommitAsync original agora é para salvar mudanças sem transação explícita
        Task<int> SaveChangesAsync();
    }
}