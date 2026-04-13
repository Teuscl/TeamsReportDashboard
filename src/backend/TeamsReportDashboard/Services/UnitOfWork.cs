using Microsoft.EntityFrameworkCore.Storage; // Adicione esta using
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Backend.Repositories;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction _transaction; // Campo para controlar a transação

        private IUserRepository _userRepository;
        private IReportRepository _reportRepository;
        private IRequesterRepository _requesterRepository;
        private IDepartmentRepository _departmentRepository;
        private IAnalysisJobRepository _analysisJobRepository;

        // ... seu construtor permanece o mesmo ...
        public UnitOfWork(AppDbContext context, IUserRepository userRepository, IReportRepository reportRepository, IRequesterRepository requesterRepository, IDepartmentRepository departmentRepository, IAnalysisJobRepository analysisJobRepository)
        {
            _context = context;
            _userRepository = userRepository;
            _reportRepository = reportRepository;
            _requesterRepository = requesterRepository;
            _departmentRepository = departmentRepository;
            _analysisJobRepository = analysisJobRepository;
        }


        // ... suas propriedades de repositório permanecem as mesmas ...
        public IUserRepository UserRepository => _userRepository;
        public IReportRepository ReportRepository => _reportRepository;
        public IRequesterRepository RequesterRepository => _requesterRepository;
        public IDepartmentRepository DepartmentRepository => _departmentRepository;
        
        public IAnalysisJobRepository AnalysisJobRepository => _analysisJobRepository;


        // Implementação dos novos métodos
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync(); // Salva as mudanças
                await _transaction.CommitAsync(); // Comita a transação
            }
            finally
            {
                await _transaction.DisposeAsync(); // Libera o objeto de transação
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                }
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }
        
        // Este é o seu método CommitAsync original, agora renomeado
        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }

        public void Dispose()
        {
            _transaction?.Dispose(); // Garante que a transação seja descartada se algo der errado
            _context.Dispose();
        }
    }
}