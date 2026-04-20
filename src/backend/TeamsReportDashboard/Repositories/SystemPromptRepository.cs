using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Interfaces;

namespace TeamsReportDashboard.Backend.Repositories;

public class SystemPromptRepository : ISystemPromptRepository
{
    private readonly AppDbContext _context;

    public SystemPromptRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SystemPrompt?> GetLatestAsync() =>
        await _context.SystemPrompts
            .Include(p => p.CreatedByUser)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<SystemPrompt?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.SystemPrompts
            .Include(p => p.CreatedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<SystemPrompt>> GetHistoryAsync(int limit = 50) =>
        await _context.SystemPrompts
            .Include(p => p.CreatedByUser)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(SystemPrompt prompt) =>
        await _context.SystemPrompts.AddAsync(prompt);
}
