using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){ }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        //Unique on email field
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<Report>()
            .HasIndex(r => r.RequesterEmail)
            .IsUnique();
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Report> Reports { get; set; }
}