using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Services.User.Read;

public class GetUsersService : IGetUsersService
{
    private IUnitOfWork _unitOfWork;

    public GetUsersService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<IEnumerable<Entities.User>> GetAll() => await _unitOfWork.UserRepository.GetAllAsync();

    public async Task<Entities.User> Get(int id)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException("User not found");
        return user;
    }
    
}