using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Requester.Read;

public class GetRequestersService : IGetRequestersService
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRequestersService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IEnumerable<Entities.Requester>> GetAll()
    {
        return await _unitOfWork.RequesterRepository.GetAllAsync();
    }

    public Task<Entities.Requester?> Get(int id)
    {
        var requester = _unitOfWork.RequesterRepository.GetRequesterAsync(id);
        if (requester == null)
            throw new KeyNotFoundException("Requester not found");
        return requester;
    }
}