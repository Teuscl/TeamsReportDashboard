using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    void Update(User user);
    Task DeleteAsync(Guid id);
    Task<User?> GetByIdAsync(Guid id); // Retorno pode ser nulo se não encontrado
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByPasswordResetToken(string resetToken);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsWithEmailAsync(string email, Guid excludeId);
}