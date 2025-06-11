using TeamsReportDashboard.Backend.Entities;

namespace TeamsReportDashboard.Backend.Interfaces;

public interface IRequesterRepository
{
    Task<List<Requester>> GetAllAsync();
    Task<Requester> GetRequesterAsync(int id);
    Task CreateRequesterAsync(Requester requester);
    Task<bool> UpdateRequesterAsync(Requester requester);
    Task<bool> DeleteRequesterAsync(int id);
}
