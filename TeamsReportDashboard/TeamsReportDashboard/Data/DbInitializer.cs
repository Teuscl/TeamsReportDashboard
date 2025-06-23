using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums; // Para a entidade User
using TeamsReportDashboard.Interfaces; // Para IPasswordService
using TeamsReportDashboard.Entities.Enums; // Supondo que você tenha um enum para Roles

namespace TeamsReportDashboard.Backend.Data;

public static class DbInitializer
{
    public static async Task SeedMasterUser(IApplicationBuilder app)
    {
        // Usamos um 'scope' para obter os serviços que precisamos
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                var passwordService = services.GetRequiredService<IPasswordService>();
                var configuration = services.GetRequiredService<IConfiguration>();

                // 1. Verifica se já existe algum usuário no banco
                if (await context.Users.AnyAsync())
                {
                    // Se já existe, não faz nada.
                    return;
                }

                // 2. Se não existe, cria o usuário Master
                
                // LEIA AS CREDENCIAIS DA CONFIGURAÇÃO - NUNCA DEIXE NO CÓDIGO!
                var masterEmail = configuration["MasterUser:Email"];
                var masterPassword = configuration["MasterUser:Password"];

                if(string.IsNullOrEmpty(masterEmail) || string.IsNullOrEmpty(masterPassword))
                {
                    Console.WriteLine("Credenciais do usuário Master não encontradas na configuração. Pulando o seeding.");
                    return;
                }

                var masterUser = new User
                {
                    Name = "Master Admin",
                    Email = masterEmail,
                    Role = UserRole.Master, // Supondo que você use um Enum
                    Password = passwordService.HashPassword(masterPassword)
                };

                await context.Users.AddAsync(masterUser);
                await context.SaveChangesAsync();
                
                Console.WriteLine("Usuário Master criado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocorreu um erro durante o data seeding: {ex.Message}");
            }
        }
    }
}