using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Services.User.Read;

public class GetUsersService : IGetUsersService
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UserDto>> GetAll()
    {
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        return users.Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role, u.IsActive));
    }

    public async Task<UserDto> Get(Guid id)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with id {id} not found");
        return new UserDto(user.Id, user.Name, user.Email, user.Role, user.IsActive);
    }
}