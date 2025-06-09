using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Data;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);
            if (user is null)
            {
                throw new KeyNotFoundException("User not found!");
            }
            _context.Users.Remove(user);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    public async Task<User> GetByIdAsync(int id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);
            if (user is null)
                throw new KeyNotFoundException("User not found!");
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
    public async Task<User?> GetByEmailAsync(string email) => await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    public async Task<IEnumerable<User>> GetAllAsync() => await _context.Users.ToListAsync();
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken) => await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    
    public async Task<User?> GetByPasswordResetToken(string resetToken) => await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == resetToken);

    
}