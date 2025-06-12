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

    public async Task DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
        }
    }

    public async Task<User?> GetByIdAsync(int id) => 
        await _context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id);

    public async Task<User?> GetByEmailAsync(string email) => 
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

    public async Task<IEnumerable<User>> GetAllAsync() => 
        await _context.Users.AsNoTracking().ToListAsync();

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken) => 
        await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        
    public async Task<User?> GetByPasswordResetToken(string resetToken) => 
        await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == resetToken);

    public async Task<bool> ExistsAsync(int id) => 
        await _context.Users.AnyAsync(u => u.Id == id);
}