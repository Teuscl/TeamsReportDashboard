using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    void Update(User user);
    Task DeleteAsync(int id);
    Task<User?> GetByIdAsync(int id); // Retorno pode ser nulo se não encontrado
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByPasswordResetToken(string resetToken);
    Task<bool> ExistsAsync(int id); // Adicionado para validação eficiente
}