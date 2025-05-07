using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Services.User.Delete;

public class DeleteUserService : IDeleteUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task Execute(int userId)
    {
        await _unitOfWork.UserRepository.DeleteAsync(userId);
        await _unitOfWork.CommitAsync();
    }
}