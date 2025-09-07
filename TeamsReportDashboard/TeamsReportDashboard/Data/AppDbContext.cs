using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Backend.Data;

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
        modelBuilder.Entity<Requester>()
            .HasIndex(r => r.Email)
            .IsUnique();

        // Relação: Department -> Requester (Um Departamento tem muitos Solicitantes)
        modelBuilder.Entity<Department>()
            .HasMany(d => d.Requesters)
            .WithOne(r => r.Department)
            .HasForeignKey(r => r.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull); // Se um depto for deletado, o depto do funcionário vira null

        // Relação: Report -> Requester (Um Relatório tem um Solicitante)
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Requester)
            .WithMany() // Um solicitante pode ter muitos relatórios
            .HasForeignKey(r => r.RequesterId)
            .OnDelete(DeleteBehavior.Restrict); // Impede que um solicitante seja deletado se ele tiver relatórios associados
        
        modelBuilder.Entity<Report>()
            .Property(r => r.FirstResponseTime)
            .HasConversion(
                timespan => timespan.Ticks,
                ticks => TimeSpan.FromTicks(ticks)
            );
        
        modelBuilder.Entity<Report>()
            .Property(r => r.AverageHandlingTime)
            .HasConversion(
                timespan => timespan.Ticks,
                ticks => TimeSpan.FromTicks(ticks)
            );

    }

    public DbSet<User> Users { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Department> Departments { get; set; } 
    public DbSet<Requester> Requesters { get; set; } 
    
    public DbSet<AnalysisJob> AnalysisJobs { get; set; }
}