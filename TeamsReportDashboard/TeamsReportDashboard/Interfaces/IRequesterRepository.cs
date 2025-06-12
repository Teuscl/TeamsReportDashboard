using TeamsReportDashboard.Backend.Entities;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface IRequesterRepository
{
    Task<List<Requester>> GetAllAsync();
    Task<Requester?> GetRequesterAsync(int id); // Retorno pode ser nulo
    Task CreateRequesterAsync(Requester requester);
    void UpdateRequester(Requester requester); // Assinatura corrigida
    Task DeleteRequesterAsync(int id);         // Assinatura corrigida
    Task<bool> ExistsAsync(int id);             // Adicionado
    
    Task<Requester?> GetByEmailAsync(string email);
}