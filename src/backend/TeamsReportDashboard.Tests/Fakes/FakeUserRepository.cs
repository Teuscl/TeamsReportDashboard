using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];
    public int UpdateCallCount { get; private set; }

    public void Seed(params User[] users) => _users.AddRange(users);

    public Task AddAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public void Update(User user)
    {
        UpdateCallCount++;
        var index = _users.FindIndex(u => u.Id == user.Id);
        if (index >= 0) _users[index] = user;
    }

    public Task DeleteAsync(Guid id)
    {
        _users.RemoveAll(u => u.Id == id);
        return Task.CompletedTask;
    }

    public Task<User?> GetByIdAsync(Guid id) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email == email && u.IsActive));

    public Task<IEnumerable<User>> GetAllAsync() =>
        Task.FromResult<IEnumerable<User>>([.. _users]);

    public Task<User?> GetByRefreshTokenAsync(string refreshToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.RefreshToken == refreshToken && u.IsActive));

    public Task<User?> GetByPasswordResetToken(string resetToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.PasswordResetToken == resetToken && u.IsActive));

    public Task<bool> ExistsAsync(Guid id) =>
        Task.FromResult(_users.Any(u => u.Id == id));

    public Task<bool> ExistsWithEmailAsync(string email, Guid excludeId) =>
        Task.FromResult(_users.Any(u => u.Email == email && u.Id != excludeId));
}
