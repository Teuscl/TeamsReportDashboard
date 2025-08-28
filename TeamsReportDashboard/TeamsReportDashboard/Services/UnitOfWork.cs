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

        // ... seu construtor permanece o mesmo ...
        public UnitOfWork(AppDbContext context, IUserRepository userRepository, IReportRepository reportRepository, IRequesterRepository requesterRepository, IDepartmentRepository departmentRepository)
        {
            _context = context;
            _userRepository = userRepository;
            _reportRepository = reportRepository;
            _requesterRepository = requesterRepository;
            _departmentRepository = departmentRepository;
        }


        // ... suas propriedades de repositório permanecem as mesmas ...
        public IUserRepository UserRepository => _userRepository ??= new UserRepository(_context);
        public IReportRepository ReportRepository => _reportRepository ??= new ReportRepository(_context);
        public IRequesterRepository RequesterRepository => _requesterRepository ??= new RequesterRepository(_context);
        public IDepartmentRepository DepartmentRepository => _departmentRepository ??= new DepartmentRepository(_context);


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
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose(); // Garante que a transação seja descartada se algo der errado
            _context.Dispose();
        }
    }
}