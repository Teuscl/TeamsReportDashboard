using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user) => await _context.Users.AddAsync(user);

    public void Update(User user) => _context.Users.Update(user);

    public async Task DeleteAsync(Guid id)
    {
        await _context.Users.Where(u => u.Id == id).ExecuteDeleteAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id) => 
        await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

    public async Task<User?> GetByEmailAsync(string email) => 
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

    public async Task<IEnumerable<User>> GetAllAsync() => 
        await _context.Users.AsNoTracking().ToListAsync();

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken) =>
        await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.IsActive);

    public async Task<User?> GetByPasswordResetToken(string resetToken) =>
        await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == resetToken && u.IsActive);

    public async Task<bool> ExistsAsync(Guid id) =>
        await _context.Users.AnyAsync(u => u.Id == id);

    public async Task<bool> ExistsWithEmailAsync(string email, Guid excludeId) =>
        await _context.Users.AnyAsync(u => u.Email == email && u.Id != excludeId);
}