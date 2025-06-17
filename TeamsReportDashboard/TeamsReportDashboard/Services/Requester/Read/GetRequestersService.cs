using TeamsReportDashboard.Backend.Models.Requester;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Requester.Read;

public class GetRequestersService : IGetRequestersService
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRequestersService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IEnumerable<RequesterDto>> GetAll()
    {
        // O repositório agora retorna Requesters com a propriedade Department preenchida
        var requesters = await _unitOfWork.RequesterRepository.GetAllAsync();
        
        // Mapeamos a lista de Entidades para uma lista de DTOs
        return requesters.Select(r => new RequesterDto
        {
            Id = r.Id,
            Name = r.Name,
            Email = r.Email,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department?.Name // Agora r.Department não é mais nulo!
        });
    }

    public async Task<RequesterDto?> Get(int id)
    {
        var requester = await _unitOfWork.RequesterRepository.GetRequesterAsync(id);
        if (requester == null) return null;

        return new RequesterDto
        {
            Id = requester.Id,
            Name = requester.Name,
            Email = requester.Email,
            DepartmentId = requester.DepartmentId,
            DepartmentName = requester.Department?.Name // Aqui também!
        };
    }
}