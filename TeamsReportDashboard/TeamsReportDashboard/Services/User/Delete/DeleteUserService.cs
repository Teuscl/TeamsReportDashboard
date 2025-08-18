using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Services.User.Delete;

public class DeleteUserService : IDeleteUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public DeleteUserService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }
    public async Task Execute(int userId)
    {
        var userToDelete = await _unitOfWork.UserRepository.GetByIdAsync(userId);
        if (userToDelete == null)
            throw new KeyNotFoundException($"User with id {userId} not found");
        
        var protectedUserEmail = _configuration["MasterUser:Email"];

        if (!string.IsNullOrWhiteSpace(protectedUserEmail) &&
            userToDelete.Email.Equals(protectedUserEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"This user cannot be deleted!");
        }
        await _unitOfWork.UserRepository.DeleteAsync(userId);
        await _unitOfWork.CommitAsync();
    }
}