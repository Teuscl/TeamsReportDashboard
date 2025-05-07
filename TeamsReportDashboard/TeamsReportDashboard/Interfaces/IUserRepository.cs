using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    void Update(User user);
    Task DeleteAsync(int id);
    Task<User> GetByIdAsync(int id);
    Task<User> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
}